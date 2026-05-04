import os
import json
import re
from datetime import datetime, timedelta
try:
    from google import genai
except Exception:
    genai = None
from fastapi import FastAPI, HTTPException
from pydantic import BaseModel, ConfigDict
from pydantic.alias_generators import to_camel
from typing import List, Optional, Dict, Any

# --- CONFIGURARE GEMINI ---
# Folosim noul SDK 'google-genai' conform recomandărilor curente
GEMINI_API_KEY = os.getenv("GEMINI_API_KEY")
GEMINI_MODEL = os.getenv("GEMINI_MODEL", "gemini-flash-latest")
client = genai.Client(api_key=GEMINI_API_KEY) if GEMINI_API_KEY else None

if not GEMINI_API_KEY:
    print("WARNING: GEMINI_API_KEY is not set. Requests to Gemini will fail.")
else:
    print(f"Using Gemini model: {GEMINI_MODEL}")

app = FastAPI(title="Nutrition AI Service")

# --- COMPATIBILITATE .NET (Fix 422 Error) ---
# .NET trimite proprietăți cu literă mare sau camelCase (ex: UserId, availableRecipes).
# Această clasă de bază face ca Python să le recunoască automat.
class BaseAiModel(BaseModel):
    model_config = ConfigDict(
        alias_generator=to_camel,  # Convertește 'user_id' în 'userId' pentru validare
        populate_by_name=True,     # Permite popularea folosind ambele variante
    )

# --- MODELE PYDANTIC ACTUALIZATE ---

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


def _normalize_text(value: Optional[str]) -> str:
    return (value or "").strip().lower()


def _recipe_text(recipe: Dict[str, Any]) -> str:
    ingredients = recipe.get("ingredients") or []
    if isinstance(ingredients, list):
        ingredients_text = " ".join(str(item) for item in ingredients)
    else:
        ingredients_text = str(ingredients)

    return " ".join(
        [
            str(recipe.get("title", "")),
            str(recipe.get("description", "")),
            ingredients_text,
            str(recipe.get("goal", "")),
        ]
    ).lower()


def _matches_avoid_list(recipe: Dict[str, Any], avoid_terms: List[str]) -> bool:
    text = _recipe_text(recipe)
    for term in avoid_terms:
        normalized = _normalize_text(term)
        if normalized and normalized in text:
            return True
    return False


def _objective_score(recipe: Dict[str, Any], objective: Optional[str]) -> int:
    text = _recipe_text(recipe)
    score = 0
    objective_text = _normalize_text(objective)
    if not objective_text:
        return score

    keywords = {
        "weight loss": ["light", "salad", "low calorie", "protein", "fit"],
        "muscle": ["protein", "chicken", "beef", "egg", "tuna"],
        "healthy": ["salad", "vegetable", "bowl", "grilled", "fresh"],
        "vegan": ["vegan", "tofu", "lentil", "beans", "chickpea"],
        "vegetarian": ["vegetarian", "cheese", "egg", "yogurt", "salad"],
        "keto": ["keto", "avocado", "egg", "salmon", "cheese"],
    }

    for key, terms in keywords.items():
        if key in objective_text:
            score += sum(1 for term in terms if term in text)

    score += sum(1 for word in objective_text.split() if word and word in text)
    return score


def _fallback_recommendations(request: UserProfileAI) -> Dict[str, Any]:
    candidates = [
        recipe for recipe in request.available_recipes
        if not _matches_avoid_list(recipe, request.allergies + request.disliked_ingredients)
    ]

    if not candidates:
        candidates = request.available_recipes[:]

    ranked = sorted(
        candidates,
        key=lambda recipe: (
            _objective_score(recipe, request.objective),
            -int(recipe.get("calories", 0) or 0),
            -int(recipe.get("cookingTimeInMinutes", 0) or 0),
        ),
        reverse=True,
    )

    recommendations = []
    for recipe in ranked[: max(1, request.limit)]:
        recommendations.append(
            {
                "id": str(recipe.get("id", "fallback")),
                "title": str(recipe.get("title", "Suggested recipe")),
                "description": str(recipe.get("description", "")),
                "ingredients": [str(item) for item in (recipe.get("ingredients") or [])][:8],
                "reason": "Selected locally from your available recipes.",
                "premium": False,
                "cooking_time_in_minutes": int(recipe.get("cookingTimeInMinutes", 0) or 0),
            }
        )

    # Provide both snake_case and camelCase user id for broader compatibility
    return {
        "mode": "fallback",
        "userId": request.user_id,
        "user_id": request.user_id,
        "recommendations": [
            dict(r, **{"cookingTimeInMinutes": r.get("cooking_time_in_minutes", r.get("cookingTimeInMinutes", 0))})
            for r in recommendations
        ],
    }


