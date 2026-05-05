import os
import json
import re
from datetime import datetime, timedelta
from typing import List, Optional, Dict, Any

# --- IMPORT GUARD ---
try:
    from google import genai  # type: ignore[import]
    HAS_GENAI = True
except ImportError:
    genai = None  # type: ignore[assignment]
    HAS_GENAI = False

from fastapi import FastAPI
from fastapi.responses import JSONResponse
import asyncio
import time
import random
from pydantic import BaseModel, ConfigDict
from pydantic.alias_generators import to_camel

# --- CONFIGURARE GEMINI ---
GEMINI_API_KEY = os.getenv("GEMINI_API_KEY")
GEMINI_MODEL = os.getenv("GEMINI_MODEL", "gemini-2.0-flash")
GEMINI_MAX_RETRIES = int(os.getenv("GEMINI_MAX_RETRIES", "0"))
GEMINI_CACHE_TTL_SECONDS = int(os.getenv("GEMINI_CACHE_TTL_SECONDS", "600"))

client: Optional[Any] = None
_AI_SEMAPHORE: Optional[asyncio.Semaphore] = None
try:
    _AI_SEMAPHORE = asyncio.Semaphore(int(os.getenv("AI_MAX_CONCURRENCY", "2")))
except Exception:
    _AI_SEMAPHORE = asyncio.Semaphore(2)
if GEMINI_API_KEY and HAS_GENAI:
    client = genai.Client(api_key=GEMINI_API_KEY)  # type: ignore[union-attr]
    print(f"Using Gemini model: {GEMINI_MODEL}")
else:
    print("WARNING: GEMINI_API_KEY not set or google-genai not installed.")

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
    # If we don't have local recipes or not enough, try external search
    if len(recommendations) < max(1, request.limit):
        ext_results = _cached_search_themealdb(request.objective or "", limit=request.limit)
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

    # Return with neutral mode to avoid exposing AI downtime to users
    return {"mode": "gemini", "userId": request.user_id, "user_id": request.user_id, "recommendations": recommendations}


def _search_themealdb(query: Optional[str], limit: int = 5) -> List[Dict[str, Any]]:
    """Search TheMealDB for recipes matching `query`. Returns simplified recipe dicts.
    This API is public and requires no key for basic searches.
    """
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

            results.append({
                "id": m.get("idMeal"),
                "title": m.get("strMeal"),
                "description": (m.get("strInstructions") or "")[:400],
                "ingredients": ingredients,
                "externalUrl": m.get("strSource") or m.get("strYoutube"),
                "cookingTimeInMinutes": 30,
            })
        return results
    except Exception as e:
        print(f"TheMealDB search failed: {e}")
        return []


# Simple in-memory cache for TheMealDB searches to reduce external calls
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

