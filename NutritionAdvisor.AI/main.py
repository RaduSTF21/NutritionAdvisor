import os
import json
import re
import uuid
from datetime import datetime, timedelta
from urllib.parse import urlsplit
from typing import List, Optional, Dict, Any, Union

# --- IMPORT GUARD ---
try:
    from google import genai  # type: ignore[import]
    HAS_GENAI = True
except ImportError:
    genai = None  # type: ignore[assignment]
    HAS_GENAI = False

from fastapi import FastAPI, Query
import asyncio
import time
import random
from pydantic import BaseModel, ConfigDict
from pydantic.alias_generators import to_camel

# --- CONFIGURARE GEMINI ---
_raw_keys = os.getenv("GEMINI_API_KEYS", os.getenv("GEMINI_API_KEY", ""))
GEMINI_API_KEYS = [k.strip() for k in _raw_keys.split(",") if k.strip()]
GEMINI_MODEL = str(os.getenv("GEMINI_MODEL", "gemini-2.0-flash"))
GEMINI_MAX_RETRIES = int(os.getenv("GEMINI_MAX_RETRIES", "0"))
GEMINI_CACHE_TTL_SECONDS = int(os.getenv("GEMINI_CACHE_TTL_SECONDS", "600"))

_AI_SEMAPHORE: Optional[asyncio.Semaphore] = None
try:
    _AI_SEMAPHORE = asyncio.Semaphore(int(os.getenv("AI_MAX_CONCURRENCY", "2")))
except Exception:
    _AI_SEMAPHORE = asyncio.Semaphore(2)

if GEMINI_API_KEYS and HAS_GENAI:
    print(f"Loaded {len(GEMINI_API_KEYS)} Gemini API keys. Using model: {GEMINI_MODEL}")
else:
    print("WARNING: GEMINI_API_KEY not set or google-genai not installed. Using local fallbacks.")

_GEMINI_RESPONSE_CACHE: Dict[str, Dict[str, Any]] = {}

app = FastAPI(title="Nutrition AI Service")

# --- MODELE ---
class BaseAiModel(BaseModel):
    model_config = ConfigDict(
        alias_generator=to_camel,
        populate_by_name=True,
    )

class UserProfileAI(BaseAiModel):
    user_id: str
    objective: Optional[str] = None
    search_query: Optional[str] = None
    use_internet_search: bool = False
    allergies: List[str] = []
    disliked_ingredients: List[str] = []
    limit: int = 5
    available_recipes: List[Dict[str, Any]] = []

class MealPlanRequest(BaseAiModel):
    user_id: str
    objective: Optional[str] = None
    days: int = 7
    allergies: List[str] = []
    disliked_ingredients: List[str] = []
    available_recipes: List[Dict[str, Any]] = []
    weight_kg: Optional[float] = None
    height_cm: Optional[float] = None
    age: Optional[int] = None
    gender: Optional[str] = None
    diet_type: Optional[str] = None
    preferred_cuisines: List[str] = []

class CoachRequest(BaseAiModel):
    user_id: str
    objective: Optional[str] = None
    message: str
    context: Optional[str] = None
    allergies: List[str] = []
    disliked_ingredients: List[str] = []


# --- HELPERS ---
def _normalize_text(value: Optional[str]) -> str:
    return (value or "").strip().lower()


def _is_guid_string(value: Any) -> bool:
    if not value:
        return False
    try:
        uuid.UUID(str(value))
        return True
    except (ValueError, TypeError, AttributeError):
        return False


def _stable_json(value: Any) -> str:
    return json.dumps(value, sort_keys=True, separators=(",", ":"), ensure_ascii=True)


def _compact_recipe(recipe: Dict[str, Any]) -> Dict[str, Any]:
    ingredients = recipe.get("ingredients") or []
    if isinstance(ingredients, list):
        ingredients = [str(item) for item in ingredients[:6]]
    else:
        ingredients = [str(ingredients)]

    return {
        "id": str(recipe.get("id") or ""),
        "title": str(recipe.get("title") or ""),
        "calories": int(recipe.get("calories", 0) or 0),
        "cookingTimeInMinutes": int(recipe.get("cookingTimeInMinutes", 0) or 0),
        "ingredients": ingredients,
    }


def _compact_available_recipes(recipes: List[Dict[str, Any]], limit: int = 6) -> List[Dict[str, Any]]:
    return [_compact_recipe(recipe) for recipe in recipes[:limit]]


def _compact_gemini_cache_key(endpoint: str, payload: Dict[str, Any]) -> str:
    return f"{endpoint}:{_stable_json(payload)}"


def _get_cached_gemini_response(cache_key: str) -> Optional[Dict[str, Any]]:
    entry = _GEMINI_RESPONSE_CACHE.get(cache_key)
    if not entry:
        return None
    if time.time() - entry.get("ts", 0) > GEMINI_CACHE_TTL_SECONDS:
        _GEMINI_RESPONSE_CACHE.pop(cache_key, None)
        return None
    return entry.get("data")


def _store_cached_gemini_response(cache_key: str, data: Dict[str, Any]) -> None:
    _GEMINI_RESPONSE_CACHE[cache_key] = {"ts": time.time(), "data": data}

def _recipe_text(recipe: Dict[str, Any]) -> str:
    ingredients = recipe.get("ingredients") or []
    ingredients_text = " ".join(str(i) for i in ingredients) if isinstance(ingredients, list) else str(ingredients)
    return " ".join([
        str(recipe.get("title", "")),
        str(recipe.get("description", "")),
        ingredients_text,
        str(recipe.get("goal", "")),
    ]).lower()

