# StartEvent API - Comprehensive Reporting System Documentation

## Overview
The StartEvent API provides a robust reporting system designed for administrative oversight, business intelligence, and data export capabilities. All reporting endpoints are secured with Admin role authorization and provide detailed insights into sales, users, events, and revenue.

## Base URL
All reporting endpoints are available under the `/api/admin/reports` base path.

## Authentication & Authorization
- **Required Role**: Admin
- **Authorization Header**: `Bearer <JWT_TOKEN>`
- All endpoints require valid JWT token with Admin role

## Available Report Types

### 1. Sales Reports
Track all payment transactions and sales performance.

#### Get Sales Report
```
GET /api/admin/reports/sales?startDate={date}&endDate={date}
```

**Query Parameters:**
- `startDate` (optional): Start date for sales data (default: 12 months ago)
- `endDate` (optional): End date for sales data (default: current date)

**Response Example:**
```json
{
  "totalAmount": 15750.50,
  "totalTransactions": 245,
  "averageTransactionAmount": 64.28,
  "transactions": [
    {
      "date": "2024-01-15T14:30:00Z",
      "eventTitle": "Tech Conference 2024",
      "category": "Technology",
      "organizer": "John Smith",
      "paymentMethod": "CreditCard",
      "amount": 125.00,
      "transactionId": "TXN123456789"
    }
  ],
  "dailyBreakdown": [
    {
      "date": "2024-01-15",
      "amount": 1250.00,
      "transactionCount": 10
    }
  ],
  "paymentMethodBreakdown": [
    {
      "method": "CreditCard",
      "amount": 12500.00,
      "percentage": 79.4
    }
  ]
}
```

#### Export Sales Report as CSV
```
GET /api/admin/reports/sales/export/csv?startDate={date}&endDate={date}
```

Downloads a CSV file with all sales transactions for the specified date range.

### 2. User Reports
Comprehensive user analytics and demographics.

#### Get User Report
```
GET /api/admin/reports/users
```

**Response Example:**
```json
{
  "totalUsers": 1250,
  "activeUsers": 1180,
  "usersByRole": [
    {
      "role": "Customer",
      "count": 1100,
      "percentage": 88.0
    },
    {
      "role": "Organizer",
      "count": 140,
      "percentage": 11.2
    }
  ],
  "registrationTrends": [
    {
      "date": "2024-01",
      "newUsers": 85,
      "cumulativeUsers": 950
    }
  ],
  "users": [
    {
      "id": "user123",
      "fullName": "Alice Johnson",
      "email": "alice@example.com",
      "role": "Customer",
      "createdAt": "2024-01-10T10:00:00Z",
      "status": "Active",
      "eventsOrganized": 0,
      "ticketsPurchased": 5,
      "totalSpent": 450.00,
      "lastLogin": "2024-01-20T15:30:00Z"
    }
  ]
}
```

#### Export User Report as CSV
```
GET /api/admin/reports/users/export/csv
```

Downloads a detailed CSV file with all user information and statistics.

### 3. Event Reports
Detailed event performance and management insights.

#### Get Event Report
```
GET /api/admin/reports/events?startDate={date}&endDate={date}
```

**Query Parameters:**
- `startDate` (optional): Filter events by start date
- `endDate` (optional): Filter events by end date

**Response Example:**
```json
{
  "totalEvents": 150,
  "publishedEvents": 125,
  "draftEvents": 25,
  "categoryBreakdown": [
    {
      "category": "Technology",
      "eventCount": 45,
      "totalRevenue": 25000.00
    }
  ],
  "events": [
    {
      "id": "evt123",
      "title": "Tech Conference 2024",
      "category": "Technology",
      "organizer": "John Smith",
      "venue": "Convention Center",
      "eventDate": "2024-02-15T09:00:00Z",
      "status": "Published",
      "ticketsSold": 150,
      "totalCapacity": 200,
      "occupancyRate": 75.0,
      "revenue": 7500.00,
      "createdAt": "2024-01-01T10:00:00Z"
    }
  ]
}
```

#### Export Event Report as CSV
```
GET /api/admin/reports/events/export/csv?startDate={date}&endDate={date}
```

### 4. Revenue Reports
Financial analysis and revenue tracking.

#### Get Revenue Report
```
GET /api/admin/reports/revenue?startDate={date}&endDate={date}
```

