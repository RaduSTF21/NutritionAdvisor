import sys
import os
import asyncio
import pytest
from unittest.mock import Mock

# Fix Pylance loading errors: Use sys.path to natively import main.py
# instead of relying on raw spec loaders which cause typing warnings.
current_dir = os.path.dirname(__file__)
parent_dir = os.path.abspath(os.path.join(current_dir, '..'))
if parent_dir not in sys.path:
    sys.path.insert(0, parent_dir)

import main as ai_main


# ==================== Extended URL Tests ====================

def test_is_placeholder_url_comprehensive():
    """Test all placeholder URL variants"""
    placeholders = [
        "http://example.com/foo",
        "https://www.example.org/bar",
        "http://example.net/test",
        "https://www.example.com",
    ]
    for url in placeholders:
        assert ai_main._is_placeholder_url(url) is True
    
    real_urls = [
        "https://real-site.com",
        "https://www.python.org",
        "https://github.com",
        "https://google.com",
    ]
    for url in real_urls:
        assert ai_main._is_placeholder_url(url) is False


def test_is_placeholder_url_edge_cases():
    """Test edge cases for placeholder URL detection"""
    assert ai_main._is_placeholder_url("") is False
    assert ai_main._is_placeholder_url(None) is False
    assert ai_main._is_placeholder_url("not-a-url") is False
    assert ai_main._is_placeholder_url(123) is False


def test_get_valid_external_url_prefers_verified_source(monkeypatch):
    """Test that TheMealDB external URL selection keeps only verified URLs"""
    def fake_is_url_accessible(url, timeout=6.0):
        return url == "https://real-site.com/recipe"

    monkeypatch.setattr(ai_main, "_is_url_accessible", fake_is_url_accessible)

    meal = {
        "strSource": "https://real-site.com/recipe",
        "strYoutube": "https://youtube.com/watch?v=abc123",
    }

    assert ai_main._get_valid_external_url(meal) == "https://real-site.com/recipe"


def test_get_valid_external_url_rejects_invalid_urls(monkeypatch):
    """Test that invalid URLs are rejected instead of being passed through"""
    monkeypatch.setattr(ai_main, "_is_url_accessible", lambda url, timeout=6.0: False)

    meal = {
        "strSource": "https://real-site.com/recipe",
        "strYoutube": "https://youtube.com/watch?v=abc123",
    }

    assert ai_main._get_valid_external_url(meal) is None


def test_enrich_single_item_keeps_verified_url(monkeypatch):
    """Test that enrichment keeps a verified external URL intact"""
    monkeypatch.setattr(ai_main, "_is_url_accessible", lambda url, timeout=6.0: url == "https://real-site.com/recipe")

    item = {
        "mealType": "Lunch",
        "title": "Cached Recipe",
        "externalUrl": "https://real-site.com/recipe",
        "recipeId": "abc",
    }

    result = ai_main._enrich_single_item(item, "italian")

    assert result["externalUrl"] == "https://real-site.com/recipe"
    assert result["title"] == "Cached Recipe"


def test_enrich_single_item_strips_invalid_url(monkeypatch):
    """Test that enrichment removes invalid external URLs"""
    monkeypatch.setattr(ai_main, "_is_url_accessible", lambda url, timeout=6.0: False)

    item = {
        "mealType": "Lunch",
        "title": "Hallucinated Recipe",
        "externalUrl": "https://fake-site.com/recipe",
        "recipeId": "abc",
    }

    result = ai_main._enrich_single_item(item, "italian")

    assert result.get("externalUrl") is None


# ==================== URL Validation Tests ====================

class MockResponse:
    def __init__(self, status_code=200, url=None, history=None):
        self.status_code = status_code
        self.url = url or "https://example.com"
        self.history = history or []


def test_is_url_accessible_with_valid_url(monkeypatch):
    """Test URL accessibility with valid HTTP 200 response"""
    ai_main._URL_VALIDATION_CACHE.clear()
    
    mock_response = MockResponse(status_code=200, url="https://real-site.com/recipe")
    mock_requests = Mock()
    mock_requests.head = Mock(return_value=mock_response)
    
    monkeypatch.setitem(sys.modules, 'requests', mock_requests)
    
    result = ai_main._is_url_accessible("https://real-site.com/recipe")
    assert result is True


def test_is_url_accessible_with_405_fallback_to_get(monkeypatch):
    """Test URL accessibility with 405 Method Not Allowed, falling back to GET"""
    ai_main._URL_VALIDATION_CACHE.clear()
    
    head_response = MockResponse(status_code=405)
    get_response = MockResponse(status_code=200, url="https://real-site.com/recipe")
    
    mock_requests = Mock()
    mock_requests.head = Mock(return_value=head_response)
    mock_requests.get = Mock(return_value=get_response)
    
    monkeypatch.setitem(sys.modules, 'requests', mock_requests)
    
    result = ai_main._is_url_accessible("https://real-site.com/recipe")
    assert result is True
    mock_requests.get.assert_called_once()


