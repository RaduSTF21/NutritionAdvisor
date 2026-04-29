from fastapi import FastAPI
from pydantic import BaseModel
from typing import List, Optional


app = FastAPI(title="Nutrition AI Service")


class UserProfileAI(BaseModel):
    user_id: str
    objective: Optional[str] = None
    allergies: List[str] = []
    disliked_ingredients: List[str] = []
    limit: int = 5


class MealPlanRequest(BaseModel):
    user_id: str
    objective: Optional[str] = None
    days: int = 7
    allergies: List[str] = []
    disliked_ingredients: List[str] = []


class CoachRequest(BaseModel):
    user_id: str
    objective: Optional[str] = None
    message: str
    context: Optional[str] = None
    allergies: List[str] = []
    disliked_ingredients: List[str] = []


MOCK_RECIPES = [
    {"id": 1, "title": "Salată cu pui", "description": "Ușoară și rapidă.", "ingredients": ["pui", "salată", "roșii"], "goal": "weight loss", "calories": 420, "cooking_time_in_minutes": 20},
    {"id": 2, "title": "Paste Carbonara", "description": "Bogată în proteine și energie.", "ingredients": ["paste", "ou", "bacon", "brânză"], "goal": "muscle gain", "calories": 760, "cooking_time_in_minutes": 30},
    {"id": 3, "title": "Smoothie Vegan", "description": "Potrivit pentru gustări rapide.", "ingredients": ["banane", "lapte de migdale", "spanac"], "goal": "weight loss", "calories": 260, "cooking_time_in_minutes": 10},
]


@app.get("/")
def read_root():
    return {"status": "Nutrition AI service running", "message": "Welcome to the Nutrition AI Service!"}


@app.post("/recommend")
def recommend_recipes(user_profile: UserProfileAI):
    objective = (user_profile.objective or "").lower()
    blocked = {item.lower() for item in user_profile.allergies + user_profile.disliked_ingredients}
    recommendations = []

    for recipe in MOCK_RECIPES:
        if objective and recipe["goal"] != objective:
            continue

        if any(ingredient.lower() in blocked for ingredient in recipe["ingredients"]):
            continue

        recommendations.append(
            {
                "id": str(recipe["id"]),
                "title": recipe["title"],
                "description": recipe["description"],
                "ingredients": recipe["ingredients"],
                "reason": "Recomandare generată de motorul AI premium.",
                "premium": True,
                "cooking_time_in_minutes": recipe["cooking_time_in_minutes"],
            }
        )

    return {"user_id": user_profile.user_id, "recommendations": recommendations[: user_profile.limit]}


@app.post("/meal-plan")
def generate_meal_plan(request: MealPlanRequest):
    days = max(1, min(request.days, 14))
    plan_days = []
    for day_index in range(days):
        recipe = MOCK_RECIPES[day_index % len(MOCK_RECIPES)]
        plan_days.append(
            {
                "date": f"day-{day_index + 1}",
                "title": recipe["title"],
                "description": recipe["description"],
                "calories": recipe["calories"],
            }
        )

    return {
        "user_id": request.user_id,
        "summary": f"Plan alimentar generat pentru {days} zile.",
        "days": plan_days,
    }


@app.post("/coach")
def coach(request: CoachRequest):
    objective = request.objective or "același obiectiv"
    answer = f"Pe baza mesajului tău, recomand să menții focusul pe {objective} și să ajustezi porțiile gradual."
    return {
        "user_id": request.user_id,
        "answer": answer,
        "tips": [
            "Hidratează-te constant.",
            "Păstrează un aport echilibrat de proteine.",
            "Evită ingredientele din lista de alergii."
        ],
    }