def _matches_avoid_list(recipe: Dict[str, Any], avoid_terms: List[str]) -> bool:
    text = _recipe_text(recipe)
    return any(_normalize_text(t) and _normalize_text(t) in text for t in avoid_terms)

def _objective_score(recipe: Dict[str, Any], objective: Optional[str]) -> int:
    text = _recipe_text(recipe)
    objective_text = _normalize_text(objective)
    if not objective_text:
        return 0
    keywords: Dict[str, List[str]] = {
        "weight loss": ["light", "salad", "low calorie", "protein", "fit"],
        "muscle": ["protein", "chicken", "beef", "egg", "tuna"],
        "healthy": ["salad", "vegetable", "bowl", "grilled", "fresh"],
        "vegan": ["vegan", "tofu", "lentil", "beans", "chickpea"],
        "vegetarian": ["vegetarian", "cheese", "egg", "yogurt", "salad"],
        "keto": ["keto", "avocado", "egg", "salmon", "cheese"],
    }
    score = sum(
        sum(1 for term in terms if term in text)
        for key, terms in keywords.items()
        if key in objective_text
    )
    score += sum(1 for word in objective_text.split() if word and word in text)
    return score

def _fallback_recommendations(request: UserProfileAI) -> Dict[str, Any]:
    candidates = [
        r for r in request.available_recipes
        if not _matches_avoid_list(r, request.allergies + request.disliked_ingredients)
    ] or list(request.available_recipes)
    ranked = sorted(
        candidates,
        key=lambda r: (_objective_score(r, request.objective), -int(r.get("calories", 0) or 0)),
        reverse=True,
    )
    recommendations = [
        {
            "id": str(r.get("id", "fallback")),
            "title": str(r.get("title", "Suggested recipe")),
            "description": str(r.get("description", "")),
            "ingredients": [str(i) for i in (r.get("ingredients") or [])][:8],
            "reason": "Selected locally.",
            "premium": False,
            "cookingTimeInMinutes": int(r.get("cookingTimeInMinutes", 0) or 0),
        }
        for r in ranked[: max(1, request.limit)]
    ]
    if len(recommendations) < max(1, request.limit):
        ext_results = _cached_search_themealdb(request.search_query or request.objective or "", limit=request.limit)
        for r in ext_results:
            recommendations.append({
                "id": str(r.get("id") or "external"),
                "title": r.get("title") or "Recipe",
                "description": r.get("description") or "",
                "ingredients": r.get("ingredients") or [],
                "reason": "Suggested recipe.",
                "premium": False,
                "cookingTimeInMinutes": int(r.get("cookingTimeInMinutes", 30) or 30),
                "externalUrl": r.get("externalUrl"),
            })

    return {"mode": "gemini", "userId": request.user_id, "user_id": request.user_id, "recommendations": recommendations}


def _search_themealdb(query: Optional[str], limit: int = 5) -> List[Dict[str, Any]]:
    try:
        import requests
    except Exception:
        return []

    q = (query or "").strip()
    try:
        url = f"https://www.themealdb.com/api/json/v1/1/search.php?s={q}"
        resp = requests.get(url, timeout=6)
        if resp.status_code != 200:
            return []
        payload = resp.json()
        meals = payload.get("meals") or []
        results: List[Dict[str, Any]] = []
        for m in meals[:limit]:
            ingredients = []
            for i in range(1, 21):
                ing = m.get(f"strIngredient{i}")
                measure = m.get(f"strMeasure{i}")
                if ing and ing.strip():
                    if measure and measure.strip():
                        ingredients.append(f"{measure.strip()} {ing.strip()}")
                    else:
                        ingredients.append(ing.strip())

            source_url = m.get("strSource")
            youtube_url = m.get("strYoutube")
            external_url = None
            for candidate_url in [source_url, youtube_url]:
                if candidate_url and not _is_placeholder_url(candidate_url) and _is_url_accessible(candidate_url):
                    external_url = candidate_url
                    break

            results.append({
                "id": m.get("idMeal"),
                "title": m.get("strMeal"),
                "description": (m.get("strInstructions") or "")[:400],
                "ingredients": ingredients,
                "sourceUrl": source_url,
                "youtubeUrl": youtube_url,
                "externalUrl": external_url,
                "cookingTimeInMinutes": 30,
            })
        return results
    except Exception as e:
        print(f"TheMealDB search failed: {e}")
        return []


_THEMEALDB_CACHE: Dict[str, Dict[str, Any]] = {}
_THEMEALDB_CACHE_TTL = int(os.getenv("THEMEALDB_CACHE_TTL", "3600"))

def _cached_search_themealdb(query: Optional[str], limit: int = 5) -> List[Dict[str, Any]]:
    key = f"{(query or '').strip().lower()}::{limit}"
    now = time.time()
    entry = _THEMEALDB_CACHE.get(key)
    if entry and (now - entry.get("ts", 0) < _THEMEALDB_CACHE_TTL):
        return entry.get("data", [])
    data = _search_themealdb(query, limit=limit)
    _THEMEALDB_CACHE[key] = {"ts": now, "data": data}
    return data


_URL_VALIDATION_CACHE: Dict[str, Dict[str, Any]] = {}
_URL_VALIDATION_CACHE_TTL = int(os.getenv("URL_VALIDATION_CACHE_TTL", "86400"))


def _homepage_like_path(parsed_url: Any) -> bool:
    path = (getattr(parsed_url, "path", "") or "").strip().lower()
    return path in {"", "/", "/home", "/index", "/index.html"}


