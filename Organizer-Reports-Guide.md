# Organizer Sales and Revenue Reports API

This guide provides comprehensive documentation for the ticket sales and revenue reporting endpoints designed specifically for logged-in organizers.

## Overview

The organizer reporting system allows event organizers to track their business performance through detailed analytics including:

- **Sales Analytics** - Comprehensive sales reports with revenue breakdown
- **Event Performance** - Individual event metrics and performance tracking
- **Revenue Trends** - Monthly revenue patterns and growth analysis
- **Payment Analytics** - Payment method preferences and transaction analysis
- **Dashboard Summary** - Quick overview of all key metrics

## Authentication & Authorization

All endpoints require:
- **Authentication**: Valid JWT token in Authorization header
- **Role**: `Organizer` role (organizers can only access their own data)
- **Format**: `Authorization: Bearer <your_jwt_token>`

The system automatically identifies the organizer from the JWT token's `NameIdentifier` claim.

---

## API Endpoints

### 1. Comprehensive Sales Report

**Endpoint:** `GET /api/Reports/my-sales`

**Purpose:** Get a detailed sales report with revenue breakdown, top events, and category analysis.

**Query Parameters:**
- `startDate` (optional): Filter from date (YYYY-MM-DD)
- `endDate` (optional): Filter to date (YYYY-MM-DD)

**Response Example:**
```json
{
  "success": true,
  "data": {
    "organizerId": "user123",
    "period": {
      "startDate": "2024-01-01T00:00:00Z",
      "endDate": "2024-12-31T23:59:59Z"
    },
    "report": {
      "totalRevenue": 15750.00,
      "totalTicketsSold": 324,
      "totalTransactions": 127,
      "averageTicketPrice": 48.61,
      "topEventsBySales": [
        {
          "eventId": "event123",
          "eventTitle": "Summer Music Festival",
          "eventCategory": "Music",
          "eventDate": "2024-07-15T19:00:00Z",
          "organizerName": "John Doe",
          "revenue": 5200.00,
          "ticketsSold": 85,
          "transactions": 32
        }
      ],
      "salesByCategory": [
        {
          "category": "Music",
          "revenue": 8500.00,
          "ticketsSold": 180,
          "eventCount": 3,
          "averageTicketPrice": 47.22
        }
      ],
      "paymentMethods": {
        "cardPayments": 12000.00,
        "cashPayments": 2500.00,
        "onlinePayments": 1250.00,
        "cardTransactions": 89,
        "cashTransactions": 25,
        "onlineTransactions": 13
      }
    },
    "generatedAt": "2024-09-20T10:30:00Z"
  },
  "message": "Sales report generated successfully"
}
```

---

### 2. Monthly Revenue Breakdown

**Endpoint:** `GET /api/Reports/my-revenue/monthly/{year}`

**Purpose:** Get monthly revenue breakdown for a specific year.

**Path Parameters:**
- `year` (required): Year for the report (e.g., 2024)

**Response Example:**
```json
{
  "success": true,
  "data": {
    "organizerId": "user123",
    "year": 2024,
    "revenueByMonth": {
      "Jan": 1250.00,
      "Feb": 980.00,
      "Mar": 1850.00,
      "Apr": 2100.00,
      "May": 1750.00,
      "Jun": 2200.00,
      "Jul": 2850.00,
      "Aug": 1920.00,
      "Sep": 850.00,
      "Oct": 0.00,
      "Nov": 0.00,
      "Dec": 0.00
    },
    "totalRevenue": 15750.00,
    "generatedAt": "2024-09-20T10:30:00Z"
  },
  "message": "Monthly revenue report generated successfully"
}
```

---

### 3. Event Performance Metrics

**Endpoint:** `GET /api/Reports/my-events/performance`

**Purpose:** Get detailed performance metrics for all events.

**Query Parameters:**
- `startDate` (optional): Filter from date (YYYY-MM-DD)
- `endDate` (optional): Filter to date (YYYY-MM-DD)

