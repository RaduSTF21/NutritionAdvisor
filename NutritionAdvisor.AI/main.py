import os
import json
import re
import uuid
import asyncio
import time
import random
from datetime import datetime, timedelta, timezone
from urllib.parse import urlsplit
from typing import List, Optional, Dict, Any, Union, Tuple, Annotated
from fastapi import FastAPI, Query
from pydantic import BaseModel, ConfigDict
from pydantic.alias_generators import to_camel

try:
    from google import genai  # type: ignore[import]
    HAS_GENAI = True
except ImportError:
    genai = None  # type: ignore[assignment]
    HAS_GENAI = False

# --- CONFIGURARE GEMINI ---
_raw_keys = os.getenv("GEMINI_API_KEYS", os.getenv("GEMINI_API_KEY", ""))
GEMINI_API_KEYS = [k.strip() for k in _raw_keys.split(",") if k.strip()]
GEMINI_MODEL = str(os.getenv("GEMINI_MODEL", "gemini-2.0-flash"))
GEMINI_MAX_RETRIES = int(os.getenv("GEMINI_MAX_RETRIES", "0"))
GEMINI_CACHE_TTL_SECONDS = int(os.getenv("GEMINI_CACHE_TTL_SECONDS", "600"))

WEIGHT_LOSS_OBJECTIVE = "weight loss"
JSON_MIME_TYPE = "application/json"

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
    model_config = ConfigDict(alias_generator=to_camel, populate_by_name=True)

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

# --- HELPERS DE BAZA ---
def _normalize_text(value: Optional[str]) -> str:
    return (value or "").strip().lower()

def _is_guid_string(value: Any) -> bool:
    if not value: return False
    try:
        uuid.UUID(str(value))
        return True
    except (ValueError, TypeError, AttributeError):
        return False

def _stable_json(value: Any) -> str:
    return json.dumps(value, sort_keys=True, separators=(",", ":"), ensure_ascii=True)

def _compact_recipe(recipe: Dict[str, Any]) -> Dict[str, Any]:
    ingredients = recipe.get("ingredients") or []
    ingredients = [str(item) for item in ingredients[:6]] if isinstance(ingredients, list) else [str(ingredients)]
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
    if entry and time.time() - entry.get("ts", 0) <= GEMINI_CACHE_TTL_SECONDS:
        return entry.get("data")
    return None

def _store_cached_gemini_response(cache_key: str, data: Dict[str, Any]) -> None:
    _GEMINI_RESPONSE_CACHE[cache_key] = {"ts": time.time(), "data": data}

def _recipe_text(recipe: Dict[str, Any]) -> str:
    ingredients = recipe.get("ingredients") or []
    ingredients_text = " ".join(str(i) for i in ingredients) if isinstance(ingredients, list) else str(ingredients)
    return f"{recipe.get('title', '')} {recipe.get('description', '')} {ingredients_text} {recipe.get('goal', '')}".lower()

def _matches_avoid_list(recipe: Dict[str, Any], avoid_terms: List[str]) -> bool:
    text = _recipe_text(recipe)
    return any(_normalize_text(t) and _normalize_text(t) in text for t in avoid_terms)

def _objective_score(recipe: Dict[str, Any], objective: Optional[str]) -> int:
    text, obj_txt = _recipe_text(recipe), _normalize_text(objective)
    if not obj_txt: return 0
    keywords = {
        WEIGHT_LOSS_OBJECTIVE: ["light", "salad", "low calorie", "protein", "fit"],
        "muscle": ["protein", "chicken", "beef", "egg", "tuna"],
        "healthy": ["salad", "vegetable", "bowl", "grilled", "fresh"],
        "vegan": ["vegan", "tofu", "lentil", "beans", "chickpea"],
    }
    score = sum(sum(1 for term in terms if term in text) for key, terms in keywords.items() if key in obj_txt)
    return score + sum(1 for word in obj_txt.split() if word and word in text)

# --- THEMEALDB HELPERS ---
_THEMEALDB_CACHE: Dict[str, Dict[str, Any]] = {}
_THEMEALDB_CACHE_TTL = int(os.getenv("THEMEALDB_CACHE_TTL", "3600"))

