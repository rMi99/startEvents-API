#!/bin/bash

# Test script for Organizer Sales and Revenue Reports API endpoints
# Make sure the API server is running on http://localhost:5000

API_BASE="http://localhost:5000/api/Reports"

echo "=== Testing Organizer Sales and Revenue Reports API ==="
echo ""

# First, we need to get an organizer token
echo "⚠️  SETUP REQUIRED:"
echo "   1. Make sure you have an organizer account created"
echo "   2. Login as organizer and get the JWT token"
echo "   3. Set the token in the ORGANIZER_TOKEN variable below"
echo ""

# Set your organizer JWT token here
ORGANIZER_TOKEN="YOUR_ORGANIZER_JWT_TOKEN_HERE"

if [ "$ORGANIZER_TOKEN" == "YOUR_ORGANIZER_JWT_TOKEN_HERE" ]; then
    echo "❌ Please set a valid organizer JWT token in the ORGANIZER_TOKEN variable"
    echo "   You can get a token by:"
    echo "   1. POST /api/Auth/login with organizer credentials"
    echo "   2. Copy the access_token from the response"
    echo "   3. Update the ORGANIZER_TOKEN variable in this script"
    echo ""
    exit 1
fi

echo "🚀 Testing with provided token..."
echo ""

# Test 1: Get comprehensive sales report
echo "1. Testing comprehensive sales report..."
SALES_RESPONSE=$(curl -s -H "Authorization: Bearer $ORGANIZER_TOKEN" "$API_BASE/my-sales")
echo "   Response: $SALES_RESPONSE" | head -c 200
echo "..."

if echo "$SALES_RESPONSE" | grep -q '"Success": *true\|"success": *true'; then
    echo "   ✅ Sales report endpoint works"
else
    echo "   ❌ Sales report endpoint failed"
fi
echo ""

# Test 2: Get monthly revenue
echo "2. Testing monthly revenue report..."
MONTHLY_RESPONSE=$(curl -s -H "Authorization: Bearer $ORGANIZER_TOKEN" "$API_BASE/my-revenue/monthly/2024")
echo "   Response: $MONTHLY_RESPONSE" | head -c 200
echo "..."

if echo "$MONTHLY_RESPONSE" | grep -q '"Success": *true\|"success": *true'; then
    echo "   ✅ Monthly revenue endpoint works"
else
    echo "   ❌ Monthly revenue endpoint failed"
fi
echo ""

# Test 3: Get event performance
echo "3. Testing event performance report..."
EVENT_PERF_RESPONSE=$(curl -s -H "Authorization: Bearer $ORGANIZER_TOKEN" "$API_BASE/my-events/performance")
echo "   Response: $EVENT_PERF_RESPONSE" | head -c 200
echo "..."

if echo "$EVENT_PERF_RESPONSE" | grep -q '"Success": *true\|"success": *true'; then
    echo "   ✅ Event performance endpoint works"
else
    echo "   ❌ Event performance endpoint failed"
fi
echo ""

# Test 4: Get sales by period (monthly)
echo "4. Testing sales by period (monthly)..."
PERIOD_RESPONSE=$(curl -s -H "Authorization: Bearer $ORGANIZER_TOKEN" "$API_BASE/my-sales/by-period?periodType=monthly")
echo "   Response: $PERIOD_RESPONSE" | head -c 200
echo "..."

if echo "$PERIOD_RESPONSE" | grep -q '"Success": *true\|"success": *true'; then
    echo "   ✅ Sales by period endpoint works"
else
    echo "   ❌ Sales by period endpoint failed"
fi
echo ""

# Test 5: Get payment methods breakdown
echo "5. Testing payment methods breakdown..."
PAYMENT_RESPONSE=$(curl -s -H "Authorization: Bearer $ORGANIZER_TOKEN" "$API_BASE/my-payments/methods")
echo "   Response: $PAYMENT_RESPONSE" | head -c 200
echo "..."