def _is_placeholder_url(url: Optional[str]) -> bool:
    if not url or not isinstance(url, str):
        return False

    try:
        host = urlsplit(url.strip()).netloc.lower()
    except Exception:
        return False

    return host in {"example.com", "www.example.com", "example.org", "www.example.org", "example.net", "www.example.net"}


def _is_url_accessible(url: Optional[str], timeout: float = 6.0) -> bool:
    if not url or not isinstance(url, str):
        return False

    cleaned_url = url.strip()
    if not cleaned_url.startswith(("http://", "https://")):
        return False

    now = time.time()
    cache_entry = _URL_VALIDATION_CACHE.get(cleaned_url)
    if cache_entry and now - cache_entry.get("ts", 0) < _URL_VALIDATION_CACHE_TTL:
        return bool(cache_entry.get("valid", False))

    try:
        import requests

        response = requests.head(cleaned_url, timeout=timeout, allow_redirects=True)
        final_url = response.url or cleaned_url
        final_status = response.status_code

        if final_status == 405:
            response = requests.get(cleaned_url, timeout=timeout, allow_redirects=True, stream=True)
            final_url = response.url or cleaned_url
            final_status = response.status_code

        parsed_original = urlsplit(cleaned_url)
        parsed_final = urlsplit(final_url)
        redirected_to_homepage = bool(response.history) and parsed_original.netloc == parsed_final.netloc and _homepage_like_path(parsed_final) and not _homepage_like_path(parsed_original)
        is_valid = 200 <= final_status < 300 and not redirected_to_homepage

        _URL_VALIDATION_CACHE[cleaned_url] = {"ts": now, "valid": is_valid}
        return is_valid
    except Exception:
        _URL_VALIDATION_CACHE[cleaned_url] = {"ts": now, "valid": False}
        return False


def _resolve_verified_external_recipe(recipe_title: str, objective: Optional[str] = None) -> Optional[Dict[str, Any]]:
    stop_words = {"the", "and", "with", "for", "a", "an", "to", "of", "in", "on", "at", "by", "easy", "quick", "healthy", "best", "simple", "recipe", "recipes", "baked", "fresh"}
    words = [word.strip(".,()[]{}!?:;\"'" ).lower() for word in str(recipe_title).split()]
    keywords = [word for word in words if len(word) >= 4 and word not in stop_words]

    search_terms = [recipe_title]
    if keywords:
        search_terms.append(" ".join(keywords[:4]))
        search_terms.extend(keywords[:6])
    if objective:
        search_terms.append(objective)

    seen_terms = set()
    unique_terms: List[str] = []
    for term in search_terms:
        cleaned_term = str(term).strip() if term is not None else ""
        if not cleaned_term:
            continue
        normalized_term = cleaned_term.lower()
        if normalized_term in seen_terms:
            continue
        seen_terms.add(normalized_term)
        unique_terms.append(cleaned_term)

    for term in unique_terms:
        candidates = _cached_search_themealdb(str(term), limit=8)
        for candidate in candidates:
            for candidate_url in [candidate.get("externalUrl"), candidate.get("sourceUrl"), candidate.get("youtubeUrl")]:
                if candidate_url and not _is_placeholder_url(candidate_url) and _is_url_accessible(candidate_url):
                    return {
                        "id": candidate.get("id"),
                        "title": candidate.get("title") or recipe_title,
                        "description": candidate.get("description") or "",
                        "ingredients": candidate.get("ingredients") or [],
                        "externalUrl": candidate_url,
                        "cookingTimeInMinutes": candidate.get("cookingTimeInMinutes") or 30,
                    }
    return None


def _enrich_meal_plan_external_urls(days: List[Dict[str, Any]], objective: Optional[str] = None) -> List[Dict[str, Any]]:
    enriched_days: List[Dict[str, Any]] = []

    for day in days:
        if not isinstance(day, dict):
            enriched_days.append(day)
            continue

        items = []
        for item in day.get("items") or []:
            if not isinstance(item, dict):
                items.append(item)
                continue

            external_url = item.get("externalUrl")
            if external_url and not _is_placeholder_url(external_url) and _is_url_accessible(external_url):
                items.append(item)
                continue

            resolved = _resolve_verified_external_recipe(str(item.get("title") or item.get("mealType") or "Recipe"), objective)
            if resolved:
                resolved_recipe_id = resolved.get("recipeId")
                item = {
                    **item,
                    "recipeId": item.get("recipeId") if _is_guid_string(item.get("recipeId")) else (_is_guid_string(resolved_recipe_id) and str(resolved_recipe_id) or None),
                    "title": resolved.get("title") or item.get("title"),
                    "externalUrl": resolved.get("externalUrl"),
                }
            else:
                item = {**item, "externalUrl": None}

            items.append(item)

        enriched_days.append({**day, "items": items})

    return enriched_days