def _parse_ingredients_from_meal(raw: Dict[str, Any]) -> List[str]:
    ing_list = []
    for i in range(1, 21):
        ing, measure = raw.get(f"strIngredient{i}"), raw.get(f"strMeasure{i}")
        if ing and ing.strip():
            ing_list.append(f"{measure.strip()} {ing.strip()}" if measure and measure.strip() else ing.strip())
    return ing_list

def _get_valid_external_url(m: Dict[str, Any]) -> Optional[str]:
    for candidate_url in [m.get("strSource"), m.get("strYoutube")]:
        if candidate_url and not _is_placeholder_url(candidate_url) and _is_url_accessible(candidate_url):
            return candidate_url
    return None

def _map_meal_db_item(m: Dict[str, Any]) -> Dict[str, Any]:
    return {
        "id": m.get("idMeal"), 
        "title": m.get("strMeal"), 
        "description": (m.get("strInstructions") or "")[:400],
        "ingredients": _parse_ingredients_from_meal(m), 
        "sourceUrl": m.get("strSource"), 
        "youtubeUrl": m.get("strYoutube"),
        "externalUrl": _get_valid_external_url(m), 
        "cookingTimeInMinutes": 30
    }

def _search_themealdb(query: Optional[str], limit: int = 5) -> List[Dict[str, Any]]:
    try: import requests
    except ImportError: return []
    try:
        url = f"https://www.themealdb.com/api/json/v1/1/search.php?s={(query or '').strip()}"
        resp = requests.get(url, timeout=6)
        if resp.status_code != 200: return []
        meals = resp.json().get("meals") or []
        return [_map_meal_db_item(m) for m in meals[:limit]]
    except Exception:
        return []

def _cached_search_themealdb(query: Optional[str], limit: int = 5) -> List[Dict[str, Any]]:
    key, now = f"{(query or '').strip().lower()}::{limit}", time.time()
    entry = _THEMEALDB_CACHE.get(key)
    if entry and (now - entry.get("ts", 0) < _THEMEALDB_CACHE_TTL): return entry.get("data", [])
    data = _search_themealdb(query, limit=limit)
    _THEMEALDB_CACHE[key] = {"ts": now, "data": data}
    return data

# --- URL VALIDATION HELPERS ---
_URL_VALIDATION_CACHE: Dict[str, Dict[str, Any]] = {}
_URL_VALIDATION_CACHE_TTL = int(os.getenv("URL_VALIDATION_CACHE_TTL", "86400"))

def _homepage_like_path(parsed_url: Any) -> bool:
    return (getattr(parsed_url, "path", "") or "").strip().lower() in {"", "/", "/home", "/index", "/index.html"}

def _is_placeholder_url(url: Optional[str]) -> bool:
    if not url or not isinstance(url, str): return False
    try: host = urlsplit(url.strip()).netloc.lower()
    except Exception: return False
    if host.startswith("www."): host = host[4:]
    return host in {"example.com", "example.org", "example.net"}

def _perform_http_check(cleaned_url: str, timeout: float) -> bool:
    import requests
    response = requests.head(cleaned_url, timeout=timeout, allow_redirects=True)
    if response.status_code == 405:
        response = requests.get(cleaned_url, timeout=timeout, allow_redirects=True, stream=True)
    parsed_original, parsed_final = urlsplit(cleaned_url), urlsplit(response.url or cleaned_url)
    redirected = bool(response.history) and parsed_original.netloc == parsed_final.netloc and _homepage_like_path(parsed_final) and not _homepage_like_path(parsed_original)
    return 200 <= response.status_code < 300 and not redirected

def _is_url_accessible(url: Optional[str], timeout: float = 6.0) -> bool:
    if not url or not isinstance(url, str) or not url.strip().startswith(("http://", "https://")): return False
    cleaned_url, now = url.strip(), time.time()
    if cleaned_url in _URL_VALIDATION_CACHE and now - _URL_VALIDATION_CACHE[cleaned_url]["ts"] < _URL_VALIDATION_CACHE_TTL:
        return _URL_VALIDATION_CACHE[cleaned_url]["valid"]
    try:
        is_valid = _perform_http_check(cleaned_url, timeout)
        _URL_VALIDATION_CACHE[cleaned_url] = {"ts": now, "valid": is_valid}
        return is_valid
    except Exception:
        _URL_VALIDATION_CACHE[cleaned_url] = {"ts": now, "valid": False}
        return False

