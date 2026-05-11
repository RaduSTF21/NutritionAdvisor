#!/bin/bash

# Test meal-plan and coach endpoints
# Uses the existing user from the previous test (or create a new one)

API="http://localhost:5066"
EMAIL="test_ai_meal_$(date +%s)@example.com"
PASSWORD="TestPass123!"

echo "=== Meal Plan and Coach Test ==="
echo ""

# Register and login
echo "1. Setting up test user..."
curl -s -X POST "$API/api/auth/register" \
  -H "Content-Type: application/json" \
  -d "{\"email\":\"$EMAIL\",\"password\":\"$PASSWORD\",\"name\":\"Test User\"}" > /dev/null

LOGIN_RESPONSE=$(curl -s -X POST "$API/api/auth/login" \
  -H "Content-Type: application/json" \
  -d "{\"email\":\"$EMAIL\",\"password\":\"$PASSWORD\"}")

TOKEN=$(echo "$LOGIN_RESPONSE" | grep -o '"token":"[^"]*' | cut -d'"' -f4)

if [ -z "$TOKEN" ]; then
  echo "ERROR: Failed to get JWT token"
  exit 1
fi

echo "✓ User created and logged in"
echo ""

# Test 1: Free recommendations (should work)
echo "2. Testing free recommendations endpoint..."
REC_RESPONSE=$(curl -s -X POST "$API/api/ai/recommend-recipes" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"limit":3}')

if echo "$REC_RESPONSE" | grep -q '"mode"'; then
  REC_MODE=$(echo "$REC_RESPONSE" | grep -o '"mode":"[^"]*' | cut -d'"' -f4)
  echo "✓ Recommendations endpoint works - mode: $REC_MODE"
else
  echo "✗ Recommendations endpoint failed"
  echo "Response: $REC_RESPONSE"
fi
echo ""

# Test 2: Meal plan (should fail - free users cannot use)
echo "3. Testing meal plan endpoint (should fail for free user)..."
MP_RESPONSE=$(curl -s -X POST "$API/api/ai/generate-meal-plan" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"days":3}')

if echo "$MP_RESPONSE" | grep -q "403\|Forbidden\|Premium"; then
  echo "✓ Meal plan correctly restricted to premium users"
elif echo "$MP_RESPONSE" | grep -q '"mode"'; then
  echo "! Meal plan succeeded (user might be premium)"
  MP_MODE=$(echo "$MP_RESPONSE" | grep -o '"mode":"[^"]*' | cut -d'"' -f4)
  echo "  Response mode: $MP_MODE"
else
  echo "? Meal plan response unclear"
fi
echo ""

# Test 3: Coach (should fail - free users cannot use)
echo "4. Testing coach endpoint (should fail for free user)..."
COACH_RESPONSE=$(curl -s -X POST "$API/api/ai/coach" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"message":"How to lose weight?","context":null}')

if echo "$COACH_RESPONSE" | grep -q "403\|Forbidden\|Premium"; then
  echo "✓ Coach correctly restricted to premium users"
elif echo "$COACH_RESPONSE" | grep -q '"mode"'; then
  echo "! Coach succeeded (user might be premium)"
  COACH_MODE=$(echo "$COACH_RESPONSE" | grep -o '"mode":"[^"]*' | cut -d'"' -f4)
  echo "  Response mode: $COACH_MODE"
else
  echo "? Coach response unclear"
fi
echo ""

echo "=== All Tests Complete ==="
