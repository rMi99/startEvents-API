# Venue Management API Documentation

## Overview
The Venue Management API provides endpoints for managing venues within the StartEvent system. These endpoints are restricted to users with **Organizer** or **Admin** roles only.

## Base URL
```
/api/venues
```

## Authentication
All endpoints require JWT Bearer token authentication with either:
- `Organizer` role
- `Admin` role

## Endpoints

### 1. Get All Venues
**GET** `/api/venues`

**Query Parameters:**
- `search` (optional): Search venues by name or location
- `location` (optional): Filter venues by location

**Response:**
```json
[
  {
    "id": "guid",
    "name": "Venue Name",
    "location": "City, Country",
    "capacity": 1000,
    "createdAt": "2025-09-15T10:00:00Z",
    "modifiedAt": "2025-09-15T10:00:00Z",
    "eventCount": 5
  }
]
```

### 2. Get Venue by ID
**GET** `/api/venues/{id}`

**Response:**
```json
{
  "id": "guid",
  "name": "Venue Name",
  "location": "City, Country",
  "capacity": 1000,
  "createdAt": "2025-09-15T10:00:00Z",
  "modifiedAt": "2025-09-15T10:00:00Z",
  "eventCount": 5
}
```

### 3. Create New Venue
**POST** `/api/venues`

**Request Body:**
```json
{
  "name": "New Venue",
  "location": "City, Country",
  "capacity": 1000
}
```

**Response:**
```json
{
  "id": "guid",
  "name": "New Venue",
  "location": "City, Country",
  "capacity": 1000,
  "createdAt": "2025-09-15T10:00:00Z",
  "modifiedAt": "2025-09-15T10:00:00Z",
  "eventCount": 0
}
```

### 4. Update Venue
**PUT** `/api/venues/{id}`

**Request Body:**
```json
{
  "name": "Updated Venue Name",
  "location": "Updated Location",
  "capacity": 1500
}
```

**Response:**
```json
{
  "id": "guid",
  "name": "Updated Venue Name",
  "location": "Updated Location",
  "capacity": 1500,
  "createdAt": "2025-09-15T10:00:00Z",
  "modifiedAt": "2025-09-15T12:30:00Z",
  "eventCount": 5
}
```

### 5. Delete Venue
**DELETE** `/api/venues/{id}`

**Notes:**
- Cannot delete venues that have associated events
- Returns 400 Bad Request if venue has events
- Returns 204 No Content on successful deletion

### 6. Get Venue Event Count
**GET** `/api/venues/{id}/events-count`

**Response:**
```json
5
```

### 7. Search Venues
**GET** `/api/venues/search?term={searchTerm}`

**Query Parameters:**
- `term` (required): Search term for venue name or location

**Response:**
```json
[
  {
    "id": "guid",
    "name": "Matching Venue",
    "location": "City, Country",
    "capacity": 1000,
    "createdAt": "2025-09-15T10:00:00Z",
    "modifiedAt": "2025-09-15T10:00:00Z",
    "eventCount": 3
  }
]
```

## Error Responses

### 400 Bad Request
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "Name": ["The Name field is required."]
  }
}
```

### 401 Unauthorized
```json
{
  "message": "Unauthorized"
}
```

### 403 Forbidden
```json
{
  "message": "Access denied. Organizer or Admin role required."
}
```

### 404 Not Found
```json
{
  "message": "Venue not found"
}
```

## Validation Rules

### CreateVenueDto & UpdateVenueDto
- **Name**: Required, max 200 characters
- **Location**: Required, max 500 characters  
- **Capacity**: Required, minimum value 1

## Features

✅ **Role-based Authorization**: Only Organizers and Admins can access  
✅ **Search Functionality**: Search by venue name or location  
✅ **Location Filtering**: Filter venues by location  
✅ **Event Count Tracking**: Shows number of events per venue  
✅ **Cascade Protection**: Cannot delete venues with associated events  
✅ **Full CRUD Operations**: Create, Read, Update, Delete  
✅ **Input Validation**: Comprehensive data validation  
✅ **Error Handling**: Detailed error responses  

## Usage Examples

### Using with curl

```bash
# Get all venues
curl -H "Authorization: Bearer YOUR_JWT_TOKEN" \
     "http://localhost:5000/api/venues"

# Create a new venue
curl -X POST \
     -H "Authorization: Bearer YOUR_JWT_TOKEN" \
     -H "Content-Type: application/json" \
     -d '{"name":"Concert Hall","location":"New York, NY","capacity":500}' \
     "http://localhost:5000/api/venues"

# Search venues
curl -H "Authorization: Bearer YOUR_JWT_TOKEN" \
     "http://localhost:5000/api/venues/search?term=Concert"
```