# --- RECIPE ENRICHMENT HELPERS ---
def _generate_search_terms(recipe_title: str, objective: Optional[str]) -> List[str]:
    stop_words = {"the", "and", "with", "for", "a", "an", "to", "healthy", "recipe", "recipes"}
    words = [word.strip(".,()[]{}!?:;\"'").lower() for word in str(recipe_title).split()]
    keywords = [w for w in words if len(w) >= 4 and w not in stop_words]
    search_terms = [recipe_title]
    if keywords: search_terms.extend([" ".join(keywords[:4])] + keywords[:6])
    if objective: search_terms.append(objective)
    seen, unique = set(), []
    for term in search_terms:
        cleaned = str(term).strip() if term else ""
        if cleaned and cleaned.lower() not in seen:
            seen.add(cleaned.lower())
            unique.append(cleaned)
    return unique

def _find_verified_candidate(term: str, recipe_title: str) -> Optional[Dict[str, Any]]:
    for candidate in _cached_search_themealdb(term, limit=8):
        for candidate_url in [candidate.get("externalUrl"), candidate.get("sourceUrl"), candidate.get("youtubeUrl")]:
            if candidate_url and not _is_placeholder_url(candidate_url) and _is_url_accessible(candidate_url):
                return {"id": candidate.get("id"), "title": candidate.get("title") or recipe_title, "ingredients": candidate.get("ingredients") or [], "externalUrl": candidate_url}
    return None

def _resolve_verified_external_recipe(recipe_title: str, objective: Optional[str] = None) -> Optional[Dict[str, Any]]:
    for term in _generate_search_terms(recipe_title, objective):
        result = _find_verified_candidate(term, recipe_title)
        if result: return result
    return None

def _enrich_single_item(item: Dict[str, Any], objective: Optional[str]) -> Dict[str, Any]:
    if not isinstance(item, dict): return item
    ext_url = item.get("externalUrl")
    if ext_url and not _is_placeholder_url(ext_url) and _is_url_accessible(ext_url): return item
    resolved = _resolve_verified_external_recipe(str(item.get("title") or item.get("mealType") or "Recipe"), objective)
    if resolved:
        res_id = resolved.get("recipeId")
        item["recipeId"] = item.get("recipeId") if _is_guid_string(item.get("recipeId")) else (_is_guid_string(res_id) and str(res_id) or None)
        item["title"], item["externalUrl"] = resolved.get("title") or item.get("title"), resolved.get("externalUrl")
    else:
        item["externalUrl"] = None
    return item

def _enrich_meal_plan_external_urls(days: List[Dict[str, Any]], objective: Optional[str] = None) -> List[Dict[str, Any]]:
    return [{**day, "items": [_enrich_single_item(it, objective) for it in (day.get("items") or [])]} if isinstance(day, dict) else day for day in days]

# --- FALLBACK & GENERATION LOGIC ---
def _fallback_recommendations(request: UserProfileAI) -> Dict[str, Any]:
    candidates = [r for r in request.available_recipes if not _matches_avoid_list(r, request.allergies + request.disliked_ingredients)] or list(request.available_recipes)
    ranked = sorted(candidates, key=lambda r: (_objective_score(r, request.objective), -int(r.get("calories", 0) or 0)), reverse=True)
    recs = [{"id": str(r.get("id", "fallback")), "title": str(r.get("title", "Recipe")), "ingredients": [str(i) for i in (r.get("ingredients") or [])][:8], "premium": False} for r in ranked[: max(1, request.limit)]]
    if len(recs) < max(1, request.limit):
        for r in _cached_search_themealdb(request.search_query or request.objective or "", limit=request.limit):
            recs.append({"id": str(r.get("id") or "external"), "title": r.get("title") or "Recipe", "ingredients": r.get("ingredients") or [], "externalUrl": r.get("externalUrl"), "premium": False})
    return {"mode": "gemini", "userId": request.user_id, "user_id": request.user_id, "recommendations": recs}