def _fallback_meal_plan(request: MealPlanRequest) -> Dict[str, Any]:
    candidates = [
        recipe for recipe in request.available_recipes
        if not _matches_avoid_list(recipe, request.allergies + request.disliked_ingredients)
    ]

    if not candidates:
        candidates = request.available_recipes[:]

    ranked = sorted(
        candidates,
        key=lambda recipe: (_objective_score(recipe, request.objective), -int(recipe.get("calories", 0) or 0)),
        reverse=True,
    )

    days = max(1, min(request.days, 14))
    if not ranked:
        return {"mode": "fallback", "userId": request.user_id, "user_id": request.user_id, "summary": "No recipes available.", "days": []}

    day_entries = []
    meal_types = ["Breakfast", "Lunch", "Dinner"]
    for index in range(days):
        recipe = ranked[index % len(ranked)]
        day_entries.append(
            {
                "date": f"Day {index + 1}",
                "title": recipe.get("title", f"Plan day {index + 1}"),
                "description": f"Fallback meal suggestion based on {request.objective or 'your profile'}.",
                "calories": int(recipe.get("calories", 0) or 0),
            }
        )

    return {
        "mode": "fallback",
        "userId": request.user_id,
        "user_id": request.user_id,
        "summary": f"Local meal plan generated for {days} days.",
        "days": day_entries,
    }