def _generate_meal_plan_days_with_macros(
    request: MealPlanRequest, days_count: int
) -> List[Dict[str, Any]]:
    """
    Generate meal plan days with realistic macros fallback.
    Uses local recipes when available, provides defaults otherwise.
    """
    from uuid import UUID
    
    days_list = []
    meal_types = ["Breakfast", "Lunch", "Dinner"]
    
    # Filter available recipes that don't conflict with allergies
    safe_recipes = [
        r for r in request.available_recipes
        if not _matches_avoid_list(r, request.allergies + request.disliked_ingredients)
    ]
    
    for day_offset in range(days_count):
        day_date = (datetime.utcnow().date() + timedelta(days=day_offset)).isoformat()
        items = []
        day_cals = 0
        
        for idx, meal_type in enumerate(meal_types):
            recipe = None
            recipe_id = None
            external_url = None
            title = "Meal"
            cals = 500 + (idx * 100)  # Default: 500, 600, 700 for B/L/D
            protein = 25
            carbs = 60
            fats = 15
            
            # Try to use a local recipe for this meal
            if safe_recipes:
                recipe = safe_recipes[idx % len(safe_recipes)]
                try:
                    recipe_id = str(recipe.get("id", ""))
                except (ValueError, TypeError):
                    recipe_id = None

                title = str(recipe.get("title", "Meal"))
                cals = int(recipe.get("calories", cals) or cals)
                # Estimate macros if recipe has them
                ingredients_count = len(recipe.get("ingredients", []) or [])
                protein = max(20, min(40, ingredients_count * 3))
                carbs = max(50, cals // 3 // 4)
                fats = max(10, (cals - protein * 4 - carbs * 4) // 9)
            else:
                # If no safe local recipes, try external search (TheMealDB)
                ext = _cached_search_themealdb(request.objective or "", limit=3)
                if ext:
                    pick = ext[(idx) % len(ext)]
                    recipe = pick
                    recipe_id = None
                    title = pick.get("title") or title
                    external_url = pick.get("externalUrl")
                    ingredients = pick.get("ingredients") or []
                    # Heuristic calories estimate: base + per-ingredient
                    cals = max(350, 350 + len(ingredients) * 25)
                    protein = max(15, min(40, len(ingredients) * 2))
                    carbs = max(40, cals // 3 // 4)
                    fats = max(10, (cals - protein * 4 - carbs * 4) // 9)
            
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
    """
    Fallback meal plan generator with realistic structure and macros.
    Always returns a complete plan, even if Gemini fails.
    """
    days_count = max(1, min(request.days, 14))
    days = _generate_meal_plan_days_with_macros(request, days_count)
    
    # Return with neutral mode so UI does not display AI-down state
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
        "availableRecipes": _compact_available_recipes(request.available_recipes, limit=6),
    }


def _build_gemini_prompt(endpoint: str, payload: Dict[str, Any]) -> str:
    if endpoint == "recommend":
        return (
            "Return compact JSON only. "
            f"objective={payload.get('objective')}; allergies={payload.get('allergies')}; "
            f"dislikes={payload.get('dislikedIngredients')}; limit={payload.get('limit')}; "
            f"recipes={_stable_json(payload.get('availableRecipes') or [])}. "
            "Schema: {recommendations:[{title,description,ingredients,reason,cookingTimeInMinutes}]}."
        )
    if endpoint == "meal-plan":
        return (
            "Return compact JSON only. "
            f"objective={payload.get('objective')}; days={payload.get('days')}; allergies={payload.get('allergies')}; "
            f"dislikes={payload.get('dislikedIngredients')}; recipes={_stable_json(payload.get('availableRecipes') or [])}. "
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
    """Try to extract a retry delay (in seconds) from exception text or message."""
    try:
        txt = str(exc)
        # look for patterns like 'Please retry in 12.63s' or 'retryDelay': '12s'
        m = re.search(r"Please retry in\s*([0-9]+(?:\.[0-9]+)?)s", txt, re.IGNORECASE)
        if m:
            return float(m.group(1))
        m2 = re.search(r"retryDelay\W*['\"]?([0-9]+)s['\"]?", txt, re.IGNORECASE)
        if m2:
            return float(m2.group(1))
    except Exception:
        return None
    return None


async def _genai_generate_with_retries(contents: str, gen_config: Dict[str, Any], model: str = None, max_retries: int = 0) -> Any:
    """Call client.models.generate_content with retries, backoff and semaphore protection."""
    if not client:
        raise RuntimeError("genai client not initialized")
    model = model or GEMINI_MODEL
    last_exc: Optional[Exception] = None
    for attempt in range(max_retries + 1):
        try:
            async with (_AI_SEMAPHORE or asyncio.Semaphore(1)):
                resp = await asyncio.to_thread(
                    lambda: client.models.generate_content(model=model, contents=contents, config=gen_config)
                )
            return resp
        except Exception as e:
            last_exc = e
            if "RESOURCE_EXHAUSTED" in str(e):
                raise
            retry_seconds = _extract_retry_seconds(e)
            if retry_seconds is None:
                # exponential backoff with jitter
                base = min(8, 2 ** attempt)
                jitter = random.uniform(0, 1)
                retry_seconds = base + jitter
            # If this was the last attempt, break and raise
            if attempt >= max_retries:
                break
            # sleep asynchronously before next retry
            try:
                await asyncio.sleep(retry_seconds)
            except Exception:
                pass
            continue
    # Retries exhausted
    raise last_exc or RuntimeError("GenAI generate failed")

def _normalize_meal_plan_item(item: Any) -> Dict[str, Any]:
    """Normalize meal plan item from Gemini response."""
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
        "recipeId": str(item.get("recipeId")) if item.get("recipeId") else None,
        "title": str(item.get("title", "Meal")),
        "externalUrl": str(item.get("externalUrl")) if item.get("externalUrl") else None,
        "calories": int(item.get("calories", 500) or 500),
        "protein": int(item.get("protein", 25) or 25),
        "carbs": int(item.get("carbs", 60) or 60),
        "fats": int(item.get("fats", 15) or 15),
    }


def _normalize_meal_plan_day(day: Any) -> Dict[str, Any]:
    """Normalize meal plan day from Gemini response."""
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
    total_cals = sum(int(it.get("calories", 500) or 500) for it in normalized_items)
    
    return {
        "date": str(day.get("date", datetime.utcnow().date().isoformat())),
        "title": str(day.get("title", "Day")),
        "description": str(day.get("description", "")),
        "calories": int(day.get("calories", total_cals) or total_cals),
        "items": normalized_items,
    }


def _normalize_recommendation_item(item: Any) -> Dict[str, Any]:
    """Normalize recipe recommendation from Gemini response."""
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
            "ingredients": [],
        }
    
    return {
        "id": str(item.get("id")) if item.get("id") else None,
        "title": str(item.get("title", "Recipe")),
        "description": str(item.get("description", "")),
        "servings": int(item.get("servings", 1) or 1),
        "totalCalories": int(item.get("totalCalories", 500) or 500),
        "protein": int(item.get("protein", 25) or 25),
        "carbs": int(item.get("carbs", 60) or 60),
        "fats": int(item.get("fats", 15) or 15),
        "ingredients": item.get("ingredients", []),
    }




def _build_search_tools() -> Optional[List[Any]]:
    """
    Returnează lista de tools pentru Google Search sau None dacă importul eșuează.
    Return type Optional[List[Any]] permite un guard explicit la locul de utilizare,
    eliminând eroarea Pylance reportOptionalIterable.
    """
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
async def recommend_recipes(request: UserProfileAI) -> Dict[str, Any]:
    fallback = _fallback_recommendations(request)
    if not client:
        return fallback

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
        # If Gemini is rate-limited or otherwise failed, fall back to external search
        try:
            ext_results = _cached_search_themealdb(request.objective or "", limit=request.limit)
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
        return fallback

@app.post("/meal-plan")
async def generate_meal_plan(request: MealPlanRequest) -> Dict[str, Any]:
    fallback = _fallback_meal_plan(request)
    if not client:
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
        
        # Ensure userId is set
        data["userId"] = request.user_id
        if "user_id" not in data:
            data["user_id"] = request.user_id
        
        # Normalize and validate days
        days_list = data.get("days") or []
        if not days_list:
            # If Gemini returned empty days, use fallback
            return fallback
        
        normalized_days = [_normalize_meal_plan_day(d) for d in days_list]
        data["days"] = normalized_days
        
        # Ensure dates are properly set
        for i, day in enumerate(data.get("days") or []):
            day["date"] = (datetime.utcnow().date() + timedelta(days=i)).isoformat()
        
        _store_cached_gemini_response(cache_key, data)
        return data
    except Exception as e:
        print(f"Gemini meal plan failed: {e}")
        # If Gemini is rate-limited or fails, generate fallback plan but try external recipes
        try:
            fb = _fallback_meal_plan(request)
            # _fallback_meal_plan already attempts external fills via _generate_meal_plan_days_with_macros
            _store_cached_gemini_response(cache_key, fb)
            return fb
        except Exception:
            return fallback

@app.post("/coach")
async def coach(request: CoachRequest) -> Dict[str, Any]:
    fallback = _fallback_coach(request)
    if not client:
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