def _determine_daily_target(request: MealPlanRequest) -> int:
    obj_txt = _normalize_text(request.objective)
    if request.weight_kg and request.weight_kg > 0:
        if any(w in obj_txt for w in [WEIGHT_LOSS_OBJECTIVE, "cut", "diet"]): return max(1600, min(2600, int(request.weight_kg * 24)))
        if any(w in obj_txt for w in ["muscle", "bulk", "gain"]): return max(2200, min(3400, int(request.weight_kg * 34)))
        return max(1800, min(3000, int(request.weight_kg * 30)))
    if obj_txt and any(w in obj_txt for w in [WEIGHT_LOSS_OBJECTIVE, "cut", "diet"]): return 1800
    if obj_txt and any(w in obj_txt for w in ["muscle", "bulk", "gain"]): return 2800
    return 2400

def _find_first_available_recipe(recipes: List[Dict[str, Any]], used_today: set, prev_id: Optional[str], ignore_prev: bool = False) -> Optional[Tuple[Dict[str, Any], str]]:
    for r in recipes:
        rid = str(r.get("id", ""))
        if rid not in used_today and (ignore_prev or rid != prev_id):
            return r, rid
    return None

def _select_recipe_for_meal(meal_type: str, safe_recipes: List[Dict[str, Any]], external_recipes: List[Dict[str, Any]], prev_ids: Dict[str, Optional[str]], used_today: set) -> Tuple[Optional[Tuple[str, Dict[str, Any]]], Optional[str]]:
    prev_id = prev_ids.get(meal_type)
    if external_recipes and len(safe_recipes) < 4:
        found = _find_first_available_recipe(external_recipes, used_today, prev_id)
        if found:
            used_today.add(found[1])
            return ("external", found[0]), None

    found = _find_first_available_recipe(safe_recipes, used_today, prev_id)
    if found:
        used_today.add(found[1])
        return ("local", found[0]), found[1]

    found = _find_first_available_recipe(safe_recipes, used_today, prev_id, ignore_prev=True)
    if found:
        used_today.add(found[1])
        return ("local", found[0]), found[1]

    return None, None