def _fallback_coach(request: CoachRequest) -> Dict[str, Any]:
    objective = request.objective or "your nutrition goals"
    tips = [
        f"Keep meals aligned with {objective}.",
        "Prefer whole foods and enough protein at each meal.",
    ]
    if request.allergies:
        tips.append(f"Avoid listed allergens: {', '.join(request.allergies[:5])}.")
    if request.disliked_ingredients:
        tips.append(f"Skip disliked ingredients: {', '.join(request.disliked_ingredients[:5])}.")

    return {
        "mode": "fallback",
        "userId": request.user_id,
        "user_id": request.user_id,
        "answer": f"I can help with {objective}. Focus on steady, sustainable choices.",
        "tips": tips,
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


def _normalize_recommendation_item(item: Any) -> Dict[str, Any]:
    if not isinstance(item, dict):
        return {
            "id": "fallback",
            "title": str(item),
            "description": "",
            "ingredients": [],
            "reason": "Returned by Gemini.",
            "premium": False,
            "cooking_time_in_minutes": 0,
            "cookingTimeInMinutes": 0,
        }

    cooking_time = int(item.get("cooking_time_in_minutes", item.get("cookingTimeInMinutes", 0)) or 0)
    return {
        "id": str(item.get("id", "fallback")),
        "title": str(item.get("title", "Suggested recipe")),
        "description": str(item.get("description", "")),
        "ingredients": [str(x) for x in (item.get("ingredients") or [])],
        "reason": str(item.get("reason", "Returned by Gemini.")),
        "premium": bool(item.get("premium", False)),
        "cooking_time_in_minutes": cooking_time,
        "cookingTimeInMinutes": cooking_time,
    }


def _normalize_day_item(item: Any) -> Dict[str, Any]:
    if not isinstance(item, dict):
        return {
            "date": "Day 1",
            "title": str(item),
            "description": "",
            "calories": 0,
            "items": [],
        }

    items = item.get("items") or []
    return {
        "date": str(item.get("date", "Day 1")),
        "title": str(item.get("title", "Meal")),
        "description": str(item.get("description", "")),
        "calories": int(item.get("calories", 0) or 0),
        "items": [
            {
                "mealType": str(meal.get("mealType", meal.get("meal_type", "Lunch"))),
                "recipeId": str(meal.get("recipeId", meal.get("recipe_id", ""))),
                "title": str(meal.get("title", meal.get("recipeTitle", "Meal"))),
                "calories": int(meal.get("calories", 0) or 0),
            }
            for meal in items
            if isinstance(meal, dict)
        ],
    }


def _build_recipe_backed_meal_plan(request: MealPlanRequest) -> Dict[str, Any]:
    candidates = [
        recipe for recipe in request.available_recipes
        if not _matches_avoid_list(recipe, request.allergies + request.disliked_ingredients)
    ]

    if not candidates:
        candidates = request.available_recipes[:]

    ranked = sorted(
        candidates,
        key=lambda recipe: (
            _objective_score(recipe, request.objective),
            -int(recipe.get("calories", 0) or 0),
        ),
        reverse=True,
    )

    days = max(1, min(request.days, 14))
    meal_types = ["Breakfast", "Lunch", "Dinner"]
    if not ranked:
        return {
            "mode": "fallback",
            "userId": request.user_id,
            "user_id": request.user_id,
            "summary": "No recipes available.",
            "days": [],
        }

    days_payload = []
    recipe_index = 0
    for day_index in range(days):
        day_items = []
        total_calories = 0
        for meal_type in meal_types:
            recipe = ranked[recipe_index % len(ranked)]
            recipe_index += 1
            recipe_calories = int(recipe.get("calories", 0) or 0)
            total_calories += recipe_calories
            day_items.append({
                "mealType": meal_type,
                "recipeId": str(recipe.get("id", "")),
                "title": str(recipe.get("title", "Meal")),
                "calories": recipe_calories,
            })

        days_payload.append({
            "date": (datetime.utcnow().date() + timedelta(days=day_index)).isoformat(),
            "title": f"Day {day_index + 1}",
            "description": f"Meal plan based on your available recipes and {request.objective or 'your profile'}.",
            "calories": total_calories,
            "items": day_items,
        })

    return {
        "mode": "recipe-based",
        "userId": request.user_id,
        "user_id": request.user_id,
        "summary": f"Meal plan generated from {len(ranked)} available recipes.",
        "days": days_payload,
    }

# --- ENDPOINTS ---

@app.get("/")
def read_root():
    return {"status": "Nutrition AI service running powered by Gemini 3.0 Flash"}

@app.post("/recommend")
async def recommend_recipes(request: UserProfileAI):
    fallback = _fallback_recommendations(request)
    if not client:
        return fallback

    prompt = f"""
    Ești un nutriționist AI. Utilizatorul are obiectivul: '{request.objective}'.
    Alergii: {request.allergies}. Ingrediente nedorite: {request.disliked_ingredients}.
    Trebuie să recomanzi {request.limit} rețete.
    Rețete disponibile în baza de date: {json.dumps(request.available_recipes)}.
    
    Reguli:
    1. Folosește rețetele din baza de date DACĂ se potrivesc profilului.
    2. Dacă nu sunt suficiente, inventează altele noi.
    3. Exclude orice rețetă care conține alergeni/ingrediente nedorite.
    4. Returnează STRICT JSON.
    """
    
    try:
        # Utilizăm modelul Gemini 3.0 Flash pentru viteză și logică avansată
        response = client.models.generate_content(
            model=GEMINI_MODEL,
            contents=prompt,
            config={'response_mime_type': 'application/json'}
        )
        data = _parse_json_or_fallback(response.text, fallback)
        if isinstance(data, list):
            data = {"recommendations": data}
        return {
            "mode": data.get("mode", "gemini"),
            "userId": request.user_id,
            "user_id": request.user_id,
            "recommendations": [
                _normalize_recommendation_item(item)
                for item in data.get("recommendations", fallback["recommendations"])
            ],
        }
    except Exception as e:
        print(f"Gemini recommend failed: {type(e).__name__}: {e}")
        return fallback

@app.post("/meal-plan")
async def generate_meal_plan(request: MealPlanRequest):
    # We prefer a deterministic plan that always uses the recipes already in the database.
    # Gemini can still be used for summaries in the future, but the meal composition stays reliable.
    return _build_recipe_backed_meal_plan(request)

@app.post("/coach")
async def coach(request: CoachRequest):
    fallback = _fallback_coach(request)
    if not client:
        return fallback

    prompt = f"""
    Ești un nutriționist AI. Obiectiv: {request.objective}. 
    Întrebare client: "{request.message}".
    
    Returnează JSON: {{ "answer": "text", "tips": ["sfat1", "sfat2"] }}
    """

    try:
        response = client.models.generate_content(
            model=GEMINI_MODEL,
            contents=prompt,
            config={'response_mime_type': 'application/json'}
        )
        data = _parse_json_or_fallback(response.text, fallback)
        if isinstance(data, list):
            data = {"tips": data}
        return {
            "mode": data.get("mode", "gemini"),
            "userId": request.user_id,
            "user_id": request.user_id,
            "answer": data.get("answer", fallback["answer"]),
            "tips": data.get("tips", fallback["tips"])
        }
    except Exception as e:
        print(f"Gemini coach failed: {type(e).__name__}: {e}")
        return fallback