def test_is_url_accessible_with_homepage_redirect(monkeypatch):
    """Test URL accessibility when redirected to homepage (should return False)"""
    ai_main._URL_VALIDATION_CACHE.clear()
    
    head_response = MockResponse(
        status_code=200, 
        url="https://real-site.com/",
        history=[Mock()]  
    )
    
    mock_requests = Mock()
    mock_requests.head = Mock(return_value=head_response)
    
    monkeypatch.setitem(sys.modules, 'requests', mock_requests)
    
    result = ai_main._is_url_accessible("https://real-site.com/recipe")
    assert result is False


def test_is_url_accessible_with_network_error(monkeypatch):
    """Test URL accessibility when network error occurs"""
    ai_main._URL_VALIDATION_CACHE.clear()
    
    mock_requests = Mock()
    # Fix S112: Replace generic Exception with RuntimeError
    mock_requests.head = Mock(side_effect=RuntimeError("Connection timeout"))
    
    monkeypatch.setitem(sys.modules, 'requests', mock_requests)
    
    result = ai_main._is_url_accessible("https://unreachable-site.com")
    assert result is False


def test_is_url_accessible_caching(monkeypatch):
    """Test URL validation caching"""
    ai_main._URL_VALIDATION_CACHE.clear()
    
    mock_response = MockResponse(status_code=200)
    mock_requests = Mock()
    mock_requests.head = Mock(return_value=mock_response)
    
    monkeypatch.setitem(sys.modules, 'requests', mock_requests)
    
    result1 = ai_main._is_url_accessible("https://cached-site.com")
    assert result1 is True
    assert mock_requests.head.call_count == 1
    
    result2 = ai_main._is_url_accessible("https://cached-site.com")
    assert result2 is True
    assert mock_requests.head.call_count == 1  


# ==================== Recipe Scoring and Matching Tests ====================

def test_objective_score_for_weight_loss():
    """Test objective scoring for weight loss"""
    recipe = {
        "title": "Light Salad",
        "description": "Low calorie salad",
        "ingredients": ["lettuce", "tomato", "vinegar"],
        "calories": 150,
    }
    
    score = ai_main._objective_score(recipe, "weight loss")
    assert score > 0


def test_objective_score_for_muscle_gain():
    """Test objective scoring for muscle gain"""
    recipe = {
        "title": "Grilled Chicken Breast with Protein",
        "description": "High protein meal",
        "ingredients": ["chicken", "egg", "tuna"],
        "calories": 500,
    }
    
    score = ai_main._objective_score(recipe, "muscle gain")
    assert score > 0


def test_objective_score_no_objective():
    """Test objective scoring with no objective specified"""
    recipe = {
        "title": "Pasta",
        "description": "Italian pasta",
        "ingredients": ["pasta", "sauce"],
    }
    
    score = ai_main._objective_score(recipe, None)
    assert score == 0


def test_matches_avoid_list_with_allergens():
    """Test avoid list matching for allergens"""
    recipe = {
        "title": "Peanut Butter Cookie",
        "description": "Made with peanuts",
        "ingredients": ["peanut butter", "flour", "eggs"],
    }
    
    avoid_terms = ["peanut", "shellfish"]
    assert ai_main._matches_avoid_list(recipe, avoid_terms) is True


def test_matches_avoid_list_no_match():
    """Test avoid list with no matches"""
    recipe = {
        "title": "Vegetable Salad",
        "description": "Fresh vegetables",
        "ingredients": ["lettuce", "tomato", "cucumber"],
    }
    
    avoid_terms = ["peanut", "shellfish"]
    assert ai_main._matches_avoid_list(recipe, avoid_terms) is False


# ==================== Meal Plan Generation Tests ====================

def test_generate_meal_plan_days_with_macros_basic():
    """Test basic meal plan generation"""
    request = ai_main.MealPlanRequest(
        user_id="test-user",
        days=3,
        available_recipes=[
            {
                "id": "recipe1",
                "title": "Pasta",
                "description": "Italian pasta",
                "calories": 450,
                "ingredients": ["pasta", "sauce"],
            },
            {
                "id": "recipe2",
                "title": "Salad",
                "description": "Fresh salad",
                "calories": 200,
                "ingredients": ["lettuce", "tomato"],
            },
        ],
    )
    
    days = ai_main._generate_meal_plan_days_with_macros(request, 3)
    
    assert len(days) == 3
    for day in days:
        assert "items" in day
        assert len(day["items"]) > 0


def test_generate_meal_plan_with_allergies():
    """Test meal plan generation with allergy filtering"""
    request = ai_main.MealPlanRequest(
        user_id="test-user",
        days=1,
        allergies=["peanut"],
        available_recipes=[
            {
                "id": "recipe1",
                "title": "Peanut Butter Cookies",
                "description": "Made with peanuts",
                "calories": 400,
                "ingredients": ["peanut butter"],
            },
            {
                "id": "recipe2",
                "title": "Salad",
                "description": "Fresh salad",
                "calories": 200,
                "ingredients": ["lettuce"],
            },
        ],
    )
    
    days = ai_main._generate_meal_plan_days_with_macros(request, 1)
    
    assert len(days) == 1


