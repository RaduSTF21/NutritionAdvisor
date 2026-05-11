#!/bin/bash

# End-to-end test for AI service integration
# This script tests: auth, user creation, AI recommendation endpoint, and response shape

API="http://localhost:5066"
EMAIL="test_ai_$(date +%s)@example.com"
PASSWORD="TestPass123!"

echo "=== AI System End-to-End Test ==="
echo "API URL: $API"
echo ""

# Step 1: Register a user
echo "1. Registering user ($EMAIL)..."
REGISTER_RESPONSE=$(curl -s -X POST "$API/api/auth/register" \
  -H "Content-Type: application/json" \
  -d "{\"email\":\"$EMAIL\",\"password\":\"$PASSWORD\",\"name\":\"Test User\"}")

echo "Response: $REGISTER_RESPONSE"
echo ""

# Step 2: Login to get JWT
echo "2. Logging in to get JWT token..."
LOGIN_RESPONSE=$(curl -s -X POST "$API/api/auth/login" \
  -H "Content-Type: application/json" \
  -d "{\"email\":\"$EMAIL\",\"password\":\"$PASSWORD\"}")

echo "Response: $LOGIN_RESPONSE"
TOKEN=$(echo "$LOGIN_RESPONSE" | grep -o '"token":"[^"]*' | cut -d'"' -f4)
USER_ID=$(echo "$LOGIN_RESPONSE" | grep -o '"userId":"[^"]*' | cut -d'"' -f4)

if [ -z "$TOKEN" ]; then
  echo "ERROR: Failed to get JWT token"
  exit 1
fi

echo "JWT Token: $TOKEN"
echo "User ID: $USER_ID"
echo ""

# Step 3: Call AI recommendation endpoint
echo "3. Calling /api/ai/recommend-recipes with JWT..."
AI_RESPONSE=$(curl -s -X POST "$API/api/ai/recommend-recipes" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"limit":5}')

echo "Response: $AI_RESPONSE"
echo ""

# Step 4: Verify response shape
echo "4. Verifying response contains required fields..."

# Check for 'mode' field
if echo "$AI_RESPONSE" | grep -q '"mode"'; then
  MODE=$(echo "$AI_RESPONSE" | grep -o '"mode":"[^"]*' | head -1 | cut -d'"' -f4)
  echo "✓ mode field found: '$MODE'"
else
  echo "✗ mode field missing"
  exit 1
fi

# Check for 'userId' field
if echo "$AI_RESPONSE" | grep -q '"userId"'; then
  echo "✓ userId field found"
else
  echo "✗ userId field missing"
  exit 1
fi

# Check for 'recommendations' field
if echo "$AI_RESPONSE" | grep -q '"recommendations"'; then
  echo "✓ recommendations field found"
else
  echo "✗ recommendations field missing"
  exit 1
fi

echo ""
echo "=== Test PASSED ==="
echo "AI system is working correctly!"
echo "Response shows mode='$MODE' and contains userId and recommendations."