**Response Example:**
```json
{
  "success": true,
  "data": {
    "organizerId": "user123",
    "period": {
      "startDate": null,
      "endDate": null
    },
    "events": [
      {
        "eventId": "event123",
        "eventTitle": "Summer Music Festival",
        "eventCategory": "Music",
        "eventDate": "2024-07-15T19:00:00Z",
        "organizerName": "John Doe",
        "revenue": 5200.00,
        "ticketsSold": 85,
        "transactions": 32
      }
    ],
    "totalEvents": 5,
    "totalRevenue": 15750.00,
    "totalTicketsSold": 324,
    "generatedAt": "2024-09-20T10:30:00Z"
  },
  "message": "Event performance report generated successfully"
}
```

---

### 4. Sales by Period

**Endpoint:** `GET /api/Reports/my-sales/by-period`

**Purpose:** Get sales breakdown by different time periods (daily, weekly, monthly).

**Query Parameters:**
- `periodType` (optional): "daily", "weekly", or "monthly" (default: "monthly")
- `startDate` (optional): Filter from date (YYYY-MM-DD)
- `endDate` (optional): Filter to date (YYYY-MM-DD)

**Response Example:**
```json
{
  "success": true,
  "data": {
    "organizerId": "user123",
    "period": {
      "startDate": "2024-09-01T00:00:00Z",
      "endDate": "2024-09-30T23:59:59Z"
    },
    "periodType": "weekly",
    "salesByPeriod": [
      {
        "period": "2024-09-02 (Week)",
        "revenue": 1250.00,
        "ticketsSold": 25,
        "transactions": 12
      },
      {
        "period": "2024-09-09 (Week)",
        "revenue": 1850.00,
        "ticketsSold": 35,
        "transactions": 18
      }
    ],
    "totalRevenue": 3100.00,
    "totalTicketsSold": 60,
    "totalTransactions": 30,
    "generatedAt": "2024-09-20T10:30:00Z"
  },
  "message": "Sales by weekly report generated successfully"
}
```

---

### 5. Payment Methods Analysis

**Endpoint:** `GET /api/Reports/my-payments/methods`

**Purpose:** Get breakdown of payment methods used by customers.

**Query Parameters:**
- `startDate` (optional): Filter from date (YYYY-MM-DD)
- `endDate` (optional): Filter to date (YYYY-MM-DD)

**Response Example:**
```json
{
  "success": true,
  "data": {
    "organizerId": "user123",
    "period": {
      "startDate": null,
      "endDate": null
    },
    "paymentMethods": {
      "cardPayments": 12000.00,
      "cashPayments": 2500.00,
      "onlinePayments": 1250.00,
      "cardTransactions": 89,
      "cashTransactions": 25,
      "onlineTransactions": 13
    },
    "summary": {
      "totalAmount": 15750.00,
      "totalTransactions": 127,
      "cardPercentage": 76.19,
      "cashPercentage": 15.87,
      "onlinePercentage": 7.94
    },
    "generatedAt": "2024-09-20T10:30:00Z"
  },
  "message": "Payment methods report generated successfully"
}
```

---

### 6. Event Summary Dashboard

**Endpoint:** `GET /api/Reports/my-events/summary`

**Purpose:** Get comprehensive dashboard overview with key performance indicators.

**Response Example:**
```json
{
  "success": true,
  "data": {
    "organizerId": "user123",
    "eventSummary": [
      {
        "eventId": "event123",
        "title": "Summer Music Festival",
        "status": "Past",
        "eventDate": "2024-07-15T19:00:00Z",
        "totalCapacity": 150,
        "ticketsSold": 85,
        "salesPercentage": 56.67,
        "revenue": 5200.00
      }
    ],
    "statistics": {
      "totalEvents": 5,
      "publishedEvents": 4,
      "upcomingEvents": 2,
      "pastEvents": 2,
      "totalRevenue": 15750.00,
      "totalTicketsSold": 324,
      "averageCapacityUtilization": 65.5
    },
    "monthlyRevenue": {
      "Jan": 1250.00,
      "Feb": 980.00,
      "Mar": 1850.00
      // ... other months
    },
    "generatedAt": "2024-09-20T10:30:00Z"
  },
  "message": "Event summary dashboard generated successfully"
}
```

---

## Error Handling

### Common Error Responses

**401 Unauthorized:**
```json
{
  "success": false,
  "message": "User ID not found in token"
}
```

**400 Bad Request (Invalid Period Type):**
```json
{
  "success": false,
  "message": "Invalid period type. Valid values: daily, weekly, monthly"
}
```

**403 Forbidden:**
```json
{
  "success": false,
  "message": "You can only view your own reports"
}
```