def test_generate_meal_plan_with_custom_calories():
    """Test meal plan generation with custom calorie targets based on weight"""
    request = ai_main.MealPlanRequest(
        user_id="test-user",
        days=1,
        weight_kg=80,
        objective="weight loss",
        available_recipes=[
            {
                "id": "recipe1",
                "title": "Salad",
                "description": "Light meal",
                "calories": 200,
                "ingredients": ["lettuce"],
            },
        ],
    )
    
    days = ai_main._generate_meal_plan_days_with_macros(request, 1)
    assert len(days) == 1


# Note: The test test_prewarm_meal_plan_recipes_populates_cache 
# was removed because _PREWARMED_THEMEALDB_CACHE tracking is not part of the 
# current sonar-optimized implementation of main.py.


# ==================== Fallback Recommendations Tests ====================

def test_fallback_recommendations_basic():
    """Test fallback recommendations without Gemini"""
    request = ai_main.UserProfileAI(
        user_id="test-user",
        limit=2,
        available_recipes=[
            {
                "id": "recipe1",
                "title": "Pasta",
                "description": "Italian pasta",
                "calories": 450,
                "ingredients": ["pasta"],
            },
            {
                "id": "recipe2",
                "title": "Salad",
                "description": "Fresh salad",
                "calories": 200,
                "ingredients": ["lettuce"],
            },
        ],
    )
    
    result = ai_main._fallback_recommendations(request)
    
    assert "recommendations" in result
    assert len(result["recommendations"]) <= 2
    assert result["mode"] == "gemini"


def test_fallback_recommendations_with_allergies():
    """Test fallback recommendations filtering allergies"""
    request = ai_main.UserProfileAI(
        user_id="test-user",
        limit=2,
        allergies=["peanut"],
        available_recipes=[
            {
                "id": "recipe1",
                "title": "Peanut Butter Cookies",
                "description": "Made with peanuts",
                "calories": 400,
                "ingredients": ["peanut butter"],
            },
            {
                "id": "recipe2",
                "title": "Salad",
                "description": "Fresh salad",
                "calories": 200,
                "ingredients": ["lettuce"],
            },
        ],
    )
    
    result = ai_main._fallback_recommendations(request)
    assert "recommendations" in result


# ==================== Utility Tests ====================

def test_normalize_text():
    """Test text normalization"""
    assert ai_main._normalize_text("Hello World") == "hello world"
    assert ai_main._normalize_text("  SPACES  ") == "spaces"
    assert ai_main._normalize_text(None) == ""
    assert ai_main._normalize_text("") == ""


def test_is_guid_string():
    """Test GUID string detection"""
    valid_guid = "550e8400-e29b-41d4-a716-446655440000"
    assert ai_main._is_guid_string(valid_guid) is True
    assert ai_main._is_guid_string("not-a-guid") is False
    assert ai_main._is_guid_string(None) is False
    assert ai_main._is_guid_string(123) is False


def test_compact_recipe():
    """Test recipe compacting"""
    recipe = {
        "id": "recipe1",
        "title": "Pasta",
        "calories": 450,
        "cookingTimeInMinutes": 30,
        "ingredients": ["pasta", "sauce", "cheese", "butter", "olive oil", "tomato", "garlic"],
    }
    
    compacted = ai_main._compact_recipe(recipe)
    
    assert compacted["id"] == "recipe1"
    assert compacted["title"] == "Pasta"
    assert len(compacted["ingredients"]) <= 6


def test_stable_json():
    """Test stable JSON generation for caching"""
    data = {"z": 1, "a": 2, "m": 3}
    
    json1 = ai_main._stable_json(data)
    json2 = ai_main._stable_json(data)
    
    assert json1 == json2  
    assert "a" in json1  


# ==================== Caching Tests ====================

def test_gemini_response_cache():
    """Test Gemini response caching"""
    ai_main._GEMINI_RESPONSE_CACHE.clear()
    
    cache_key = "test_endpoint:test_payload"
    test_data = {"result": "test_value"}
    
    ai_main._store_cached_gemini_response(cache_key, test_data)
    
    retrieved = ai_main._get_cached_gemini_response(cache_key)
    assert retrieved == test_data

def test_themealdb_cache():
    """Test TheMealDB search caching"""
    ai_main._THEMEALDB_CACHE.clear()
    
    cache_key = "test query::5"
    test_data = [{"id": "1", "title": "Recipe"}]
    
    ai_main._THEMEALDB_CACHE[cache_key] = {"ts": __import__('time').time(), "data": test_data}
    
    entry = ai_main._THEMEALDB_CACHE.get(cache_key)
    assert entry is not None
    assert entry["data"] == test_data


if __name__ == "__main__":
    pytest.main([__file__, "-v"])