**Response Example:**
```json
{
  "totalRevenue": 125750.50,
  "monthlyRevenue": [
    {
      "month": "2024-01",
      "revenue": 15250.00,
      "transactionCount": 95,
      "averageTransactionValue": 160.53
    }
  ],
  "categoryRevenue": [
    {
      "category": "Technology",
      "revenue": 45000.00,
      "percentage": 35.8,
      "transactionCount": 150
    }
  ],
  "revenueGrowth": [
    {
      "period": "2024-01",
      "revenue": 15250.00,
      "growthRate": 15.5
    }
  ]
}
```

### 5. Dashboard Summary
High-level overview for admin dashboard.

#### Get Dashboard Summary
```
GET /api/admin/reports/dashboard
```

**Response Example:**
```json
{
  "totalUsers": 1250,
  "totalEvents": 150,
  "totalTicketsSold": 2150,
  "totalRevenue": 125750.50,
  "recentActivity": {
    "newUsers": 45,
    "newEvents": 8,
    "recentSales": 125,
    "recentRevenue": 5250.00
  },
  "topEvents": [
    {
      "title": "Annual Tech Summit",
      "ticketsSold": 250,
      "revenue": 12500.00
    }
  ],
  "revenueByCategory": [
    {
      "category": "Technology",
      "revenue": 45000.00,
      "percentage": 35.8
    }
  ]
}
```

#### Export Dashboard as Excel-Compatible CSV
```
GET /api/admin/reports/dashboard/export/excel
```

Downloads a formatted CSV file suitable for Excel import with dashboard metrics.

### 6. Comprehensive JSON Export
Export all reports in a single JSON file.

#### Export All Reports as JSON
```
GET /api/admin/reports/export/json?startDate={date}&endDate={date}
```

Downloads a comprehensive JSON file containing all report types for the specified date range.

## Export Formats

### CSV Exports
- **Encoding**: UTF-8
- **Format**: Standard CSV with headers
- **Excel Compatible**: Yes, use appropriate endpoints for Excel formatting

### JSON Exports
- **Format**: Pretty-printed JSON
- **Naming**: camelCase property names
- **Structure**: Hierarchical with nested objects for complex data

## Error Handling

All endpoints return standard HTTP status codes:

- **200 OK**: Successful request
- **400 Bad Request**: Invalid query parameters
- **401 Unauthorized**: Missing or invalid authentication token
- **403 Forbidden**: Insufficient permissions (non-Admin user)
- **500 Internal Server Error**: Server-side error

**Error Response Format:**
```json
{
  "error": "Error description",
  "details": "Additional error details if available"
}
```

## Rate Limiting & Performance

### Best Practices
1. **Date Range Limits**: For large datasets, limit date ranges to avoid timeouts
2. **Export Limits**: CSV exports are limited to 1000 records for performance
3. **Caching**: Results are not cached - each request generates fresh data
4. **Concurrent Requests**: Limit concurrent report requests to avoid database overload

### Performance Considerations
- Large date ranges may result in slower response times
- Export endpoints may take longer for large datasets
- Consider using pagination for very large result sets

## Security Notes

1. **Admin Only**: All endpoints require Admin role authorization
2. **Sensitive Data**: Reports may contain sensitive user and financial information
3. **Audit Trail**: Consider logging report access for security auditing
4. **Data Privacy**: Ensure compliance with data protection regulations when exporting user data

## Integration Examples

### Using with JavaScript/Fetch
```javascript
const token = 'your-jwt-token';

// Get dashboard summary
const dashboardResponse = await fetch('/api/admin/reports/dashboard', {
    headers: {
        'Authorization': `Bearer ${token}`,
        'Content-Type': 'application/json'
    }
});
const dashboardData = await dashboardResponse.json();

// Export sales report
const exportResponse = await fetch('/api/admin/reports/sales/export/csv?startDate=2024-01-01&endDate=2024-01-31', {
    headers: {
        'Authorization': `Bearer ${token}`
    }
});
const blob = await exportResponse.blob();
```

### Using with C# HttpClient
```csharp
using var client = new HttpClient();
client.DefaultRequestHeaders.Authorization = 
    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

// Get revenue report
var response = await client.GetAsync("/api/admin/reports/revenue");
var content = await response.Content.ReadAsStringAsync();
var revenueReport = JsonSerializer.Deserialize<RevenueReportDto>(content);
```

## Changelog

### Version 1.0 (Current)
- Initial release with all core reporting functionality
- CSV and JSON export capabilities
- Comprehensive dashboard summary
- Full CRUD operations for all report types

## Support & Troubleshooting

### Common Issues
1. **403 Forbidden**: Ensure user has Admin role assigned
2. **Large Exports**: For large datasets, consider breaking into smaller date ranges
3. **Timeout Issues**: Reduce date range scope for better performance

### Contact
For technical support or feature requests, please contact the development team.