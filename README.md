# PpmV2 - Shift and Personnel Management API

[![.NET](https://img.shields.io/badge/.NET_10-5C2D91?style=flat-square&logo=.net&logoColor=white)](https://dotnet.microsoft.com/)
[![ASP.NET Core](https://img.shields.io/badge/ASP.NET_Core-5C2D91?style=flat-square&logo=.net&logoColor=white)](https://docs.microsoft.com/aspnet/core/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-316192?style=flat-square&logo=postgresql&logoColor=white)](https://www.postgresql.org/)
[![Docker](https://img.shields.io/badge/Docker-0db7ed?style=flat-square&logo=docker&logoColor=white)](https://www.docker.com/)
[![xUnit](https://img.shields.io/badge/xUnit-5C2D91?style=flat-square&logo=.net&logoColor=white)](https://xunit.net/)
[![CI](https://github.com/djzh23/PpmV2/actions/workflows/ci.yml/badge.svg)](https://github.com/djzh23/PpmV2/actions/workflows/ci.yml)
[![Live](https://img.shields.io/badge/Live-Render-46E3B7?style=flat-square&logo=render&logoColor=white)](https://ppmv2-hbb4.onrender.com)

REST API for shift planning and personnel management in a volunteer association, built with .NET 10, Clean Architecture, JWT authentication, and PostgreSQL.

**Live Demo:** [ppmv2.vercel.app](https://ppmv2.vercel.app)
**API:** [ppmv2-hbb4.onrender.com](https://ppmv2-hbb4.onrender.com)
**Frontend repo:** [ppmv2-next-frontend](https://github.com/djzh23/ppmv2-next-frontend) (Next.js 15 + shadcn/ui)

---

## Background and Motivation

This project started as a direct outcome of my Bachelor's thesis, where I built the first version of this system as a Laravel REST API with a .NET MAUI mobile client. The system was designed for a real volunteer association (*Verein*) where shift coordination and personnel planning were handled almost entirely with spreadsheets and manual communication.

After completing the thesis, I identified clear architectural weaknesses in v1: tight coupling, no clear domain boundary, and a tech stack that limited extensibility. PpmV2 is my complete rewrite in .NET 10 with Clean Architecture, treating it as a production-grade system from the start.

**The problem being solved:** Coordinators had no unified tool to plan shifts (*Einsätze*), assign roles to members, or track approval states. New members had to be manually verified and role-assigned. This project digitalizes and automates that entire workflow.

**Design philosophy:** The backend is intentionally frontend-agnostic. The API contract is stable enough to connect any client, whether a Next.js web app (current), the original MAUI mobile client, or a future MAUI Blazor Hybrid app targeting all platforms.

---

## What This Demonstrates

| Area | Detail |
|---|---|
| Architecture | Clean Architecture with strict dependency rule: Domain, Application, Infrastructure, Api |
| Domain modeling | Rich domain with entities (Shift, UserProfile, Location), role enums, shift status state machine |
| Auth | ASP.NET Core Identity with JWT Bearer, refresh token rotation (single-use, DB-persisted), policy-based authorization |
| Error handling | RFC 7807 ProblemDetails across all failure paths, ServiceResult for non-exceptional control flow |
| Data access | EF Core with CQRS query/command separation, AsNoTracking on all read paths, optimized multi-query joins |
| API design | RESTful controllers, CancellationToken propagation at every layer, consistent response shapes |
| Testing | 92 tests: 63 unit tests (xUnit + Moq) + 29 integration tests (WebApplicationFactory + real PostgreSQL) |
| CI/CD | GitHub Actions pipeline: build and integration tests on every push, PostgreSQL service container |
| Deployment | Docker multi-stage build, live on Render with Neon PostgreSQL, CORS config-driven per environment |

---

## Architecture

PpmV2 follows Clean Architecture with four projects and a strict inward dependency rule. No layer references anything above it, and the Domain has zero external NuGet dependencies.

```
Domain  <--  Application  <--  Infrastructure  <--  Api
```

- **Domain:** Entities and business rules. No EF Core, no HTTP, no external packages.
- **Application:** Use cases, DTOs, and service interfaces. Defines what needs to happen.
- **Infrastructure:** EF Core, Identity, JWT. Implements interfaces defined in Application.
- **Api:** Controllers, middleware, DI wiring. Thin HTTP boundary only.

Full layer diagrams, dependency graphs, and a request flow walkthrough are in [`src/docs/architecture.md`](src/docs/architecture.md).

---

## Features

| Area | Status | Description |
|---|---|---|
| Registration and Login | ✅ | JWT access token + refresh token issued on login, structured errors on failure |
| Refresh token rotation | ✅ | Single-use rotation: old token revoked on each refresh, logout revokes server-side |
| User approval workflow | ✅ | New users start as Pending; admin approves or rejects |
| Role management | ✅ | Admin assigns roles: Admin, Coordinator, Festmitarbeiter, Honorarkraft |
| Shift lifecycle | ✅ | Full state machine: Draft, PendingApproval, Planned, Active, Completed, Cancelled |
| Shift details | ✅ | Full shift view with participants, location, and readiness check |
| Participant response | ✅ | Festmitarbeiter accept or decline shift invitations |
| Location management | ✅ | Full CRUD: create, read, update, soft-deactivate, reactivate |
| Available staff query | ✅ | Query Festmitarbeiter available at a location on a given date |
| Validation errors | ✅ | Field-level 400 Bad Request responses via application/problem+json |
| Demo data seeding | ✅ | Admin, demo users across all roles, and locations auto-seeded on startup |
| Multi-database | ✅ | PostgreSQL (primary) and SQL Server supported, separate migration folders |
| Integration tests | ✅ | 29 integration tests via WebApplicationFactory against a real PostgreSQL instance |
| CI/CD | ✅ | GitHub Actions: build and integration tests on every push |

---

## Tech Stack

| Category | Technology |
|---|---|
| Runtime | .NET 10 / C# 14 |
| Web framework | ASP.NET Core Web API (MVC Controllers) |
| Authentication | ASP.NET Core Identity with JWT Bearer |
| ORM | Entity Framework Core 10 |
| Database | PostgreSQL (primary), SQL Server (supported) |
| Containerization | Docker + Docker Compose |
| Testing | xUnit v2 + Moq + WebApplicationFactory |
| Architecture | Clean Architecture |
| Deployment | Render (API), Neon (PostgreSQL), Vercel (frontend) |

---

## API Endpoints

### Auth

| Method | Route | Description | Auth |
|---|---|---|---|
| `POST` | `/api/auth/register` | Register a new user | Public |
| `POST` | `/api/auth/login` | Login and receive JWT access token + refresh token | Public |
| `POST` | `/api/auth/refresh` | Rotate refresh token and receive new token pair | Public |
| `POST` | `/api/auth/logout` | Revoke refresh token | Public |

### Users

| Method | Route | Description | Auth |
|---|---|---|---|
| `GET` | `/api/users/me` | Get own profile | Bearer |
| `GET` | `/api/users/me/locations` | Get locations assigned to the current Festmitarbeiter | Bearer |

### Locations

| Method | Route | Description | Auth |
|---|---|---|---|
| `GET` | `/api/locations` | List locations (`?includeInactive=true` for Coordinator/Admin) | Bearer |
| `GET` | `/api/locations/{id}` | Get location detail | Bearer |
| `POST` | `/api/locations` | Create a location | Coordinator / Admin |
| `PUT` | `/api/locations/{id}` | Update a location (set `isActive: false` to deactivate, `true` to reactivate) | Coordinator / Admin |
| `DELETE` | `/api/locations/{id}` | Soft-deactivate a location | Coordinator / Admin |
| `GET` | `/api/locations/{id}/available-staff` | List Festmitarbeiter available at a location on a given date (`?date=YYYY-MM-DD`) | Bearer |

### Shifts

| Method | Route | Description | Auth |
|---|---|---|---|
| `GET` | `/api/shifts` | List shifts (Festmitarbeiter see only their own, `?status=` filter supported) | Bearer |
| `GET` | `/api/shifts/{id}` | Get shift detail with participants and readiness check | Bearer |
| `POST` | `/api/shifts` | Create a shift in Draft status | Coordinator / Festmitarbeiter |
| `PUT` | `/api/shifts/{id}/propose` | Propose a team (Draft to PendingApproval) | Coordinator / Festmitarbeiter |
| `PUT` | `/api/shifts/{id}/approve` | Approve a shift (PendingApproval or Draft to Planned) | Coordinator / Admin |
| `PUT` | `/api/shifts/{id}/start` | Start a shift (Planned to Active) | Coordinator / Admin |
| `PUT` | `/api/shifts/{id}/complete` | Complete a shift (Active to Completed) | Coordinator / Admin |
| `PUT` | `/api/shifts/{id}/cancel` | Cancel a shift | Coordinator / Admin |
| `PUT` | `/api/shifts/{id}/respond` | Accept or decline a shift invitation | Bearer |

### Admin

| Method | Route | Description | Auth |
|---|---|---|---|
| `GET` | `/api/admin/users/pending` | List pending users | Admin |
| `GET` | `/api/admin/users/approved` | List approved users | Admin |
| `GET` | `/api/admin/users/rejected` | List rejected users | Admin |
| `PUT` | `/api/admin/users/approve/{id}` | Approve a user | Admin |
| `PUT` | `/api/admin/users/reject/{id}` | Reject a user | Admin |
| `PUT` | `/api/admin/users/{id}/role` | Assign a role | Admin |

Error responses follow RFC 7807 (`application/problem+json`):

```json
{
  "status": 400,
  "title": "VALIDATION_ERROR",
  "detail": "One or more fields are invalid.",
  "errors": {
    "email": ["Email is required."]
  }
}
```

---

## Quick Start

### Docker (API + PostgreSQL)

```bash
git clone https://github.com/djzh23/PpmV2.git
cd PpmV2
docker-compose up -d
# API: http://localhost:8080
```

### Local Development

```bash
docker-compose up -d postgres
dotnet ef database update --project src/PpmV2.Infrastructure --startup-project src/PpmV2.Api
dotnet run --project src/PpmV2.Api
# API: http://localhost:5105
```

### Tests

Unit tests (no database required):
```bash
dotnet test --filter "FullyQualifiedName~Unit"
```

Integration tests (requires docker-compose postgres running):
```bash
docker-compose up -d postgres
dotnet test --filter "FullyQualifiedName~Integration"
```

All tests:
```bash
dotnet test
```

Demo accounts (auto-seeded on startup, password: `Pass123$`):

| Email | Role |
|---|---|
| `admin@test.com` | Admin |
| `koord1@test.com` | Coordinator |
| `fest1@test.com` | Festmitarbeiter |
| `hon1@test.com` | Honorarkraft |

---

## Roadmap

### Completed

- [x] Clean Architecture, four-project layered structure
- [x] ASP.NET Core Identity with JWT Bearer authentication
- [x] Refresh token rotation: single-use, DB-persisted, revoked on logout
- [x] Role-based access control with authorization policies
- [x] User approval workflow (Pending to Approved or Rejected)
- [x] Full shift lifecycle state machine (Draft to Completed or Cancelled)
- [x] Location management with full CRUD and soft-delete
- [x] Participant response workflow (accept or decline)
- [x] RFC 7807 ProblemDetails error responses across all failure paths
- [x] ServiceResult pattern: no exceptions for expected business failures
- [x] Repository and query service separation (CQRS read/write split)
- [x] EF Core with multi-database support (PostgreSQL and SQL Server)
- [x] CancellationToken propagation at every layer
- [x] AsNoTracking on all read-only queries, optimized N+1-free join queries
- [x] Config-driven CORS (per-environment, suffix matching)
- [x] Docker Compose with database seeding
- [x] 63 unit tests + 29 integration tests (WebApplicationFactory + real PostgreSQL)
- [x] GitHub Actions CI/CD (build and integration tests on every push)
- [x] Deployed: Render, Neon PostgreSQL, Vercel

### Planned

- [ ] Pagination on list endpoints (shifts, users)
- [ ] Shift assignment notifications
- [ ] Calendar and schedule view endpoint
- [ ] PDF shift reports export
- [ ] Audit log (who approved whom and when)
- [ ] Location photo gallery (multi-image upload with cover selection)

---

## Future Vision

The API is intentionally frontend-agnostic. Any HTTP client can consume it.

The next planned client is a **MAUI Blazor Hybrid** app, a single codebase targeting Android, iOS, Windows, and macOS. This would replace the original MAUI/XAML client from the thesis and give all association members real cross-platform access.

This reflects a key architectural decision: by keeping the backend a stable, well-documented REST API, the frontend technology can evolve independently. From the original MAUI mobile client, to the current Next.js web app, to a future hybrid app.

---

## Related Projects

| Repository | Description |
|---|---|
| [ppmv2-next-frontend](https://github.com/djzh23/ppmv2-next-frontend) | Next.js 15 + shadcn/ui frontend for this API |
| [apiproject](https://github.com/djzh23/apiproject) | PPM v1, original Laravel backend from the Bachelor's thesis |
| [frontendproject](https://github.com/djzh23/frontendproject) | PPM v1, .NET MAUI mobile client from the Bachelor's thesis |
