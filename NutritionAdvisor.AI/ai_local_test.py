import json

def _normalize_text(value):
    return (value or "").strip().lower()


def _recipe_text(recipe):
    ingredients = recipe.get("ingredients") or []
    if isinstance(ingredients, list):
        ingredients_text = " ".join(str(item) for item in ingredients)
    else:
        ingredients_text = str(ingredients)

    return " ".join([
        str(recipe.get("title", "")),
        str(recipe.get("description", "")),
        ingredients_text,
        str(recipe.get("goal", "")),
    ]).lower()


def _matches_avoid_list(recipe, avoid_terms):
    text = _recipe_text(recipe)
    for term in avoid_terms:
        normalized = _normalize_text(term)
        if normalized and normalized in text:
            return True
    return False


def _objective_score(recipe, objective):
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


def _fallback_recommendations(request):
    candidates = [
        recipe for recipe in request.get('available_recipes', [])
        if not _matches_avoid_list(recipe, request.get('allergies', []) + request.get('disliked_ingredients', []))
    ]

    if not candidates:
        candidates = request.get('available_recipes', [])[:]

    ranked = sorted(
        candidates,
        key=lambda recipe: (
            _objective_score(recipe, request.get('objective')),
            -int(recipe.get('calories', 0) or 0),
            -int(recipe.get('cooking_time_in_minutes', 0) or 0),
        ),
        reverse=True,
    )

    recommendations = []
    limit = max(1, request.get('limit', 5))
    for recipe in ranked[: limit]:
        recommendations.append(
            {
                "id": str(recipe.get("id", "fallback")),
                "title": str(recipe.get("title", "Suggested recipe")),
                "description": str(recipe.get("description", "")),
                "ingredients": [str(item) for item in (recipe.get("ingredients") or [])][:8],
                "reason": "Selected locally from your available recipes.",
                "premium": False,
                "cooking_time_in_minutes": int(recipe.get("cooking_time_in_minutes", 0) or 0),
            }
        )

    return {
        "mode": "fallback",
        "userId": request.get('user_id') or request.get('userId'),
        "user_id": request.get('user_id') or request.get('userId'),
        "recommendations": [
            dict(r, **{"cookingTimeInMinutes": r.get("cooking_time_in_minutes", r.get("cookingTimeInMinutes", 0))})
            for r in recommendations
        ],
    }


def _fallback_meal_plan(request):
    candidates = [
        recipe for recipe in request.get('available_recipes', [])
        if not _matches_avoid_list(recipe, request.get('allergies', []) + request.get('disliked_ingredients', []))
    ]

    if not candidates:
        candidates = request.get('available_recipes', [])[:]

    ranked = sorted(
        candidates,
        key=lambda recipe: (_objective_score(recipe, request.get('objective')), -int(recipe.get('calories', 0) or 0)),
        reverse=True,
    )

    days = max(1, min(request.get('days', 7), 14))
    if not ranked:
        return {"mode": "fallback", "userId": request.get('user_id') or request.get('userId'), "user_id": request.get('user_id') or request.get('userId'), "summary": "No recipes available.", "days": []}

    day_entries = []
    for index in range(days):
        recipe = ranked[index % len(ranked)]
        day_entries.append(
            {
                "date": f"Day {index + 1}",
                "title": recipe.get("title", f"Plan day {index + 1}"),
                "description": f"Fallback meal suggestion based on {request.get('objective') or 'your profile' }.",
                "calories": int(recipe.get("calories", 0) or 0),
            }
        )

    return {
        "mode": "fallback",
        "userId": request.get('user_id') or request.get('userId'),
        "user_id": request.get('user_id') or request.get('userId'),
        "summary": f"Local meal plan generated for {days} days.",
        "days": day_entries,
    }


def _fallback_coach(request):
    objective = request.get('objective') or "your nutrition goals"
    tips = [
        f"Keep meals aligned with {objective}.",
        "Prefer whole foods and enough protein at each meal.",
    ]
    if request.get('allergies'):
        tips.append(f"Avoid listed allergens: {', '.join(request.get('allergies')[:5])}.")
    if request.get('disliked_ingredients'):
        tips.append(f"Skip disliked ingredients: {', '.join(request.get('disliked_ingredients')[:5])}.")

    return {
        "mode": "fallback",
        "userId": request.get('user_id') or request.get('userId'),
        "user_id": request.get('user_id') or request.get('userId'),
        "answer": f"I can help with {objective}. Focus on steady, sustainable choices.",
        "tips": tips,
    }


if __name__ == '__main__':
    sample = {
        'user_id': 'test-user',
        'objective': 'weight loss',
        'allergies': ['peanuts'],
        'disliked_ingredients': ['bacon'],
        'limit': 3,
        'available_recipes': [
            {'id':'r1','title':'Salata','description':'salata cu pui','ingredients':['pui','salata'],'calories':300,'cooking_time_in_minutes':20},
            {'id':'r2','title':'Paste','description':'paste cu bacon','ingredients':['paste','bacon'],'calories':800,'cooking_time_in_minutes':30},
        ]
    }
    print('---recommendations---')
    print(json.dumps(_fallback_recommendations(sample), indent=2, ensure_ascii=False))
    print('\n---meal plan---')
    print(json.dumps(_fallback_meal_plan(sample), indent=2, ensure_ascii=False))
    print('\n---coach---')
    print(json.dumps(_fallback_coach(sample), indent=2, ensure_ascii=False))
