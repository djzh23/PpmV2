# PpmV2 — Schichtverwaltungs-Backend

[![.NET](https://img.shields.io/badge/.NET_10-5C2D91?style=flat-square&logo=.net&logoColor=white)](https://dotnet.microsoft.com/)
[![ASP.NET Core](https://img.shields.io/badge/ASP.NET_Core-5C2D91?style=flat-square&logo=.net&logoColor=white)](https://docs.microsoft.com/aspnet/core/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-316192?style=flat-square&logo=postgresql&logoColor=white)](https://www.postgresql.org/)
[![Docker](https://img.shields.io/badge/Docker-0db7ed?style=flat-square&logo=docker&logoColor=white)](https://www.docker.com/)
[![xUnit](https://img.shields.io/badge/xUnit-5C2D91?style=flat-square&logo=.net&logoColor=white)](https://xunit.net/)

REST-API zur Verwaltung von Einsätzen, Standorten und rollenbasierter Benutzerverwaltung.
Entwickelt als vollständige Neuentwicklung des [Originals in Laravel](https://github.com/djzh23/apiproject) — migriert auf .NET 10 nach Clean-Architecture-Prinzipien.

> **Frontend:** [PpmV2-Next-Client](https://github.com/djzh23/PpmV2-Next-Client) — Next.js 16 + shadcn/ui

---

## Screenshots

**Authentifizierung — Login (POST /api/auth/login)**
![Login 200 OK](docs/screenshots/screenshot-auth-login.png)

**Admin — Genehmigte Benutzer abrufen (GET /api/admin/users/approved)**
![Admin genehmigte Benutzer](docs/screenshots/screenshot-admin-approved-users.png)

**Einsatz — Einzelner Einsatz per ID (GET /api/einsaetze/:id)**
![Einsatz nach ID](docs/screenshots/screenshot-einsatz-getbyid.png)

**Negative Tests — Validierungsfehler (400 Bad Request)**

| Fehlende E-Mail | Fehlendes Passwort |
|---|---|
| ![Fehlende E-Mail](docs/screenshots/screenshot-negative-missing-email.png) | ![Fehlendes Passwort](docs/screenshots/screenshot-negative-missing-password.png) |

---

## Architektur

```mermaid
graph TD
    API["PpmV2.Api\nREST-Endpunkte · Controller · DI-Root"]
    APP["PpmV2.Application\nUse Cases · DTOs · Schnittstellen"]
    INFRA["PpmV2.Infrastructure\nEF Core · ASP.NET Identity · DB-Kontext"]
    DOMAIN["PpmV2.Domain\nEntitäten · Geschäftsregeln · Keine ext. Abhängigkeiten"]
    TESTS["PpmV2.Tests\nxUnit · Moq"]

    API   -->|hängt ab von| APP
    API   -.->|verdrahtet via DI| INFRA
    APP   -->|hängt ab von| DOMAIN
    INFRA -->|implementiert Schnittstellen aus| APP
    INFRA -->|hängt ab von| DOMAIN
    TESTS -.->|testet| APP
    TESTS -.->|testet| DOMAIN

    style API    fill:#EEEDFE,stroke:#534AB7,color:#26215C
    style APP    fill:#E1F5EE,stroke:#0F6E56,color:#04342C
    style INFRA  fill:#E1F5EE,stroke:#0F6E56,color:#04342C
    style DOMAIN fill:#FAECE7,stroke:#993C1D,color:#4A1B0C
    style TESTS  fill:#EAF3DE,stroke:#3B6D11,color:#173404
```

```mermaid
sequenceDiagram
    participant C  as HTTP-Client
    participant A  as PpmV2.Api
    participant AP as PpmV2.Application
    participant D  as PpmV2.Domain
    participant I  as PpmV2.Infrastructure
    participant DB as PostgreSQL

    C  ->> A  : HTTP-Request
    A  ->> AP : Use-Case-Handler aufrufen
    AP ->> D  : Geschäftsregeln anwenden
    D  -->> AP: Entitäten zurückgeben
    AP ->> I  : IRepository-Aufruf (nur Schnittstelle)
    Note right of AP: Infrastructure wird zur Laufzeit<br/>via DI aufgelöst. Application hat<br/>keine direkte Abhängigkeit zu EF Core.
    I  ->> DB : SQL-Abfrage via EF Core
    DB -->> I  : Ergebnismenge
    I  -->> AP : gemappte Domänen-Entitäten
    AP -->> A  : DTO / Ergebnis zurückgeben
    A  -->> C  : HTTP-Response (200 / 4xx / 5xx)
```

> Vollständige Architekturdokumentation: [`docs/architecture.md`](docs/architecture.md)

---

## Funktionsumfang

| Bereich | Implementiert |
|---|---|
| Authentifizierung | Registrierung, Login, JWT-Token |
| Benutzergenehmigung | Admin genehmigt oder lehnt ausstehende Benutzer ab |
| Rollenverwaltung | Admin weist Rollen zu (Admin, Koordinator, Leader, Honorarkraft) |
| Einsätze | Erstellen, Veröffentlichen, Abrufen per ID — mit Standort- und Teilnehmerdaten |
| Standorte | CRUD für Einsatzstandorte |
| Validierung | Strukturierte `400 Bad Request`-Antworten mit feldbezogenen Fehlermeldungen |
| Negative Tests | Vollständige Negativtestsuite in Insomnia (fehlende Felder, falsches Passwort, nicht genehmigter Benutzer) |

---

## Projektstruktur

```
PpmV2/
├── src/
│   ├── PpmV2.Api/            # Controller, Middleware, Program.cs (DI-Root)
│   ├── PpmV2.Application/    # Use Cases, DTOs, IRepository-Schnittstellen
│   ├── PpmV2.Domain/         # Entitäten, Geschäftsregeln — keine ext. Abhängigkeiten
│   └── PpmV2.Infrastructure/ # EF Core, ASP.NET Identity, Repository-Implementierungen
├── PpmV2.Tests/              # xUnit + Moq Unit-Tests
├── docker-compose.yml        # App + PostgreSQL
└── docs/
    └── architecture.md
```

---

## Schnellstart

```bash
git clone https://github.com/djzh23/PpmV2.git
cd PpmV2
docker-compose up -d
# API:     http://localhost:5000
# Swagger: http://localhost:5000/swagger
```

**Lokale Entwicklung (ohne Docker für die App):**
```bash
# Nur die Datenbank als Container starten
docker-compose up -d db

# Migrationen anwenden
dotnet ef database update \
  --project src/PpmV2.Infrastructure \
  --startup-project src/PpmV2.Api

# API starten
dotnet run --project src/PpmV2.Api
```

**Tests ausführen:**
```bash
dotnet test
```

---

## API-Endpunkte

| Methode | Route | Beschreibung | Auth |
|---|---|---|---|
| `POST` | `/api/auth/register` | Neuen Benutzer registrieren | — |
| `POST` | `/api/auth/login` | Anmelden → JWT-Token | — |
| `GET` | `/api/admin/users/approved` | Genehmigte Benutzer auflisten | Admin |
| `GET` | `/api/admin/users/pending` | Ausstehende Benutzer auflisten | Admin |
| `PUT` | `/api/admin/users/:id/approve` | Benutzer genehmigen | Admin |
| `PUT` | `/api/admin/users/:id/role` | Rolle zuweisen | Admin |
| `PUT` | `/api/admin/users/:id/reject` | Benutzer ablehnen | Admin |
| `GET` | `/api/einsaetze/:id` | Einsatz per ID abrufen | Auth |
| `POST` | `/api/einsaetze` | Einsatz erstellen (Entwurf) | Koordinator |
| `POST` | `/api/einsaetze/:id/publish` | Einsatz veröffentlichen | Koordinator |
| `GET` | `/api/locations` | Standorte auflisten | Auth |

---

## Technologie-Stack

| Kategorie | Technologie |
|---|---|
| Laufzeit | .NET 10 |
| Framework | ASP.NET Core Web API |
| Authentifizierung | ASP.NET Core Identity + JWT |
| ORM | Entity Framework Core |
| Datenbank | PostgreSQL |
| Container | Docker + Docker Compose |
| Tests | xUnit + Moq |
| Architektur | Clean Architecture |

---

## Roadmap

- [x] Clean-Architecture-Schichtaufbau
- [x] ASP.NET Core Identity
- [x] Repository-Pattern
- [x] Docker Compose + PostgreSQL
- [x] xUnit + Moq Unit-Tests
- [x] Architekturdokumentation (Mermaid)
- [x] JWT-Authentifizierung
- [x] Rollenbasierte Zugriffskontrolle (RBAC)
- [x] Strukturierte Validierungsfehler
- [ ] Globaler Fehler-Handler (Problem Details RFC 7807)
- [ ] GitHub Actions CI/CD-Pipeline
- [ ] Integrationstests

---

## Verwandte Projekte

| Repository | Beschreibung |
|---|---|
| [PpmV2-Next-Client](https://github.com/djzh23/PpmV2-Next-Client) | Next.js-16-Frontend für diese API |
| [apiproject](https://github.com/djzh23/apiproject) | Laravel-v1-Backend (ursprüngliche Version) |
| [frontendproject](https://github.com/djzh23/frontendproject) | .NET-MAUI-Mobile-Client |

---

*Zouhair Ijaad · [LinkedIn](https://www.linkedin.com/in/zouhair-ijaad/)*