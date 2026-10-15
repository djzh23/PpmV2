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

Newly registered users have status `Pending` and must be approved by an admin before they can log in. If the email already exists, the response is `409` with title `AUTH_USER_ALREADY_EXISTS`.

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

If the password is valid and the account is not `Approved`, the response is `403` with title `AUTH_NOT_APPROVED`.

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

Error `401` if the token is unknown, expired, or already revoked. If the user is missing or not `Approved`, the response is `403` with title `AUTH_NOT_APPROVED`.

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
  "status": "Pending | Approved | Rejected | Deactivated"
}
```

**Own location assignments** `GET /api/users/me/locations` — Bearer required

Returns the active locations assigned to the current user. Empty array when that user has none.

Response `200`: array of location items (same shape as `GET /api/locations`).

**Staff** `GET /api/users/staff` — Coordinator, Festmitarbeiter, or Honorarkraft

Returns approved Festmitarbeiter and Honorarkraft, with their location assignments.

Optional query: `startAt` and `endAt` (UTC). `hasConflict` is true only when both are set and the person is a non-declined participant of a shift that is not `Cancelled` or `Completed` and overlaps `[startAt, endAt)`.

Response `200`: array of:
```json
{
  "userId": "guid",
  "firstname": "string",
  "lastname": "string",
  "role": "Festmitarbeiter | Honorarkraft",
  "locations": [{ "id": "guid", "name": "string", "district": "string" }],
  "hasConflict": false
}
```

**Update location assignments** `PUT /api/users/{userId}/locations` — Coordinator or Admin

Replaces that user's assignments. Only Festmitarbeiter and Honorarkraft can be assigned. Ids that are not existing locations are ignored. An empty `locationIds` array clears the assignments.

Request:
```json
{ "locationIds": ["guid"] }
```

Response `204 No Content`.

`404` if the user does not exist. `400` if the role is not Festmitarbeiter or Honorarkraft.

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
  "capacity": "number or null",
  "notes": "string or null",
  "isActive": true,
  "createdAt": "ISO 8601 UTC",
  "updatedAt": "ISO 8601 UTC or null"
}
```

**Create location** `POST /api/locations` — Coordinator or Admin

Request: `name`, `district`, `address`, `description`, `photoUrl`, `contactPerson`, `capacity`, `notes`. No `id`, `isActive`, `createdAt`, or `updatedAt`.

Response `201 Created`: location detail shape.

**Update location** `PUT /api/locations/{id}` — Coordinator or Admin

Request: same fields as create. Set `isActive: false` to deactivate, `true` to reactivate.

Response `200`: location detail shape.

**Soft-deactivate** `DELETE /api/locations/{id}` — Coordinator or Admin

Sets `isActive` to `false`. Response `204 No Content`.

**Available staff** `GET /api/locations/{id}/available-staff?date=YYYY-MM-DD` — Bearer required

Returns users with role `Festmitarbeiter` who are assigned to this location and have no overlapping, non-declined participation on that date. `locations` is always `[]` and `hasConflict` is always `false`.

Response `200`: array of:
```json
{
  "userId": "guid",
  "firstname": "string",
  "lastname": "string",
  "role": "Festmitarbeiter",
  "locations": [],
  "hasConflict": false
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
  "location": { "id": "guid", "name": "string", "district": "string", "address": "string or null" },
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

**Create shift** `POST /api/shifts` — Coordinator, Festmitarbeiter, or Honorarkraft

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

**Propose team** `PUT /api/shifts/{id}/propose` — Coordinator, Festmitarbeiter, or Honorarkraft; only the assigned Leader succeeds

Transitions: Draft → PendingApproval. If the leader is the only participant, the status becomes Planned instead.

Response `204 No Content`.

**Approve shift** `PUT /api/shifts/{id}/approve` — Coordinator or Admin

Transitions: Draft or PendingApproval → Planned.

Response `204 No Content`.

**Start shift** `PUT /api/shifts/{id}/start` — only this shift's Leader

Transitions: Planned → Active.

Response `204 No Content`.

**Complete shift** `PUT /api/shifts/{id}/complete` — only this shift's Leader

Transitions: Active → Completed.

Response `204 No Content`.

**Cancel shift** `PUT /api/shifts/{id}/cancel`

Coordinator or Admin may cancel any shift that is not `Completed`. The shift's Leader may cancel a shift in `Draft` or `PendingApproval`.

Response `204 No Content`.

**Add participant** `POST /api/shifts/{id}/participants` — Bearer required; only the shift's Leader

Only `Draft` or `PendingApproval`. The new participant is stored as `Invited`.

Request:
```json
{ "userId": "guid", "role": "Leader | Member | Support" }
```

Response `204 No Content`.

`400` if the caller is not the leader, the shift is in another status, or the user is already a participant. `404` if the shift does not exist.

**Respond to invitation** `PUT /api/shifts/{id}/participants/{userId}/respond` — Bearer required

`userId` must match the logged-in user; otherwise `403 Forbidden`.

Request:
```json
{ "response": "Accepted" | "Declined" }
```

Response `204 No Content`.

Possible errors: `400` if the shift is not in `PendingApproval` status, or if the participant has already responded.

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
  "status": "Pending | Approved | Rejected | Deactivated",
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
| User status | `Pending`, `Approved`, `Rejected`, `Deactivated` |
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
| `403` | Insufficient role, or `AUTH_NOT_APPROVED` when login or refresh finds an account that is not approved |
| `404` | Resource not found |
| `409` | `AUTH_USER_ALREADY_EXISTS` when registering an email that already exists |

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
