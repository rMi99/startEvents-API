#!/bin/bash

echo "=== Testing Admin User Creation Endpoint ==="

# Step 1: Login as existing admin to get JWT token
echo "1. Logging in as existing admin..."
LOGIN_RESPONSE=$(curl -s -X POST http://localhost:5000/api/Auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "admin@example.com",
    "password": "Password@123"
  }')

echo "Login Response: $LOGIN_RESPONSE"
echo ""

# Extract JWT token from login response
JWT_TOKEN=$(echo $LOGIN_RESPONSE | jq -r '.data.token')
echo "JWT Token extracted: ${JWT_TOKEN:0:50}..."
echo ""

# Step 2: Try to create admin user without authentication (should fail)
echo "2. Testing admin creation without authentication (should fail with 401)..."
CREATE_ADMIN_NO_AUTH=$(curl -s -X POST http://localhost:5000/api/Auth/create-admin \
  -H "Content-Type: application/json" \
  -d '{
    "email": "newadmin@example.com",
    "password": "NewAdmin@123",
    "fullName": "New Admin User",
    "address": "123 Admin Street"
  }')

echo "Create Admin Response (No Auth): $CREATE_ADMIN_NO_AUTH"
echo ""

# Step 3: Create admin user with proper authentication (should succeed)
echo "3. Testing admin creation with proper authentication (should succeed)..."
CREATE_ADMIN_WITH_AUTH=$(curl -s -X POST http://localhost:5000/api/Auth/create-admin \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $JWT_TOKEN" \
  -d '{
    "email": "newadmin@example.com",
    "password": "NewAdmin@123",
    "fullName": "New Admin User",
    "address": "123 Admin Street",
    "organizationName": "StartEvent Admin Division"
  }')

echo "Create Admin Response (With Auth): $CREATE_ADMIN_WITH_AUTH"
echo ""

# Step 4: Try to create the same admin user again (should fail with 409)
echo "4. Testing duplicate admin creation (should fail with 409)..."
CREATE_DUPLICATE_ADMIN=$(curl -s -X POST http://localhost:5000/api/Auth/create-admin \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $JWT_TOKEN" \
  -d '{
    "email": "newadmin@example.com",
    "password": "AnotherPassword@123",
    "fullName": "Another Admin User"
  }')

echo "Duplicate Admin Response: $CREATE_DUPLICATE_ADMIN"
echo ""

# Step 5: Test login with the newly created admin user
echo "5. Testing login with newly created admin user..."
NEW_ADMIN_LOGIN=$(curl -s -X POST http://localhost:5000/api/Auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "newadmin@example.com",
    "password": "NewAdmin@123"
  }')

echo "New Admin Login Response: $NEW_ADMIN_LOGIN"
echo ""

echo "=== Testing Complete ==="