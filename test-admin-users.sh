#!/bin/bash

# API Base URL
API_BASE="http://localhost:5000/api"

echo "=== Testing StartEvent API Admin Users Endpoint ==="
echo ""

# Step 1: Login as admin user
echo "Step 1: Logging in as admin..."
LOGIN_RESPONSE=$(curl -s -X POST ${API_BASE}/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@example.com","password":"Password@123"}')

echo "Login Response:"
echo "$LOGIN_RESPONSE" | jq '.' 2>/dev/null || echo "$LOGIN_RESPONSE"
echo ""

# Extract token from response (assuming the response format we saw earlier)
TOKEN=$(echo "$LOGIN_RESPONSE" | jq -r '.data.token' 2>/dev/null || echo "")

if [ -z "$TOKEN" ] || [ "$TOKEN" == "null" ]; then
    echo "Failed to extract JWT token from login response"
    exit 1
fi

echo "JWT Token extracted: ${TOKEN:0:50}..."
echo ""

# Step 2: Test GET admin-users endpoint
echo "Step 2: Testing GET /admin-users endpoint..."
ADMIN_USERS_RESPONSE=$(curl -s -X GET ${API_BASE}/auth/admin-users \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -w "\n%{http_code}")

# Split the response and status code
HTTP_CODE=$(echo "$ADMIN_USERS_RESPONSE" | tail -n1)
RESPONSE_BODY=$(echo "$ADMIN_USERS_RESPONSE" | head -n -1)

echo "HTTP Status Code: $HTTP_CODE"
echo "Response Body:"
echo "$RESPONSE_BODY" | jq '.' 2>/dev/null || echo "$RESPONSE_BODY"
echo ""

# Step 3: Test without authorization (should fail)
echo "Step 3: Testing without authorization (should return 401)..."
UNAUTHORIZED_RESPONSE=$(curl -s -X GET ${API_BASE}/auth/admin-users \
  -H "Content-Type: application/json" \
  -w "\n%{http_code}")

UNAUTH_HTTP_CODE=$(echo "$UNAUTHORIZED_RESPONSE" | tail -n1)
UNAUTH_RESPONSE_BODY=$(echo "$UNAUTHORIZED_RESPONSE" | head -n -1)

echo "HTTP Status Code: $UNAUTH_HTTP_CODE"
echo "Response Body:"
echo "$UNAUTH_RESPONSE_BODY" | jq '.' 2>/dev/null || echo "$UNAUTH_RESPONSE_BODY"
echo ""

echo "=== Test Complete ==="