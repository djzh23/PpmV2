# PpmV2 - Shift & Personnel Management API

[![.NET](https://img.shields.io/badge/.NET_10-5C2D91?style=flat-square&logo=.net&logoColor=white)](https://dotnet.microsoft.com/)
[![ASP.NET Core](https://img.shields.io/badge/ASP.NET_Core-5C2D91?style=flat-square&logo=.net&logoColor=white)](https://docs.microsoft.com/aspnet/core/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-316192?style=flat-square&logo=postgresql&logoColor=white)](https://www.postgresql.org/)
[![Docker](https://img.shields.io/badge/Docker-0db7ed?style=flat-square&logo=docker&logoColor=white)](https://www.docker.com/)
[![xUnit](https://img.shields.io/badge/xUnit-5C2D91?style=flat-square&logo=.net&logoColor=white)](https://xunit.net/)
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
| Domain modeling | Rich domain with entities (Shift, UserProfile, Location), role enums, status state machines |
| Auth | ASP.NET Core Identity with JWT Bearer and role-based authorization policies |
| Error handling | RFC 7807 ProblemDetails across all failure paths, structured validation errors, global middleware |
| Data access | EF Core with repository and query separation, AsNoTracking on all read paths, multi-DB support |
| API design | RESTful controllers, CancellationToken propagation at every layer, consistent response shapes |
| Testing | 57 unit tests (xUnit + Moq), repository tests with EF InMemory, middleware tests |
| Deployment | Docker multi-stage build, live on Render with Neon PostgreSQL, CORS config-driven per environment |
| Observability | Structured logging, environment-specific configuration, DB seeding on startup |

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
| Registration and Login | ✅ | JWT token issued on login, structured errors on failure |
| User approval workflow | ✅ | New users start as Pending; admin approves or rejects |
| Role management | ✅ | Admin assigns roles: Admin, Coordinator, Festmitarbeiter, Honorarkraft |
| Shift creation | ✅ | Coordinators create shifts with location, time window, and participant list |
| Shift details | ✅ | Full shift view with readiness check and missing requirements |
| Location management | ✅ | Active locations seeded and queryable |
| Validation errors | ✅ | Field-level 400 Bad Request responses via application/problem+json |
| Demo data seeding | ✅ | Admin, demo users across all roles, and locations auto-seeded on startup |
| Multi-database | ✅ | PostgreSQL (primary) and SQL Server supported, separate migration folders |

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
| Testing | xUnit v2 + Moq + coverlet |
| Architecture | Clean Architecture |
| Deployment | Render (API), Neon (PostgreSQL), Vercel (frontend) |

---

## API Endpoints

| Method | Route | Description | Auth |
|---|---|---|---|
| `POST` | `/api/auth/register` | Register a new user | Public |
| `POST` | `/api/auth/login` | Login and receive JWT token | Public |
| `GET` | `/api/locations` | List active locations | Bearer |
| `GET` | `/api/einsaetze/{id}` | Get shift by ID | Bearer |
| `POST` | `/api/einsaetze` | Create a shift (Draft) | Coordinator / Festmitarbeiter |
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
    "email": ["Email is required."],
    "password": ["Password must be at least 6 characters."]
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
docker-compose up -d db
dotnet ef database update --project src/PpmV2.Infrastructure --startup-project src/PpmV2.Api
dotnet run --project src/PpmV2.Api
# API: http://localhost:5105
```

### Tests

```bash
dotnet test
```

Test users (auto-seeded, password: `Pass123$`):

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
- [x] Role-based access control with authorization policies
- [x] User approval workflow (Pending to Approved or Rejected)
- [x] Shift creation with participant and location management
- [x] RFC 7807 ProblemDetails error responses across all failure paths
- [x] Repository pattern with interface segregation (Application / Infrastructure)
- [x] EF Core with multi-database support (PostgreSQL and SQL Server)
- [x] CancellationToken propagation at every layer
- [x] AsNoTracking on all read-only queries
- [x] Config-driven CORS (per-environment, suffix matching)
- [x] Docker Compose with database seeding
- [x] 57 unit tests (xUnit + Moq)
- [x] Deployed: Render, Neon PostgreSQL, Vercel

### Planned

- [ ] GitHub Actions CI/CD pipeline (build and test on PR)
- [ ] Integration tests with WebApplicationFactory and Testcontainers
- [ ] Shift publication workflow (Draft to Published)
- [ ] Shift assignment notifications
- [ ] Calendar and schedule view endpoint
- [ ] PDF shift reports export
- [ ] Audit log (who approved whom and when)

---

## Future Vision

The API is intentionally frontend-agnostic. Any HTTP client can consume it.

The next planned client is a **MAUI Blazor Hybrid** app, a single codebase targeting Android, iOS, Windows, and macOS. This would replace the original MAUI/XAML client from the thesis and give all association members real cross-platform access.

This also reflects a key architectural decision: by keeping the backend a stable, well-documented REST API, the frontend technology can evolve independently. From the original MAUI mobile client, to the current Next.js web app, to a future hybrid app.

---

## Related Projects

| Repository | Description |
|---|---|
| [ppmv2-next-frontend](https://github.com/djzh23/ppmv2-next-frontend) | Next.js 15 + shadcn/ui frontend for this API |
| [apiproject](https://github.com/djzh23/apiproject) | PPM v1, original Laravel backend from the Bachelor's thesis |
| [frontendproject](https://github.com/djzh23/frontendproject) | PPM v1, .NET MAUI mobile client from the Bachelor's thesis |