def _calculate_macros(cals: int, protein: int) -> Tuple[int, int]:
    carbs = max(50, cals // 12)
    fats = max(10, (cals - protein * 4 - carbs * 4) // 9)
    return carbs, fats

def _get_recipe_details(recipe_type: str, recipe: Dict[str, Any], target_cals: int) -> Tuple[str, Optional[str], int, int]:
    title = str(recipe.get("title", "Meal"))
    ing_count = len(recipe.get("ingredients") or [])
    if recipe_type == "external":
        return title, recipe.get("externalUrl"), max(300, 300 + ing_count * 20), ing_count
    return title, None, int(recipe.get("calories", target_cals) or target_cals), ing_count

def _build_item_from_selected(selected: Optional[Tuple[str, Dict[str, Any]]], recipe_id: Optional[str], target_cals: int, idx: int, meal_type: str) -> Tuple[Dict[str, Any], Optional[str]]:
    if not selected:
        carbs, fats = _calculate_macros(target_cals, 25)
        return {"mealType": meal_type, "recipeId": recipe_id, "title": "Meal", "externalUrl": None, "calories": target_cals, "protein": 25, "carbs": carbs, "fats": fats}, recipe_id or f"ext_{idx}"

    recipe_type, recipe = selected
    title, ext_url, rec_cals, ing_count = _get_recipe_details(recipe_type, recipe, target_cals)
    
    reps = max(1, min(3, int(target_cals / rec_cals))) if 0 < rec_cals < target_cals * 0.75 else 1
    if reps > 1: title = f"{title} (x{reps})"
        
    cals = rec_cals * reps
    protein = max(15, min(40, ing_count * 2)) * reps
    carbs, fats = _calculate_macros(cals, protein)
    current_id = recipe_id or str(recipe.get("id") or f"ext_{idx}")
    
    item = {"mealType": meal_type, "recipeId": recipe_id, "title": title, "externalUrl": ext_url, "calories": int(cals), "protein": int(protein), "carbs": int(carbs), "fats": int(fats)}
    return item, current_id

def _generate_single_day(day_offset: int, meal_types: List[str], safe_recipes: List[Dict[str, Any]], external_recipes: List[Dict[str, Any]], prev_ids: Dict[str, Optional[str]], meal_cals: Dict[str, int]) -> Dict[str, Any]:
    items, day_cals, used_today = [], 0, set()
    for idx, meal_type in enumerate(meal_types):
        selected, sel_id = _select_recipe_for_meal(meal_type, safe_recipes, external_recipes, prev_ids, used_today)
        item, cur_id = _build_item_from_selected(selected, sel_id, meal_cals.get(meal_type, 700), idx, meal_type)
        if cur_id: prev_ids[meal_type] = cur_id
        items.append(item)
        day_cals += int(item.get("calories", 0))
    date_iso = (datetime.now(timezone.utc).date() + timedelta(days=day_offset)).isoformat()
    return {"date": date_iso, "title": f"Day {day_offset + 1}", "calories": day_cals, "items": items}

def _generate_meal_plan_days_with_macros(request: MealPlanRequest, days_count: int) -> List[Dict[str, Any]]:
    meal_types = ["Breakfast", "Lunch", "Dinner"]
    daily_target = _determine_daily_target(request)
    meal_cals = {"Breakfast": int(daily_target * 0.30), "Lunch": int(daily_target * 0.35), "Dinner": int(daily_target * 0.35)}
    safe_recipes = [r for r in request.available_recipes if not _matches_avoid_list(r, request.allergies + request.disliked_ingredients)]
    ext_term = (request.preferred_cuisines[0] if request.preferred_cuisines else None) or request.objective or ""
    external_recipes = _cached_search_themealdb(ext_term, limit=12) if len(safe_recipes) < 4 else []
    prev_ids: Dict[str, Optional[str]] = dict.fromkeys(meal_types)
    return [_generate_single_day(d, meal_types, safe_recipes, external_recipes, prev_ids, meal_cals) for d in range(days_count)]

def _fallback_meal_plan(request: MealPlanRequest) -> Dict[str, Any]:
    days = _enrich_meal_plan_external_urls(_generate_meal_plan_days_with_macros(request, max(1, min(request.days, 14))), request.objective)
    return {"mode": "gemini", "userId": request.user_id, "user_id": request.user_id, "days": days}

def _fallback_coach(request: CoachRequest) -> Dict[str, Any]:
    return {"mode": "fallback", "userId": request.user_id, "user_id": request.user_id, "answer": "Focus on sustainable choices.", "tips": []}

# --- JSON/PROMPT UTILS ---
def _parse_json_or_fallback(raw_text: Optional[str], fallback: Dict[str, Any]) -> Dict[str, Any]:
    if not raw_text: return fallback
    try: return json.loads(raw_text)
    except json.JSONDecodeError:
        match = re.search(r"\{.*\}", raw_text, re.DOTALL)
        if match:
            try: return json.loads(match.group(0))
            except json.JSONDecodeError: pass
    return fallback

def _extract_retry_seconds(exc: Exception) -> Optional[float]:
    try:
        m = re.search(r"(?:Please retry in\s*|retryDelay\W*['\"]?)(\d+(?:\.\d+)?)s", str(exc), re.IGNORECASE)
        return float(m.group(1)) if m else None
    except Exception: return None

def _format_debug_response(data: Any, prompt: str, payload: dict, config: dict, err: Optional[str], text: str) -> dict:
    info = {"prompt": prompt, "compact_payload": payload, "gen_config": config, "model": GEMINI_MODEL}
    if err: info["error"] = err
    else: info["raw_response"] = text[:20000]
    
    if isinstance(data, dict):
        data.setdefault("_debug", {}).update(info)
        return data
    return {"_debug": info, "result": data}

def _compact_recommendation_payload(request: UserProfileAI) -> Dict[str, Any]:
    return {"objective": request.objective, "searchQuery": request.search_query, "allergies": request.allergies[:6], "dislikedIngredients": request.disliked_ingredients[:6], "limit": max(1, min(request.limit, 5)), "availableRecipes": _compact_available_recipes(request.available_recipes, limit=6)}

def _compact_meal_plan_payload(request: MealPlanRequest) -> Dict[str, Any]:
    return {"objective": request.objective, "days": max(1, min(request.days, 7)), "allergies": request.allergies[:6], "dislikedIngredients": request.disliked_ingredients[:6], "weightKg": request.weight_kg, "availableRecipes": _compact_available_recipes(request.available_recipes, limit=6)}

def _build_gemini_prompt(endpoint: str, payload: Dict[str, Any]) -> str:
    base = f"objective={payload.get('objective')}; allergies={payload.get('allergies')}; dislikes={payload.get('dislikedIngredients')};"
    if endpoint == "recommend": return f"Return compact JSON only. {base} limit={payload.get('limit')}; recipes={_stable_json(payload.get('availableRecipes') or [])}. Schema: {{recommendations:[{{title,description,ingredients,externalUrl}}]}}."
    if endpoint == "meal-plan": return f"Return compact JSON only. NO REPEATS. {base} days={payload.get('days')}; weightKg={payload.get('weightKg')}; recipes={_stable_json(payload.get('availableRecipes') or [])}. Schema: {{days:[{{date,title,calories,items:[{{mealType,title,recipeId,externalUrl,calories,protein,carbs,fats}}]}}]}}."
    return f"Return compact JSON only. {base} message={payload.get('message')}. Schema: {{answer,tips:[...]}}."

# --- AI CORE ENGINE ---
_KEY_EXHAUSTION_TRACKER: Dict[str, float] = {}
_KEY_ROTATION_CURSOR = 0

def _rotated_keys(keys: List[str]) -> List[str]:
    global _KEY_ROTATION_CURSOR
    if len(keys) <= 1: return list(keys)
    start = _KEY_ROTATION_CURSOR % len(keys)
    _KEY_ROTATION_CURSOR = (_KEY_ROTATION_CURSOR + 1) % len(keys)
    return keys[start:] + keys[:start]

async def _attempt_genai_call(client, model_name, contents, config):
    async with (_AI_SEMAPHORE or asyncio.Semaphore(1)):
        return await asyncio.to_thread(lambda c=client: c.models.generate_content(model=model_name, contents=contents, config=config))

async def _try_generate_single_key(api_key: str, model_name: str, contents: str, gen_config: Dict[str, Any], max_retries: int) -> Tuple[Any, Optional[Exception], bool]:
    if genai is None:
        raise RuntimeError("GenAI not configured.")
    client = genai.Client(api_key=str(api_key))
    last_exc = None
    for attempt in range(max_retries + 1):
        try:
            res = await _attempt_genai_call(client, model_name, contents, gen_config)
            return res, None, False
        except Exception as e:
            last_exc = e
            err_str = str(e)
            if "RESOURCE_EXHAUSTED" in err_str or "429" in err_str:
                return None, e, True
            if attempt >= max_retries: break
            retry_seconds = _extract_retry_seconds(e) or (min(8, 2 ** attempt) + random.uniform(0, 1))
            try: await asyncio.sleep(retry_seconds)
            except Exception: pass
    return None, last_exc, False

async def _genai_generate_with_retries(contents: str, gen_config: Dict[str, Any], model: Optional[str] = None, max_retries: int = 0) -> Any:
    if genai is None or not GEMINI_API_KEYS: raise RuntimeError("GenAI not configured.")
    model_name = str(model or GEMINI_MODEL)
    last_exc = None
    valid_keys = [k for k in GEMINI_API_KEYS if k not in _KEY_EXHAUSTION_TRACKER or time.time() - _KEY_EXHAUSTION_TRACKER[k] > 3600]
    
    for api_key in _rotated_keys(valid_keys or list(GEMINI_API_KEYS)):
        res, exc, exhausted = await _try_generate_single_key(api_key, model_name, contents, gen_config, max_retries)
        if res: return res
        if exhausted: _KEY_EXHAUSTION_TRACKER[api_key] = time.time()
        last_exc = exc
    raise last_exc or RuntimeError("GenAI generate failed.")

def _safe_int(value: Any, default: int = 0) -> int:
    if isinstance(value, int): return value
    try:
        match = re.search(r'(\d+(?:\.\d+)?)', str(value).strip())
        return int(float(match.group(1))) if match else default
    except Exception: return default

def _normalize_meal_plan_item(item: Any) -> Dict[str, Any]:
    if not isinstance(item, dict): return {"mealType": "Meal", "recipeId": None, "title": str(item), "externalUrl": None, "calories": 500, "protein": 25, "carbs": 60, "fats": 15}
    return {"mealType": str(item.get("mealType", "Meal")), "recipeId": str(item.get("recipeId")) if _is_guid_string(item.get("recipeId")) else None, "title": str(item.get("title", "Meal")), "externalUrl": str(item.get("externalUrl")) if item.get("externalUrl") else None, "calories": _safe_int(item.get("calories", 500), 500), "protein": _safe_int(item.get("protein", 25), 25), "carbs": _safe_int(item.get("carbs", 60), 60), "fats": _safe_int(item.get("fats", 15), 15)}

def _normalize_meal_plan_day(day: Any) -> Dict[str, Any]:
    if not isinstance(day, dict): return {"date": datetime.now(timezone.utc).date().isoformat(), "title": "Day", "description": "", "calories": 1500, "items": []}
    items = [_normalize_meal_plan_item(it) for it in (day.get("items") or [])]
    return {"date": str(day.get("date", datetime.now(timezone.utc).date().isoformat())), "title": str(day.get("title", "Day")), "description": str(day.get("description", "")), "calories": _safe_int(day.get("calories", sum(it["calories"] for it in items)), 1500), "items": items}

def _normalize_recommendation_item(item: Any) -> Dict[str, Any]:
    if not isinstance(item, dict): return {"id": None, "title": str(item), "totalCalories": 500, "externalUrl": None, "ingredients": []}
    return {"id": str(item.get("id")) if item.get("id") else None, "title": str(item.get("title", "Recipe")), "totalCalories": _safe_int(item.get("totalCalories", 500), 500), "externalUrl": str(item.get("externalUrl")) if item.get("externalUrl") else None, "ingredients": item.get("ingredients", [])}

# --- ENDPOINTS ---
@app.get("/")
def read_root() -> Dict[str, str]:
    return {"status": f"Nutrition AI service running powered by {GEMINI_MODEL}"}

def _handle_internet_search(request: UserProfileAI) -> Optional[Dict[str, Any]]:
    if not request.use_internet_search or not (request.search_query or request.objective): return None
    term = request.search_query or request.objective or ""
    results = _cached_search_themealdb(term, limit=request.limit)
    if not results: return None
    return {"mode": "internet", "userId": request.user_id, "recommendations": [_normalize_recommendation_item({"id": r.get("id"), "title": r.get("title"), "ingredients": r.get("ingredients"), "externalUrl": r.get("externalUrl")}) for r in results]}

@app.post("/recommend")
async def recommend_recipes(request: UserProfileAI, debug: Annotated[bool, Query(False)]) -> Dict[str, Any]:
    fallback = _fallback_recommendations(request)
    if genai is None or not GEMINI_API_KEYS: return fallback
    if internet_res := _handle_internet_search(request): return internet_res
    
    payload = _compact_recommendation_payload(request)
    cache_key = _compact_gemini_cache_key("recommend", payload)
    if cached := _get_cached_gemini_response(cache_key): return cached

    prompt = _build_gemini_prompt("recommend", payload)
    config = {"response_mime_type": JSON_MIME_TYPE}
    try:
        resp = await _genai_generate_with_retries(prompt, config, model=GEMINI_MODEL, max_retries=GEMINI_MAX_RETRIES)
        data = _parse_json_or_fallback(resp.text, fallback)
        if debug: data = _format_debug_response(data, prompt, payload, config, None, getattr(resp, "text", ""))
        
        result = {"mode": data.get("mode", "gemini") if isinstance(data, dict) else "gemini", "userId": request.user_id, "recommendations": [_normalize_recommendation_item(it) for it in ((data.get("recommendations") if isinstance(data, dict) else data) or fallback["recommendations"])]}
        _store_cached_gemini_response(cache_key, result)
        return result
    except Exception as e:
        if debug: return _format_debug_response(fallback.copy(), prompt, payload, config, str(e), "")
        return fallback

@app.post("/meal-plan")
async def generate_meal_plan(request: MealPlanRequest, debug: Annotated[bool, Query(False)]) -> Dict[str, Any]:
    fallback = _fallback_meal_plan(request)
    if genai is None or not GEMINI_API_KEYS: return fallback

    payload = _compact_meal_plan_payload(request)
    payload["days"] = max(1, min(request.days, 14))
    cache_key = _compact_gemini_cache_key("meal-plan", payload)
    if cached := _get_cached_gemini_response(cache_key): return cached

    prompt = _build_gemini_prompt("meal-plan", payload)
    config = {"response_mime_type": JSON_MIME_TYPE}
    try:
        resp = await _genai_generate_with_retries(prompt, config, model=GEMINI_MODEL, max_retries=GEMINI_MAX_RETRIES)
        data = _parse_json_or_fallback(resp.text, fallback)
        if debug: data = _format_debug_response(data, prompt, payload, config, None, getattr(resp, "text", ""))
        
        data["userId"] = request.user_id
        days_list = data.get("days") if isinstance(data, dict) else []
        if not days_list: return fallback
        
        days = _enrich_meal_plan_external_urls([_normalize_meal_plan_day(d) for d in days_list], request.objective)
        for i, day in enumerate(days): day["date"] = (datetime.now(timezone.utc).date() + timedelta(days=i)).isoformat()
        
        data["days"] = days
        _store_cached_gemini_response(cache_key, data)
        return data
    except Exception as e:
        if debug: return _format_debug_response(_fallback_meal_plan(request), prompt, payload, config, str(e), "")
        return fallback

@app.post("/coach")
async def coach(request: CoachRequest, debug: Annotated[bool, Query(False)]) -> Dict[str, Any]:
    fallback = _fallback_coach(request)
    if genai is None or not GEMINI_API_KEYS: return fallback

    payload = {"objective": request.objective, "message": request.message[:500], "context": (request.context or "")[:300]}
    cache_key = _compact_gemini_cache_key("coach", payload)
    if cached := _get_cached_gemini_response(cache_key): return cached

    prompt = _build_gemini_prompt("coach", payload)
    config = {"response_mime_type": JSON_MIME_TYPE}
    try:
        resp = await _genai_generate_with_retries(prompt, config, model=GEMINI_MODEL, max_retries=GEMINI_MAX_RETRIES)
        data = _parse_json_or_fallback(resp.text, fallback)
        if debug: data = _format_debug_response(data, prompt, payload, config, None, getattr(resp, "text", ""))
        
        result = {"mode": data.get("mode", "gemini") if isinstance(data, dict) else "gemini", "userId": request.user_id, "answer": data.get("answer", fallback["answer"]) if isinstance(data, dict) else fallback["answer"], "tips": data.get("tips", fallback["tips"]) if isinstance(data, dict) else fallback["tips"]}
        _store_cached_gemini_response(cache_key, result)
        return result
    except Exception: return fallback

async def _prewarm_meal_plan(request: MealPlanRequest):
    payload = _compact_meal_plan_payload(request)
    payload["days"] = max(1, min(request.days, 7))
    try:
        resp = await _genai_generate_with_retries(_build_gemini_prompt("meal-plan", payload), {"response_mime_type": JSON_MIME_TYPE}, model=GEMINI_MODEL, max_retries=GEMINI_MAX_RETRIES)
        if parsed := _parse_json_or_fallback(getattr(resp, "text", None), _fallback_meal_plan(request)):
            if isinstance(parsed, dict): _store_cached_gemini_response(_compact_gemini_cache_key("meal-plan", payload), parsed)
    except Exception: pass

async def _prewarm_recommend(request: MealPlanRequest):
    profile = UserProfileAI(user_id=request.user_id, objective=request.objective, limit=3, available_recipes=request.available_recipes or [])
    payload = _compact_recommendation_payload(profile)
    try:
        resp = await _genai_generate_with_retries(_build_gemini_prompt("recommend", payload), {"response_mime_type": JSON_MIME_TYPE}, model=GEMINI_MODEL, max_retries=GEMINI_MAX_RETRIES)
        if parsed := _parse_json_or_fallback(getattr(resp, "text", None), _fallback_recommendations(profile)):
            if isinstance(parsed, dict): _store_cached_gemini_response(_compact_gemini_cache_key("recommend", payload), parsed)
    except Exception: pass

@app.post("/prewarm")
async def prewarm(request: MealPlanRequest) -> Dict[str, Any]:
    await asyncio.gather(_prewarm_meal_plan(request), _prewarm_recommend(request))
    return {"status": "ok"}