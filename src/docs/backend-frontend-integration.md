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
  "email": "string (required)",
  "password": "string (required)"
}
```

Response `200`:
```json
{ "userId": "guid", "email": "string" }
```

Newly registered users have status `Pending` and must be approved by an admin before they can log in.

---

**Login** `POST /api/auth/login`

Request:
```json
{ "email": "string", "password": "string" }
```

Response `200`:
```json
{
  "token": "JWT string",
  "refreshToken": "string",
  "userId": "guid",
  "email": "string"
}
```

The `token` is a short-lived JWT access token. The `refreshToken` is a single-use opaque token valid for 7 days.

---

**Refresh** `POST /api/auth/refresh`

Rotates the refresh token. The old token is revoked on use.

Request:
```json
{ "refreshToken": "string" }
```

Response `200`:
```json
{
  "token": "JWT string",
  "refreshToken": "string",
  "userId": "guid",
  "email": "string"
}
```

Error `401` if the token is unknown, expired, or already revoked.

---

**Logout** `POST /api/auth/logout`

Revokes the refresh token server-side. Idempotent — succeeds even if the token is already revoked or unknown.

Request:
```json
{ "refreshToken": "string" }
```

Response `204 No Content`.

---

## User Endpoints

**Own profile** `GET /api/users/me` — Bearer required

Response `200`:
```json
{
  "id": "guid",
  "firstname": "string",
  "lastname": "string",
  "email": "string",
  "role": "Admin | Coordinator | Festmitarbeiter | Honorarkraft",
  "status": "Pending | Approved | Rejected"
}
```

**Own location assignments** `GET /api/users/me/locations` — Bearer required

Returns the locations assigned to the current Festmitarbeiter. Empty array for other roles.

Response `200`: array of location items (same shape as `GET /api/locations`).

---

## Location Endpoints

All location endpoints require Bearer token.

**List locations** `GET /api/locations`

Active locations only by default. Coordinators and Admins may pass `?includeInactive=true` to include inactive ones.

Response `200`: array of:
```json
{
  "id": "guid",
  "name": "string",
  "district": "string",
  "isActive": true
}
```

**Location detail** `GET /api/locations/{id}`

Response `200`:
```json
{
  "id": "guid",
  "name": "string",
  "district": "string",
  "address": "string or null",
  "description": "string or null",
  "photoUrl": "string or null",
  "contactPerson": "string or null",
  "capacity": 0,
  "notes": "string or null",
  "isActive": true
}
```

**Create location** `POST /api/locations` — Coordinator or Admin

Request: same fields as location detail (without `id` and `isActive`).

Response `201 Created`: location detail shape.

**Update location** `PUT /api/locations/{id}` — Coordinator or Admin

Request: same fields as create. Set `isActive: false` to deactivate, `true` to reactivate.

Response `200`: location detail shape.

**Soft-deactivate** `DELETE /api/locations/{id}` — Coordinator or Admin

Sets `isActive` to `false`. Response `204 No Content`.

**Available staff** `GET /api/locations/{id}/available-staff?date=YYYY-MM-DD` — Bearer required

Response `200`: array of:
```json
{
  "userId": "guid",
  "firstname": "string",
  "lastname": "string",
  "role": "Festmitarbeiter"
}
```

---

## Shift Endpoints

All shift endpoints require Bearer token.

**List shifts** `GET /api/shifts`

Optional query param: `?status=Draft|PendingApproval|Planned|Active|Completed|Cancelled`

Coordinators and Admins see all shifts. Festmitarbeiter and Honorarkraft see only shifts they are assigned to.

Response `200`: array of:
```json
{
  "id": "guid",
  "title": "string",
  "status": "Draft | PendingApproval | Planned | Active | Completed | Cancelled",
  "startAtUtc": "ISO 8601 UTC",
  "endAtUtc": "ISO 8601 UTC or null",
  "location": { "id": "guid", "name": "string", "district": "string" },
  "participantCount": 0
}
```

**Shift detail** `GET /api/shifts/{id}`

Response `200`:
```json
{
  "id": "guid",
  "title": "string",
  "description": "string or null",
  "startAtUtc": "ISO 8601 UTC",
  "endAtUtc": "ISO 8601 UTC or null",
  "status": "Draft | PendingApproval | Planned | Active | Completed | Cancelled",
  "location": { "id": "guid", "name": "string", "district": "string", "address": "string or null" },
  "participants": [
    {
      "userId": "guid",
      "firstname": "string",
      "lastname": "string",
      "role": "Leader | Member | Support",
      "confirmationStatus": "Invited | Accepted | Declined"
    }
  ],
  "readiness": "ready | not_ready",
  "missingRequirements": ["leader", "festmitarbeiter"]
}
```

**Create shift** `POST /api/shifts` — Coordinator or Festmitarbeiter

Request:
```json
{
  "title": "string",
  "description": "string or null",
  "startAtUtc": "ISO 8601 UTC",
  "endAtUtc": "ISO 8601 UTC or null",
  "locationId": "guid",
  "participants": [
    { "userId": "guid", "role": "Leader | Member | Support" }
  ]
}
```

Response `201 Created`: shift detail shape.

**Propose team** `PUT /api/shifts/{id}/propose` — Coordinator or Festmitarbeiter

Transitions: Draft → PendingApproval. Only the assigned leader can propose.

Response `200`: shift detail shape.

**Approve shift** `PUT /api/shifts/{id}/approve` — Coordinator or Admin

Transitions: Draft or PendingApproval → Planned.

Response `200`: shift detail shape.

**Start shift** `PUT /api/shifts/{id}/start` — Coordinator or Admin

Transitions: Planned → Active.

Response `200`: shift detail shape.

**Complete shift** `PUT /api/shifts/{id}/complete` — Coordinator or Admin

Transitions: Active → Completed.

Response `200`: shift detail shape.

**Cancel shift** `PUT /api/shifts/{id}/cancel` — Coordinator or Admin

Response `200`: shift detail shape.

**Respond to invitation** `PUT /api/shifts/{id}/respond` — Bearer required

Request:
```json
{ "accept": true }
```

Response `204 No Content`.

---

## Admin Endpoints

All admin routes require the `Admin` role.

**List users** — three endpoints:

| Method | Route | Description |
|---|---|---|
| `GET` | `/api/admin/users/pending` | Users awaiting approval |
| `GET` | `/api/admin/users/approved` | Approved users |
| `GET` | `/api/admin/users/rejected` | Rejected users |

Response: array of:
```json
{
  "id": "guid",
  "email": "string",
  "firstname": "string",
  "lastname": "string",
  "status": "Pending | Approved | Rejected",
  "role": "Admin | Coordinator | Festmitarbeiter | Honorarkraft"
}
```

**Approve user** `PUT /api/admin/users/approve/{id}` — Response `204 No Content`

**Reject user** `PUT /api/admin/users/reject/{id}` — Response `204 No Content`

**Assign role** `PUT /api/admin/users/{id}/role`

Request:
```json
{ "role": "Coordinator | Festmitarbeiter | Honorarkraft" }
```

Note: `Admin` cannot be assigned via this endpoint.

Response `204 No Content`.

---

## Enums

All enums are serialized as strings in API responses.

| Enum | Values |
|---|---|
| User role | `Admin`, `Coordinator`, `Festmitarbeiter`, `Honorarkraft` |
| User status | `Pending`, `Approved`, `Rejected` |
| Shift status | `Draft`, `PendingApproval`, `Planned`, `Active`, `Completed`, `Cancelled` |
| Shift participant role | `Leader`, `Member`, `Support` |
| Confirmation status | `Invited`, `Accepted`, `Declined` |

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

| Status | When |
|---|---|
| `400` | Validation error or invalid business operation |
| `401` | Missing or invalid token (including expired/revoked refresh token) |
| `403` | Valid token but insufficient role |
| `404` | Resource not found |

---

## Date and Time

- Backend expects and returns UTC timestamps.
- Frontend sends ISO 8601 strings with `Z` suffix: `2026-04-01T20:00:00Z`.
- Convert UTC to local timezone only at the display layer.

---

## CORS

CORS is allowlist-based. Allowed origins are configured per environment in `appsettings.{Environment}.json`:

```json
"Cors": {
  "AllowedOrigins": ["https://your-frontend.vercel.app"],
  "AllowedOriginHostSuffixes": ["projects.vercel.app"]
}
```

Or via environment variables:
```
Cors__AllowedOrigins__0=https://your-frontend-domain
Cors__AllowedOriginsCsv=https://a.example.com,https://b.example.com
```