**500 Internal Server Error:**
```json
{
  "success": false,
  "message": "An error occurred while generating sales report",
  "error": "Detailed error message"
}
```

---

## Usage Examples

### JavaScript/Fetch API

```javascript
// Get comprehensive sales report
const getSalesReport = async (token, startDate = null, endDate = null) => {
  const url = new URL('http://localhost:5000/api/Reports/my-sales');
  if (startDate) url.searchParams.append('startDate', startDate);
  if (endDate) url.searchParams.append('endDate', endDate);

  const response = await fetch(url, {
    headers: {
      'Authorization': `Bearer ${token}`,
      'Content-Type': 'application/json'
    }
  });

  return response.json();
};

// Get monthly revenue
const getMonthlyRevenue = async (token, year) => {
  const response = await fetch(`http://localhost:5000/api/Reports/my-revenue/monthly/${year}`, {
    headers: {
      'Authorization': `Bearer ${token}`,
      'Content-Type': 'application/json'
    }
  });

  return response.json();
};
```

### cURL Examples

```bash
# Get sales report with date range
curl -H "Authorization: Bearer YOUR_TOKEN" \
     "http://localhost:5000/api/Reports/my-sales?startDate=2024-01-01&endDate=2024-12-31"

# Get event performance
curl -H "Authorization: Bearer YOUR_TOKEN" \
     "http://localhost:5000/api/Reports/my-events/performance"

# Get daily sales for September 2024
curl -H "Authorization: Bearer YOUR_TOKEN" \
     "http://localhost:5000/api/Reports/my-sales/by-period?periodType=daily&startDate=2024-09-01&endDate=2024-09-30"
```

---

## Testing

### HTTP File
Use `test-organizer-reports.http` for manual testing with various scenarios.

### Shell Script
Run `test-organizer-reports.sh` for automated endpoint testing:

```bash
# Make executable and run
chmod +x test-organizer-reports.sh
./test-organizer-reports.sh
```

**Before running tests:**
1. Start the API server (`dotnet run`)
2. Create an organizer account
3. Login and get JWT token
4. Update the token in test files

---

## Integration Guide

### Frontend Dashboard Implementation

1. **Login Flow**: Authenticate organizer and store JWT token
2. **Dashboard Overview**: Call `/my-events/summary` for main metrics
3. **Detailed Reports**: Use specific endpoints based on user selection
4. **Date Range Filtering**: Implement date pickers for all relevant endpoints
5. **Visualizations**: Use the structured data for charts and graphs

### Recommended Dashboard Sections

1. **Overview Cards**
   - Total Revenue
   - Total Events
   - Tickets Sold
   - Average Capacity Utilization

2. **Charts & Graphs**
   - Monthly Revenue Trend
   - Sales by Category (Pie Chart)
   - Event Performance (Bar Chart)
   - Payment Methods (Donut Chart)

3. **Data Tables**
   - Top Performing Events
   - Recent Transactions
   - Event Summary List

### Data Refresh Strategy

- **Real-time**: Not required (daily updates sufficient)
- **Caching**: Implement client-side caching for better UX
- **Loading States**: Show loading indicators during API calls
- **Error Handling**: Graceful error messages and retry options

---

## Security Considerations

### Access Control
- ✅ JWT token validation
- ✅ Role-based authorization (Organizer only)
- ✅ Automatic user identification from token
- ✅ Data isolation (organizers see only their data)

### Data Privacy
- ✅ No exposure of other organizers' data
- ✅ Customer information properly filtered
- ✅ Secure token handling

### Performance
- ✅ Efficient database queries with proper indexing
- ✅ Date range filtering to limit data scope
- ✅ Aggregated data to reduce payload size

---

## Support & Troubleshooting

### Common Issues

1. **401 Unauthorized**: Check JWT token validity and expiration
2. **403 Forbidden**: Ensure user has Organizer role
3. **Empty Results**: Check if organizer has created events and sales
4. **Date Range Issues**: Use ISO date format (YYYY-MM-DD)

### Performance Tips

1. Use date range filters for large datasets
2. Cache dashboard data on frontend
3. Implement pagination for large result sets
4. Consider using background jobs for complex reports

---

*This comprehensive reporting system provides organizers with powerful insights into their event business performance, enabling data-driven decisions for growth and optimization.*