def _generate_meal_plan_days_with_macros(
    request: MealPlanRequest, days_count: int
) -> List[Dict[str, Any]]:
    days_list = []
    meal_types = ["Breakfast", "Lunch", "Dinner"]
    
    # Better calorie targets: default 2400 cal/day for maintenance (can be adjusted based on objective and body weight)
    # Breakfast ~30%, Lunch ~35%, Dinner ~35%
    daily_target = 2400
    objective_text = _normalize_text(request.objective)
    if request.weight_kg and request.weight_kg > 0:
        if any(w in objective_text for w in ["weight loss", "cut", "diet"]):
            daily_target = max(1600, min(2600, int(request.weight_kg * 24)))
        elif any(w in objective_text for w in ["muscle", "bulk", "gain"]):
            daily_target = max(2200, min(3400, int(request.weight_kg * 34)))
        else:
            daily_target = max(1800, min(3000, int(request.weight_kg * 30)))
    elif objective_text and any(w in objective_text for w in ["weight loss", "cut", "diet"]):
        daily_target = 1800
    elif objective_text and any(w in objective_text for w in ["muscle", "bulk", "gain"]):
        daily_target = 2800
    
    meal_cals = {
        "Breakfast": int(daily_target * 0.30),
        "Lunch": int(daily_target * 0.35),
        "Dinner": int(daily_target * 0.35),
    }
    
    safe_recipes = [
        r for r in request.available_recipes
        if not _matches_avoid_list(r, request.allergies + request.disliked_ingredients)
    ]

    # Fetch external recipes once for the entire plan (prefer external for diversity if few local recipes)
    external_search_term = (request.preferred_cuisines[0] if request.preferred_cuisines else None) or request.objective or ""
    external_recipes = _cached_search_themealdb(external_search_term, limit=12) if len(safe_recipes) < 4 else []

    # Track previous day's chosen recipe IDs per meal type
    prev_recipe_id_for_meal: Dict[str, Optional[str]] = {mt: None for mt in meal_types}
    
    for day_offset in range(days_count):
        day_date = (datetime.utcnow().date() + timedelta(days=day_offset)).isoformat()
        items = []
        day_cals = 0
        
        # Track recipe IDs used in THIS day to avoid same-day repeats
        recipe_ids_used_today: set = set()
        
        for idx, meal_type in enumerate(meal_types):
            recipe = None
            recipe_id = None
            external_url = None
            title = "Meal"
            cals = meal_cals.get(meal_type, 700)
            protein = 25
            carbs = 60
            fats = 15
            target_cals = cals
            repetitions = 1
            
            # Strategy 1: Try external recipes first for diversity (if few local)
            selected_recipe = None
            
            if external_recipes and len(safe_recipes) < 4:
                # Prefer external: find one not used today and not used yesterday for this meal type
                for ext in external_recipes:
                    ext_id = str(ext.get("id") or "")
                    if ext_id not in recipe_ids_used_today and ext_id != prev_recipe_id_for_meal.get(meal_type):
                        selected_recipe = ("external", ext)
                        recipe_id = None  # External recipes don't have local IDs
                        recipe_ids_used_today.add(ext_id)
                        break
            
            # Strategy 2: Use local recipes (ensuring variety)
            if selected_recipe is None and safe_recipes:
                for local_rec in safe_recipes:
                    local_id = str(local_rec.get("id", ""))
                    # Only pick if NOT used today AND NOT same as yesterday for this meal type
                    if local_id not in recipe_ids_used_today and local_id != prev_recipe_id_for_meal.get(meal_type):
                        selected_recipe = ("local", local_rec)
                        recipe_id = local_id
                        recipe_ids_used_today.add(local_id)
                        break
            
            # Strategy 3: Fallback - try TheMealDB if nothing else
            if selected_recipe is None and not external_recipes:
                ext = _cached_search_themealdb(external_search_term, limit=5)
                if ext:
                    for ext_cand in ext:
                        ext_id = str(ext_cand.get("id") or "")
                        if ext_id not in recipe_ids_used_today and ext_id != prev_recipe_id_for_meal.get(meal_type):
                            selected_recipe = ("external", ext_cand)
                            recipe_ids_used_today.add(ext_id)
                            break
            
            # Last resort: use any available (just to fill the slot)
            if selected_recipe is None and safe_recipes:
                for local_rec in safe_recipes:
                    local_id = str(local_rec.get("id", ""))
                    if local_id not in recipe_ids_used_today:
                        selected_recipe = ("local", local_rec)
                        recipe_id = local_id
                        recipe_ids_used_today.add(local_id)
                        break
            
            # Process selected recipe
            if selected_recipe:
                recipe_type, recipe = selected_recipe
                
                title = str(recipe.get("title", "Meal"))
                if recipe_type == "external":
                    external_url = recipe.get("externalUrl")
                    ingredients = recipe.get("ingredients") or []
                    recipe_cals = max(300, 300 + len(ingredients) * 20)
                else:
                    recipe_cals = int(recipe.get("calories", target_cals) or target_cals)
                
                # Apply repetition if recipe is too small for target
                if recipe_cals < target_cals * 0.75 and recipe_cals > 0:
                    repetitions = max(1, min(3, int(target_cals / recipe_cals)))
                    if repetitions > 1:
                        title = f"{title} (x{repetitions})"
                    cals = recipe_cals * repetitions
                else:
                    cals = recipe_cals
                
                # Calculate macros
                if recipe_type == "external":
                    ingredients = recipe.get("ingredients") or []
                    protein = max(15, min(40, len(ingredients) * 2)) * repetitions
                else:
                    ingredients_count = len(recipe.get("ingredients", []) or [])
                    protein = max(20, min(40, ingredients_count * 3)) * repetitions
                
                carbs = max(50, cals // 3 // 4)
                fats = max(10, (cals - protein * 4 - carbs * 4) // 9)
                
                # Update prev_recipe_id for this meal type for next day's logic
                current_recipe_id = recipe_id or str(recipe.get("id") or f"ext_{idx}")
                prev_recipe_id_for_meal[meal_type] = current_recipe_id
            
            item = {
                "mealType": meal_type,
                "recipeId": recipe_id,
                "title": title,
                "externalUrl": external_url,
                "calories": int(cals),
                "protein": int(protein),
                "carbs": int(carbs),
                "fats": int(fats),
            }
            items.append(item)
            day_cals += int(cals)
        
        day = {
            "date": day_date,
            "title": f"Day {day_offset + 1}",
            "description": f"Balanced meal plan for {day_date}",
            "calories": day_cals,
            "items": items,
        }
        days_list.append(day)
    
    return days_list


def _fallback_meal_plan(request: MealPlanRequest) -> Dict[str, Any]:
    days_count = max(1, min(request.days, 14))
    days = _generate_meal_plan_days_with_macros(request, days_count)
    days = _enrich_meal_plan_external_urls(days, request.objective)
    
    return {
        "mode": "gemini",
        "userId": request.user_id,
        "user_id": request.user_id,
        "summary": f"Meal plan for {days_count} days.",
        "days": days,
    }

def _fallback_coach(request: CoachRequest) -> Dict[str, Any]:
    return {"mode": "fallback", "userId": request.user_id, "user_id": request.user_id, "answer": "Focus on sustainable choices.", "tips": []}


def _compact_recommendation_payload(request: UserProfileAI) -> Dict[str, Any]:
    return {
        "objective": request.objective,
        "searchQuery": request.search_query,
        "useInternetSearch": request.use_internet_search,
        "allergies": request.allergies[:6],
        "dislikedIngredients": request.disliked_ingredients[:6],
        "limit": max(1, min(request.limit, 5)),
        "availableRecipes": _compact_available_recipes(request.available_recipes, limit=6),
    }


def _compact_meal_plan_payload(request: MealPlanRequest) -> Dict[str, Any]:
    return {
        "objective": request.objective,
        "days": max(1, min(request.days, 7)),
        "allergies": request.allergies[:6],
        "dislikedIngredients": request.disliked_ingredients[:6],
        "weightKg": request.weight_kg,
        "heightCm": request.height_cm,
        "age": request.age,
        "gender": request.gender,
        "dietType": request.diet_type,
        "preferredCuisines": request.preferred_cuisines[:6],
        "availableRecipes": _compact_available_recipes(request.available_recipes, limit=6),
    }


def _build_gemini_prompt(endpoint: str, payload: Dict[str, Any]) -> str:
    if endpoint == "recommend":
        return (
            "Return compact JSON only. "
            f"objective={payload.get('objective')}; allergies={payload.get('allergies')}; "
            f"dislikes={payload.get('dislikedIngredients')}; limit={payload.get('limit')}; "
            f"searchQuery={payload.get('searchQuery')}; useInternetSearch={payload.get('useInternetSearch')}; "
            f"recipes={_stable_json(payload.get('availableRecipes') or [])}. "
            "Schema: {recommendations:[{title,description,ingredients,reason,cookingTimeInMinutes,externalUrl}]}.")
    if endpoint == "meal-plan":
        return (
            "Return compact JSON only. Create a diverse meal plan with NO REPEATS in the same day across different meal types. "
            "Vary recipes across days. If local recipes are limited, MUST search internet for diverse options. "
            f"objective={payload.get('objective')}; days={payload.get('days')}; weightKg={payload.get('weightKg')}; "
            f"heightCm={payload.get('heightCm')}; age={payload.get('age')}; gender={payload.get('gender')}; "
            f"dietType={payload.get('dietType')}; preferredCuisines={payload.get('preferredCuisines')}; "
            f"allergies={payload.get('allergies')}; dislikes={payload.get('dislikedIngredients')}; "
            f"availableRecipes={_stable_json(payload.get('availableRecipes') or [])}. "
            "Use only different recipes for breakfast/lunch/dinner in same day. If few recipes available, fetch from internet for diversity. "
            "Schema: {summary,days:[{date,title,description,calories,items:[{mealType,title,recipeId,externalUrl,calories,protein,carbs,fats}]}]}."
        )
    return (
        "Return compact JSON only. "
        f"objective={payload.get('objective')}; message={payload.get('message')}; context={payload.get('context')}; "
        f"allergies={payload.get('allergies')}; dislikes={payload.get('dislikedIngredients')}. "
        "Schema: {answer,tips:[...]}."
    )


def _compact_coach_payload(request: CoachRequest) -> Dict[str, Any]:
    return {
        "objective": request.objective,
        "message": request.message[:500],
        "context": (request.context or "")[:300],
        "allergies": request.allergies[:6],
        "dislikedIngredients": request.disliked_ingredients[:6],
    }

def _parse_json_or_fallback(raw_text: Optional[str], fallback: Dict[str, Any]) -> Dict[str, Any]:
    if not raw_text:
        return fallback
    try:
        return json.loads(raw_text)
    except json.JSONDecodeError:
        match = re.search(r"\{.*\}", raw_text, re.DOTALL)
        if match:
            try:
                return json.loads(match.group(0))
            except json.JSONDecodeError:
                pass
    return fallback


def _extract_retry_seconds(exc: Exception) -> Optional[float]:
    try:
        txt = str(exc)
        m = re.search(r"Please retry in\s*([0-9]+(?:\.[0-9]+)?)s", txt, re.IGNORECASE)
        if m:
            return float(m.group(1))
        m2 = re.search(r"retryDelay\W*['\"]?([0-9]+)s['\"]?", txt, re.IGNORECASE)
        if m2:
            return float(m2.group(1))
    except Exception:
        return None
    return None


# --- MULTI-KEY MANAGEMENT ---
_KEY_EXHAUSTION_TRACKER: Dict[str, float] = {}
_KEY_ROTATION_CURSOR = 0


def _rotated_keys(keys: List[str]) -> List[str]:
    global _KEY_ROTATION_CURSOR

    if len(keys) <= 1:
        return list(keys)

    start_index = _KEY_ROTATION_CURSOR % len(keys)
    _KEY_ROTATION_CURSOR = (_KEY_ROTATION_CURSOR + 1) % len(keys)
    return keys[start_index:] + keys[:start_index]

async def _genai_generate_with_retries(
    contents: str, 
    gen_config: Dict[str, Any], 
    model: Optional[str] = None, 
    max_retries: int = 0
) -> Any:
    """Call Gemini with multiple API key round-robin, retries, and semaphore protection."""
    if genai is None or not GEMINI_API_KEYS:
        raise RuntimeError("GenAI not available or no API keys configured.")
    
    model_name = str(model or GEMINI_MODEL)
    last_exc: Optional[Exception] = None
    available_keys = [k for k in GEMINI_API_KEYS if k not in _KEY_EXHAUSTION_TRACKER or time.time() - _KEY_EXHAUSTION_TRACKER[k] > 3600]

    if not available_keys:
        available_keys = list(GEMINI_API_KEYS)

    available_keys = _rotated_keys(available_keys)

    for api_key in available_keys:
        local_client = genai.Client(api_key=str(api_key))
        
        for attempt in range(max_retries + 1):
            try:
                async with (_AI_SEMAPHORE or asyncio.Semaphore(1)):
                    resp = await asyncio.to_thread(
                        lambda c=local_client: c.models.generate_content(model=model_name, contents=contents, config=gen_config)
                    )
                print(f"[AI] Gemini request succeeded with key ending in ...{api_key[-4:]}")
                return resp
            except Exception as e:
                last_exc = e
                error_str = str(e)
                
                if "RESOURCE_EXHAUSTED" in error_str or "429" in error_str:
                    print(f"[AI] API key exhausted (ending in ...{api_key[-4:]}), marking for 1 hour and trying next key.")
                    _KEY_EXHAUSTION_TRACKER[api_key] = time.time()
                    break  # Move to next key
                
                retry_seconds = _extract_retry_seconds(e)
                if retry_seconds is None:
                    base = min(8, 2 ** attempt)
                    jitter = random.uniform(0, 1)
                    retry_seconds = base + jitter
                
                if attempt >= max_retries:
                    print(f"[AI] Max retries exceeded for key ending in ...{api_key[-4:]}: {error_str[:100]}")
                    break  # Move to next key
                
                try:
                    print(f"[AI] Retry attempt {attempt + 1}/{max_retries + 1} after {retry_seconds:.1f}s")
                    await asyncio.sleep(retry_seconds)
                except Exception:
                    pass

    raise last_exc or RuntimeError("GenAI generate failed across all provided API keys.")


def _safe_int(value: Any, default: int = 0) -> int:
    """Safely convert value to int, handling strings with units like '12g', '500 cal'."""
    if isinstance(value, int):
        return value
    if value is None:
        return default
    try:
        # If already a number, return it
        return int(value)
    except (ValueError, TypeError):
        # Try to extract digits from string
        try:
            text = str(value).strip()
            # Extract all digits (and optional decimal point)
            import re
            match = re.search(r'(\d+(?:\.\d+)?)', text)
            if match:
                return int(float(match.group(1)))
        except Exception:
            pass
    return default


def _normalize_meal_plan_item(item: Any) -> Dict[str, Any]:
    if not isinstance(item, dict):
        return {
            "mealType": "Meal",
            "recipeId": None,
            "title": str(item),
            "externalUrl": None,
            "calories": 500,
            "protein": 25,
            "carbs": 60,
            "fats": 15,
        }
    
    return {
        "mealType": str(item.get("mealType", "Meal")),
        "recipeId": str(item.get("recipeId")) if _is_guid_string(item.get("recipeId")) else None,
        "title": str(item.get("title", "Meal")),
        "externalUrl": str(item.get("externalUrl")) if item.get("externalUrl") else None,
        "calories": _safe_int(item.get("calories", 500), 500),
        "protein": _safe_int(item.get("protein", 25), 25),
        "carbs": _safe_int(item.get("carbs", 60), 60),
        "fats": _safe_int(item.get("fats", 15), 15)
    }


def _normalize_meal_plan_day(day: Any) -> Dict[str, Any]:
    if not isinstance(day, dict):
        return {
            "date": datetime.utcnow().date().isoformat(),
            "title": "Day",
            "description": "",
            "calories": 1500,
            "items": [],
        }
    
    items = day.get("items") or []
    normalized_items = [_normalize_meal_plan_item(it) for it in items]
    total_cals = sum(_safe_int(it.get("calories", 500), 500) for it in normalized_items)
    
    return {
        "date": str(day.get("date", datetime.utcnow().date().isoformat())),
        "title": str(day.get("title", "Day")),
        "description": str(day.get("description", "")),
        "calories": _safe_int(day.get("calories", total_cals), total_cals),
        "items": normalized_items,
    }


def _normalize_recommendation_item(item: Any) -> Dict[str, Any]:
    if not isinstance(item, dict):
        return {
            "id": None,
            "title": str(item),
            "description": "",
            "servings": 1,
            "totalCalories": 500,
            "protein": 25,
            "carbs": 60,
            "fats": 15,
            "externalUrl": None,
            "ingredients": [],
        }
    
    return {
        "id": str(item.get("id")) if item.get("id") else None,
        "title": str(item.get("title", "Recipe")),
        "description": str(item.get("description", "")),
        "servings": _safe_int(item.get("servings", 1), 1),
        "totalCalories": _safe_int(item.get("totalCalories", 500), 500),
        "protein": _safe_int(item.get("protein", 25), 25),
        "carbs": _safe_int(item.get("carbs", 60), 60),
        "fats": _safe_int(item.get("fats", 15), 15),
        "externalUrl": str(item.get("externalUrl")) if item.get("externalUrl") else None,
        "ingredients": item.get("ingredients", []),
    }


def _build_search_tools() -> Optional[List[Any]]:
    try:
        from google.genai import types as _types  # type: ignore[import]
        return [_types.Tool(google_search=_types.GoogleSearch())]
    except (ImportError, Exception):
        return None


# --- ENDPOINTS ---

@app.get("/")
def read_root() -> Dict[str, str]:
    return {"status": f"Nutrition AI service running powered by {GEMINI_MODEL}"}

@app.post("/recommend")
async def recommend_recipes(request: UserProfileAI, debug: bool = Query(False)) -> Dict[str, Any]:
    fallback = _fallback_recommendations(request)
    if genai is None or not GEMINI_API_KEYS:
        return fallback

    if request.use_internet_search and (request.search_query or request.objective):
        search_term = request.search_query or request.objective or ""
        ext_results = _cached_search_themealdb(search_term, limit=request.limit)
        if ext_results:
            return {
                "mode": "internet",
                "userId": request.user_id,
                "user_id": request.user_id,
                "recommendations": [
                    _normalize_recommendation_item({
                        "id": r.get("id"),
                        "title": r.get("title"),
                        "description": r.get("description"),
                        "ingredients": r.get("ingredients"),
                        "reason": f"Found online for '{search_term}'.",
                        "premium": False,
                        "cookingTimeInMinutes": int(r.get("cookingTimeInMinutes", 30) or 30),
                        "externalUrl": r.get("externalUrl"),
                        "totalCalories": 500,
                    })
                    for r in ext_results
                ],
            }

    compact_payload = _compact_recommendation_payload(request)
    cache_key = _compact_gemini_cache_key("recommend", compact_payload)
    cached = _get_cached_gemini_response(cache_key)
    if cached:
        return cached

    prompt = _build_gemini_prompt("recommend", compact_payload)
    try:
        gen_config = {"response_mime_type": "application/json"}
        response = await _genai_generate_with_retries(prompt, gen_config, model=GEMINI_MODEL, max_retries=GEMINI_MAX_RETRIES)
        data = _parse_json_or_fallback(response.text, fallback)

        # Attach debug info when requested
        if debug:
            raw_text = getattr(response, "text", "")
            debug_info = {
                "prompt": prompt,
                "compact_payload": compact_payload,
                "gen_config": gen_config,
                "raw_response": raw_text[:20000] if isinstance(raw_text, str) else str(raw_text),
                "model": GEMINI_MODEL,
            }
            if isinstance(data, dict):
                data.setdefault("_debug", {}).update(debug_info)
            else:
                data = {"_debug": debug_info, "result": data}
        if isinstance(data, list):
            data = {"recommendations": data}
        result = {
            "mode": data.get("mode", "gemini"),
            "userId": request.user_id,
            "user_id": request.user_id,
            "recommendations": [
                _normalize_recommendation_item(item)
                for item in (data.get("recommendations") or fallback["recommendations"])
            ],
        }
        _store_cached_gemini_response(cache_key, result)
        return result
    except Exception as e:
        print(f"Gemini recommend failed: {e}")
        try:
            ext_results = _cached_search_themealdb(request.search_query or request.objective or "", limit=request.limit)
            if ext_results:
                result = {
                    "mode": "gemini",
                    "userId": request.user_id,
                    "user_id": request.user_id,
                    "recommendations": [
                        _normalize_recommendation_item({
                            "id": r.get("id"),
                            "title": r.get("title"),
                            "description": r.get("description"),
                            "ingredients": r.get("ingredients"),
                            "totalCalories": 500,
                            "protein": 25,
                            "carbs": 60,
                            "fats": 15,
                            "externalUrl": r.get("externalUrl"),
                        })
                        for r in ext_results
                    ],
                }
                _store_cached_gemini_response(cache_key, result)
                return result
        except Exception:
            pass
        # If debug requested, attach the exception/raw info so caller can inspect Gemini failure
        if debug:
            debug_info = {
                "prompt": prompt,
                "compact_payload": compact_payload,
                "gen_config": gen_config,
                "error": str(e),
                "model": GEMINI_MODEL,
            }
            fb = fallback.copy()
            fb.setdefault("_debug", {}).update(debug_info)
            return fb
        return fallback

@app.post("/meal-plan")
async def generate_meal_plan(request: MealPlanRequest, debug: bool = Query(False)) -> Dict[str, Any]:
    fallback = _fallback_meal_plan(request)
    if genai is None or not GEMINI_API_KEYS:
        return fallback

    days = max(1, min(request.days, 14))

    compact_payload = _compact_meal_plan_payload(request)
    compact_payload["days"] = days
    cache_key = _compact_gemini_cache_key("meal-plan", compact_payload)
    cached = _get_cached_gemini_response(cache_key)
    if cached:
        return cached

    prompt = _build_gemini_prompt("meal-plan", compact_payload)
    try:
        gen_config: Dict[str, Any] = {"response_mime_type": "application/json"}
        response = await _genai_generate_with_retries(prompt, gen_config, model=GEMINI_MODEL, max_retries=GEMINI_MAX_RETRIES)
        data = _parse_json_or_fallback(response.text, fallback)

        # If debug requested, attach information about the request/response so caller can inspect payloads
        if debug:
            # Truncate raw response to avoid overly large payloads, but include enough for inspection
            raw_text = getattr(response, "text", "")
            debug_info = {
                "prompt": prompt,
                "compact_payload": compact_payload,
                "gen_config": gen_config,
                "raw_response": raw_text[:20000] if isinstance(raw_text, str) else str(raw_text),
                "model": GEMINI_MODEL,
            }
            # attach debug info into the data returned from Gemini
            if isinstance(data, dict):
                data.setdefault("_debug", {}).update(debug_info)
            else:
                data = {"_debug": debug_info, "result": data}
        
        data["userId"] = request.user_id
        if "user_id" not in data:
            data["user_id"] = request.user_id
        
        days_list = data.get("days") or []
        if not days_list:
            return fallback
        
        normalized_days = [_normalize_meal_plan_day(d) for d in days_list]
        data["days"] = _enrich_meal_plan_external_urls(normalized_days, request.objective)
        
        for i, day in enumerate(data.get("days") or []):
            day["date"] = (datetime.utcnow().date() + timedelta(days=i)).isoformat()
        
        _store_cached_gemini_response(cache_key, data)
        return data
    except Exception as e:
        print(f"Gemini meal plan failed: {e}")
        try:
            fb = _fallback_meal_plan(request)
            # attach debug info when requested so caller can inspect failure
            if debug:
                fb.setdefault("_debug", {}).update({
                    "prompt": prompt,
                    "compact_payload": compact_payload,
                    "gen_config": gen_config,
                    "error": str(e),
                    "model": GEMINI_MODEL,
                })
            _store_cached_gemini_response(cache_key, fb)
            return fb
        except Exception:
            if debug:
                fb2 = fallback.copy()
                fb2.setdefault("_debug", {}).update({"error": str(e), "model": GEMINI_MODEL})
                return fb2
            return fallback

@app.post("/coach")
async def coach(request: CoachRequest, debug: bool = Query(False)) -> Dict[str, Any]:
    fallback = _fallback_coach(request)
    if genai is None or not GEMINI_API_KEYS:
        return fallback

    compact_payload = _compact_coach_payload(request)
    cache_key = _compact_gemini_cache_key("coach", compact_payload)
    cached = _get_cached_gemini_response(cache_key)
    if cached:
        return cached

    prompt = _build_gemini_prompt("coach", compact_payload)
    try:
        gen_config = {"response_mime_type": "application/json"}
        response = await _genai_generate_with_retries(prompt, gen_config, model=GEMINI_MODEL, max_retries=GEMINI_MAX_RETRIES)
        data = _parse_json_or_fallback(response.text, fallback)

        # Attach debug info when requested
        if debug:
            raw_text = getattr(response, "text", "")
            debug_info = {
                "prompt": prompt,
                "compact_payload": compact_payload,
                "gen_config": gen_config,
                "raw_response": raw_text[:20000] if isinstance(raw_text, str) else str(raw_text),
                "model": GEMINI_MODEL,
            }
            if isinstance(data, dict):
                data.setdefault("_debug", {}).update(debug_info)
            else:
                data = {"_debug": debug_info, "result": data}
        if isinstance(data, list):
            data = {"tips": data}
        result = {
            "mode": data.get("mode", "gemini"),
            "userId": request.user_id,
            "user_id": request.user_id,
            "answer": data.get("answer", fallback["answer"]),
            "tips": data.get("tips", fallback["tips"]),
        }
        _store_cached_gemini_response(cache_key, result)
        return result
    except Exception as e:
        print(f"Gemini coach failed: {e}")
        return fallback


@app.post("/prewarm")
async def prewarm(request: MealPlanRequest) -> Dict[str, Any]:
    """Best-effort warm-up: call Gemini for meal-plan and recommendations to populate cache."""
    try:
        # Warm meal-plan cache
        compact_payload = _compact_meal_plan_payload(request)
        compact_payload["days"] = max(1, min(request.days, 7))
        cache_key = _compact_gemini_cache_key("meal-plan", compact_payload)
        try:
            prompt = _build_gemini_prompt("meal-plan", compact_payload)
            gen_config = {"response_mime_type": "application/json"}
            resp = await _genai_generate_with_retries(prompt, gen_config, model=GEMINI_MODEL, max_retries=GEMINI_MAX_RETRIES)
            parsed = _parse_json_or_fallback(getattr(resp, "text", None), _fallback_meal_plan(request))
            if isinstance(parsed, dict):
                _store_cached_gemini_response(cache_key, parsed)
        except Exception as e:
            print(f"Prewarm meal-plan failed: {e}")

        # Warm recommendations cache (use compact recommendation payload)
        try:
            user_profile = UserProfileAI(
                user_id=request.user_id,
                objective=request.objective,
                search_query=None,
                use_internet_search=False,
                allergies=request.allergies or [],
                disliked_ingredients=request.disliked_ingredients or [],
                limit=3,
                available_recipes=request.available_recipes or []
            )
            rec_payload = _compact_recommendation_payload(user_profile)
            rec_cache_key = _compact_gemini_cache_key("recommend", rec_payload)
            rec_prompt = _build_gemini_prompt("recommend", rec_payload)
            gen_config = {"response_mime_type": "application/json"}
            rresp = await _genai_generate_with_retries(rec_prompt, gen_config, model=GEMINI_MODEL, max_retries=GEMINI_MAX_RETRIES)
            rparsed = _parse_json_or_fallback(getattr(rresp, "text", None), _fallback_recommendations(user_profile))
            if isinstance(rparsed, dict):
                _store_cached_gemini_response(rec_cache_key, rparsed)
        except Exception as e:
            print(f"Prewarm recommend failed: {e}")

    except Exception as e:
        print(f"Prewarm overall failed: {e}")
    return {"status": "ok"}
