# API Contract — PpmV2

This document describes the API contract that any frontend consuming PpmV2 needs to know.
For setup and deployment instructions see the [README](../../README.md).

---

## Base URLs

| Environment | URL |
|---|---|
| Local (dotnet run) | `http://localhost:5105` |
| Local (Docker) | `http://localhost:8080` |
| Production (Render) | `https://ppmv2-hbb4.onrender.com` |

---

## Authentication

All protected routes require a JWT Bearer token in the `Authorization` header:

```
Authorization: Bearer <token>
```

**Register** `POST /api/auth/register`

Request:
```json
{
  "firstname": "string (required)",
  "lastname": "string (required)",
  "email": "string (required, valid email)",
  "password": "string (required, min 6 chars)"
}
```

Response `200`:
```json
{ "userId": "guid", "email": "string" }
```

Note: newly registered users have status `Pending` and must be approved by an admin before they can log in.

**Login** `POST /api/auth/login`

Request:
```json
{ "email": "string", "password": "string" }
```

Response `200`:
```json
{ "token": "JWT string", "userId": "guid", "email": "string" }
```

---

## Protected Endpoints

**Own profile** `GET /api/users/me` — Bearer required

Response:
```json
{
  "id": "guid",
  "firstname": "string",
  "lastname": "string",
  "email": "string",
  "role": "Coordinator",
  "status": "Approved"
}
```

**Locations** `GET /api/locations` — Bearer required

Response: array of `{ "id": "guid", "name": "string", "district": "string" }`

**Get shift** `GET /api/shifts/{id}` — Bearer required

Response: `ShiftDetailsDto` (see below)

**Create shift** `POST /api/shifts` — Coordinator or Festmitarbeiter role required

Request:
```json
{
  "title": "string",
  "description": "string or null",
  "startAtUtc": "ISO 8601 UTC, e.g. 2026-04-01T20:00:00Z",
  "endAtUtc": "ISO 8601 UTC or null",
  "locationId": "guid",
  "participants": [
    { "userId": "guid", "role": "Leader | Member | Support" }
  ]
}
```

Response: `ShiftDetailsDto`

**ShiftDetailsDto shape:**
```json
{
  "id": "guid",
  "title": "string",
  "description": "string or null",
  "startAtUtc": "ISO 8601 UTC",
  "endAtUtc": "ISO 8601 UTC or null",
  "status": "Draft | Planned | Active | Completed | Cancelled",
  "location": { "id": "guid", "name": "string", "district": "string" },
  "participants": [
    { "userId": "guid", "role": "Leader | Member | Support" }
  ],
  "readiness": "ready | not_ready",
  "missingRequirements": ["leader", "festmitarbeiter"]
}
```

---

## Admin Endpoints

All admin routes require the `Admin` role.

| Method | Route | Description |
|---|---|---|
| `GET` | `/api/admin/users/pending` | List users awaiting approval |
| `GET` | `/api/admin/users/approved` | List approved users |
| `GET` | `/api/admin/users/rejected` | List rejected users |
| `PUT` | `/api/admin/users/approve/{id}` | Approve a user |
| `PUT` | `/api/admin/users/reject/{id}` | Reject a user |
| `PUT` | `/api/admin/users/{id}/role` | Assign a role |

Role assignment body:
```json
{ "role": "Admin | Coordinator | Festmitarbeiter | Honorarkraft" }
```

User list item shape:
```json
{
  "id": "guid",
  "email": "string",
  "firstname": "string",
  "lastname": "string",
  "status": "Pending | Approved | Rejected | Deactivated",
  "role": "Admin | Coordinator | Festmitarbeiter | Honorarkraft"
}
```

---

## Enums

All enums are serialized as strings in API responses.

| Enum | Values |
|---|---|
| User role | `Admin`, `Coordinator`, `Festmitarbeiter`, `Honorarkraft` |
| User status | `Pending`, `Approved`, `Rejected`, `Deactivated` |
| Shift status | `Draft`, `Planned`, `Active`, `Completed`, `Cancelled` |
| Shift participant role | `Leader`, `Member`, `Support` |

---

## Error Responses

All errors follow RFC 7807 (`application/problem+json`):

```json
{
  "status": 400,
  "title": "AUTH_VALIDATION_FAILED",
  "detail": "Validation failed.",
  "instance": "/api/auth/login",
  "errors": {
    "email": ["Email is required."]
  }
}
```

The `errors` field is only present when there are field-level validation errors.

---

## Date and Time

- Backend expects and returns UTC timestamps.
- Frontend sends ISO 8601 strings with `Z` suffix: `2026-04-01T20:00:00Z`.
- Convert UTC to local timezone only at the display layer.

---

## CORS

CORS is allowlist-based. Allowed origins are configured per environment in `appsettings.{Environment}.json` or via environment variables:

```
Cors__AllowedOrigins__0=https://your-frontend-domain
Cors__AllowedOriginsCsv=https://a.example.com,https://b.example.com
```

If the frontend origin changes, update the CORS config and redeploy.
