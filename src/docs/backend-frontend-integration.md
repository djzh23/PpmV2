# PpmV2 Backend Runtime + Frontend Contract

## 1) How to start the backend

### Localhost (.NET process + Postgres in Docker)

1. Start Postgres:
   - `docker compose up -d postgres`
2. Start API:
   - HTTP only: `dotnet run --project src/PpmV2.Api --launch-profile http`
   - HTTPS + HTTP: `dotnet run --project src/PpmV2.Api --launch-profile https`
3. API base URL:
   - HTTP: `http://localhost:5105`
   - HTTPS: `https://localhost:7129`

### Docker (API + Postgres together)

1. Start both services:
   - `docker compose up --build -d`
2. API base URL:
   - `http://localhost:8080`
3. Useful checks:
   - `docker compose ps`
   - `docker compose logs -f api`

### Render

Backend URL:
- `https://ppmv2-hbb4.onrender.com`

Required environment variables:
- `ASPNETCORE_ENVIRONMENT=Production`
- `Jwt__Issuer=PpmV2`
- `Jwt__Audience=PpmV2`
- `Jwt__Key=<strong-random-secret>`
- `DATABASE_URL=<provided by Render PostgreSQL>` (preferred) or `ConnectionStrings__PostgresConnection`

Optional CORS env overrides:
- `Cors__AllowedOrigins__0=https://your-frontend-domain`
- `Cors__AllowedOrigins__1=https://another-frontend-domain`
- or `Cors__AllowedOriginsCsv=https://a.example.com,https://b.example.com`

## 2) What frontend must know from backend

### Base URLs by environment

- Localhost: `http://localhost:5105` or `https://localhost:7129`
- Docker: `http://localhost:8080`
- Render: `https://ppmv2-hbb4.onrender.com`

### Auth model

- `POST /api/auth/register`:
  - request:
    - `firstname` (string, required)
    - `lastname` (string, required)
    - `email` (string, required, valid email)
    - `password` (string, required, min length 6)
  - response `200`:
    - `userId` (guid)
    - `email` (string)

- `POST /api/auth/login`:
  - request:
    - `email` (string, required)
    - `password` (string, required)
  - response `200`:
    - `token` (JWT string)
    - `userId` (guid)
    - `email` (string)

Use JWT for protected routes:
- header: `Authorization: Bearer <token>`

### Main protected endpoints

- `GET /api/locations`
  - auth required
  - response: array of `{ id, name, district }`

- `POST /api/einsaetze`
  - auth + role required: `Coordinator` or `Festmitarbeiter`
  - request:
    - `title` (string)
    - `description` (string|null)
    - `startAtUtc` (ISO UTC datetime)
    - `endAtUtc` (ISO UTC datetime|null)
    - `locationId` (guid)
    - `participants` array of:
      - `userId` (guid)
      - `role` (`Leader` | `Member` | `Support`)
  - response: `ShiftDetailsDto`

- `GET /api/einsaetze/{id}`
  - auth required
  - response: `ShiftDetailsDto`

- Admin routes (admin role only):
  - `GET /api/admin/users/pending`
  - `GET /api/admin/users/approved`
  - `GET /api/admin/users/rejected`
  - `PUT /api/admin/users/approve/{id}`
  - `PUT /api/admin/users/reject/{id}`
  - `PUT /api/admin/users/{id}/role` with body `{ "role": "Coordinator" }`

### Important enums for UI logic

- User roles: `Admin`, `Coordinator`, `Festmitarbeiter`, `Honorarkraft`
- User status: `Pending`, `Approved`, `Rejected`, `Deactivated`
- Shift status: `Draft`, `Planned`, `Active`, `Completed`, `Cancelled`
- Shift participant role: `Leader`, `Member`, `Support`

### Date/time handling

- Backend expects and returns UTC timestamps.
- Frontend should send ISO-8601 strings with `Z` suffix.
- Frontend can convert UTC to local timezone only at display layer.

### Error response shape

Auth/domain errors use RFC7807-like `ProblemDetails`:
- `status` (number)
- `title` (error code, e.g. `AUTH_INVALID_CREDENTIALS`)
- `detail` (message)
- optional `errors` object with field-specific errors

Validation middleware errors also return `application/problem+json`.

## 3) CORS behavior and configuration

- CORS is allowlist-based and environment-configurable.
- Origins are normalized (trailing slash removed, canonical scheme/host/port).
- Exact allowed origins come from:
  - `Cors:AllowedOrigins`
  - `Cors:AllowedOriginsCsv`
- Optional suffix matching is supported via:
  - `Cors:AllowedOriginHostSuffixes`

Examples:
- `http://localhost:3000`
- `http://localhost:5173`
- `https://ppmv2-next-frontend.vercel.app`

If frontend origin changes, update CORS config/env and redeploy the backend.