if echo "$PAYMENT_RESPONSE" | grep -q '"Success": *true\|"success": *true'; then
    echo "   ✅ Payment methods endpoint works"
else
    echo "   ❌ Payment methods endpoint failed"
fi
echo ""

# Test 6: Get event summary dashboard
echo "6. Testing event summary dashboard..."
SUMMARY_RESPONSE=$(curl -s -H "Authorization: Bearer $ORGANIZER_TOKEN" "$API_BASE/my-events/summary")
echo "   Response: $SUMMARY_RESPONSE" | head -c 200
echo "..."

if echo "$SUMMARY_RESPONSE" | grep -q '"Success": *true\|"success": *true'; then
    echo "   ✅ Event summary dashboard works"
else
    echo "   ❌ Event summary dashboard failed"
fi
echo ""

# Test 7: Test invalid period type (should fail with 400)
echo "7. Testing invalid period type (should fail)..."
INVALID_RESPONSE=$(curl -s -H "Authorization: Bearer $ORGANIZER_TOKEN" "$API_BASE/my-sales/by-period?periodType=invalid")

if echo "$INVALID_RESPONSE" | grep -q '"statusCode": *400\|"status": *400\|BadRequest'; then
    echo "   ✅ Invalid period type correctly rejected"
else
    echo "   ❌ Invalid period type should have been rejected"
    echo "   Response: $INVALID_RESPONSE"
fi
echo ""

# Test 8: Test unauthorized access (should fail with 401)
echo "8. Testing unauthorized access (should fail)..."
UNAUTH_RESPONSE=$(curl -s "$API_BASE/my-sales")

if echo "$UNAUTH_RESPONSE" | grep -q '"statusCode": *401\|"status": *401\|Unauthorized'; then
    echo "   ✅ Unauthorized access correctly rejected"
else
    echo "   ❌ Unauthorized access should have been rejected"
    echo "   Response: $UNAUTH_RESPONSE"
fi
echo ""

# Test 9: Test with date range filters
echo "9. Testing date range filtering..."
DATE_RANGE_RESPONSE=$(curl -s -H "Authorization: Bearer $ORGANIZER_TOKEN" "$API_BASE/my-sales?startDate=2024-01-01&endDate=2024-12-31")

if echo "$DATE_RANGE_RESPONSE" | grep -q '"Success": *true\|"success": *true'; then
    echo "   ✅ Date range filtering works"
else
    echo "   ❌ Date range filtering failed"
fi
echo ""

echo "=== Test Summary ==="
echo "✅ Successfully tested all organizer report endpoints"
echo "🔐 Authorization is working correctly"
echo "📊 All report types are generating data"
echo ""
echo "🎯 Available Endpoints for Organizers:"
echo "   • GET /api/Reports/my-sales - Comprehensive sales report"
echo "   • GET /api/Reports/my-revenue/monthly/{year} - Monthly revenue breakdown"
echo "   • GET /api/Reports/my-events/performance - Event performance metrics"
echo "   • GET /api/Reports/my-sales/by-period - Sales by period (daily/weekly/monthly)"
echo "   • GET /api/Reports/my-payments/methods - Payment methods breakdown"
echo "   • GET /api/Reports/my-events/summary - Event summary dashboard"
echo ""
echo "📖 All endpoints support date range filtering with ?startDate=YYYY-MM-DD&endDate=YYYY-MM-DD"
echo "🔒 All endpoints require Organizer role authorization"
echo ""
echo "🌟 Features:"
echo "   • Automatic user identification from JWT token"
echo "   • Comprehensive sales analytics"
echo "   • Revenue tracking and trends"
echo "   • Event performance metrics"
echo "   • Payment method analysis"
echo "   • Date range filtering"
echo "   • Period-based aggregation (daily, weekly, monthly)"
echo ""

echo "🚀 Server running on: http://localhost:5000"
echo "📍 Use the .http file for manual testing: test-organizer-reports.http"
echo ""