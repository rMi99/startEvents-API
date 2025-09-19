#!/bin/bash

# Get customer token
CUSTOMER_TOKEN="eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJuYW1laWQiOiJjZDQxZTM0MC02N2YzLTRhYWUtYjQyOC05NjJhNDRiYzkxOGQiLCJlbWFpbCI6ImN1c3RvbWVyQGV4YW1wbGUuY29tIiwidW5pcXVlX25hbWUiOiJEZW1vIEN1c3RvbWVyIiwianRpIjoiNTFmMTU3MDEtYzZhYy00ODNhLTg0NDEtNmNjNGZmZmY3Yjg2Iiwicm9sZSI6IkN1c3RvbWVyIiwibmJmIjoxNzU4Mjg4MDA5LCJleHAiOjE3NTgyOTE2MDksImlhdCI6MTc1ODI4ODAwOSwiaXNzIjoiU3RhcnRFdmVudEFQSSIsImF1ZCI6IlN0YXJ0RXZlbnRBUElVc2VycyJ9.PEEyfdx4VADDF8c2OA2AEWRvzyhKEdswF_mqpvnZ-zQ"

echo "Testing admin creation with Customer role (should return 403 Forbidden)..."
curl -s -w "\nHTTP_CODE:%{http_code}\n" -X POST http://localhost:5000/api/Auth/create-admin \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $CUSTOMER_TOKEN" \
  -d '{
    "email": "hackadmin@example.com",
    "password": "HackAdmin@123",
    "fullName": "Hacker Admin"
  }'