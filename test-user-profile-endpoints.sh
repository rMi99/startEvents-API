#!/bin/bash

# API Base URL
API_BASE="http://localhost:5000/api"

echo "=== Testing StartEvent API User Profile Endpoints ==="
echo ""

# Step 1: Login as admin user to get token
echo "Step 1: Logging in as admin user..."
LOGIN_RESPONSE=$(curl -s -X POST ${API_BASE}/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@example.com","password":"Password@123"}')

echo "Login Response:"
echo "$LOGIN_RESPONSE" | jq '.' 2>/dev/null || echo "$LOGIN_RESPONSE"
echo ""

# Extract token from response
TOKEN=$(echo "$LOGIN_RESPONSE" | jq -r '.data.token' 2>/dev/null || echo "")

if [ -z "$TOKEN" ] || [ "$TOKEN" == "null" ]; then
    echo "Failed to extract JWT token from login response"
    exit 1
fi

echo "JWT Token extracted successfully: ${TOKEN:0:50}..."
echo ""

# Step 2: Test GET profile endpoint
echo "Step 2: Testing GET /profile endpoint..."
PROFILE_RESPONSE=$(curl -s -X GET ${API_BASE}/auth/profile \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -w "\n%{http_code}")

HTTP_CODE=$(echo "$PROFILE_RESPONSE" | tail -n1)
RESPONSE_BODY=$(echo "$PROFILE_RESPONSE" | head -n -1)

echo "HTTP Status Code: $HTTP_CODE"
echo "Response Body:"
echo "$RESPONSE_BODY" | jq '.' 2>/dev/null || echo "$RESPONSE_BODY"
echo ""

# Step 3: Test PUT update-name endpoint
echo "Step 3: Testing PUT /update-name endpoint..."
UPDATE_NAME_RESPONSE=$(curl -s -X PUT ${API_BASE}/auth/update-name \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"fullName":"System Administrator Updated"}' \
  -w "\n%{http_code}")

UPDATE_HTTP_CODE=$(echo "$UPDATE_NAME_RESPONSE" | tail -n1)
UPDATE_RESPONSE_BODY=$(echo "$UPDATE_NAME_RESPONSE" | head -n -1)

echo "HTTP Status Code: $UPDATE_HTTP_CODE"
echo "Response Body:"
echo "$UPDATE_RESPONSE_BODY" | jq '.' 2>/dev/null || echo "$UPDATE_RESPONSE_BODY"
echo ""

# Step 4: Test POST reset-password endpoint
echo "Step 4: Testing POST /reset-password endpoint..."
RESET_RESPONSE=$(curl -s -X POST ${API_BASE}/auth/reset-password \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@example.com"}' \
  -w "\n%{http_code}")

RESET_HTTP_CODE=$(echo "$RESET_RESPONSE" | tail -n1)
RESET_RESPONSE_BODY=$(echo "$RESET_RESPONSE" | head -n -1)

echo "HTTP Status Code: $RESET_HTTP_CODE"
echo "Response Body:"
echo "$RESET_RESPONSE_BODY" | jq '.' 2>/dev/null || echo "$RESET_RESPONSE_BODY"
echo ""

# Extract reset token for testing confirmation
RESET_TOKEN=$(echo "$RESET_RESPONSE_BODY" | jq -r '.data.resetToken' 2>/dev/null || echo "")

# Step 5: Test POST confirm-password-reset endpoint (only if reset token available)
if [ ! -z "$RESET_TOKEN" ] && [ "$RESET_TOKEN" != "null" ]; then
    echo "Step 5: Testing POST /confirm-password-reset endpoint..."
    CONFIRM_RESPONSE=$(curl -s -X POST ${API_BASE}/auth/confirm-password-reset \
      -H "Content-Type: application/json" \
      -d "{\"email\":\"admin@example.com\",\"resetToken\":\"$RESET_TOKEN\",\"newPassword\":\"NewPassword@123\"}" \
      -w "\n%{http_code}")

    CONFIRM_HTTP_CODE=$(echo "$CONFIRM_RESPONSE" | tail -n1)
    CONFIRM_RESPONSE_BODY=$(echo "$CONFIRM_RESPONSE" | head -n -1)

    echo "HTTP Status Code: $CONFIRM_HTTP_CODE"
    echo "Response Body:"
    echo "$CONFIRM_RESPONSE_BODY" | jq '.' 2>/dev/null || echo "$CONFIRM_RESPONSE_BODY"
    echo ""
else
    echo "Step 5: Skipping confirm-password-reset test (no reset token available)"
    echo ""
fi

# Step 6: Test unauthorized access (without token)
echo "Step 6: Testing unauthorized access to profile endpoint..."
UNAUTH_RESPONSE=$(curl -s -X GET ${API_BASE}/auth/profile \
  -H "Content-Type: application/json" \
  -w "\n%{http_code}")

UNAUTH_HTTP_CODE=$(echo "$UNAUTH_RESPONSE" | tail -n1)
UNAUTH_RESPONSE_BODY=$(echo "$UNAUTH_RESPONSE" | head -n -1)

echo "HTTP Status Code: $UNAUTH_HTTP_CODE (should be 401)"
echo "Response Body:"
echo "$UNAUTH_RESPONSE_BODY" | jq '.' 2>/dev/null || echo "$UNAUTH_RESPONSE_BODY"
echo ""

# Step 7: Test validation error on update-name
echo "Step 7: Testing validation error on update-name (empty name)..."
VALIDATION_RESPONSE=$(curl -s -X PUT ${API_BASE}/auth/update-name \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"fullName":""}' \
  -w "\n%{http_code}")

VALIDATION_HTTP_CODE=$(echo "$VALIDATION_RESPONSE" | tail -n1)
VALIDATION_RESPONSE_BODY=$(echo "$VALIDATION_RESPONSE" | head -n -1)

echo "HTTP Status Code: $VALIDATION_HTTP_CODE (should be 400)"
echo "Response Body:"
echo "$VALIDATION_RESPONSE_BODY" | jq '.' 2>/dev/null || echo "$VALIDATION_RESPONSE_BODY"
echo ""

echo "=== All User Profile Endpoint Tests Complete ==="