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
![Login 200 OK](src/docs/screenshots/auth-login-200.png)

**Admin — Genehmigte Benutzer abrufen (GET /api/admin/users/approved)**
![Admin genehmigte Benutzer](src/docs/screenshots/admin-users-approved-200.png)

**Einsatz — Einzelner Einsatz per ID (GET /api/einsaetze/:id)**
![Einsatz nach ID](src/docs/screenshots/einsatz-get-by-id-200.png)

**Negative Tests — Validierungsfehler (400 Bad Request)**

| Fehlende E-Mail | Fehlendes Passwort |
|---|---|
| ![Fehlende E-Mail](src/docs/screenshots/auth-login-400-missing-email.png) | ![Fehlendes Passwort](src/docs/screenshots/auth-login-400-missing-password.png) |

---

## Architektur

```mermaid
graph TD
    API["PpmV2.Api<br/>REST endpoints · Controllers · DI root"]
    APP["PpmV2.Application<br/>Use Cases · DTOs · Interfaces"]
    INFRA["PpmV2.Infrastructure<br/>EF Core · ASP.NET Identity · DB context"]
    DOMAIN["PpmV2.Domain<br/>Entities · Business rules · No external deps"]
    TESTS["PpmV2.Tests<br/>xUnit · Moq"]

    API   -->|depends on| APP
    API   -.->|wires up via DI| INFRA
    APP   -->|depends on| DOMAIN
    INFRA -->|implements interfaces from| APP
    INFRA -->|depends on| DOMAIN
    TESTS -.->|tests| APP
    TESTS -.->|tests| DOMAIN

    style API    fill:#EEEDFE,stroke:#534AB7,color:#26215C
    style APP    fill:#E1F5EE,stroke:#0F6E56,color:#04342C
    style INFRA  fill:#E1F5EE,stroke:#0F6E56,color:#04342C
    style DOMAIN fill:#FAECE7,stroke:#993C1D,color:#4A1B0C
    style TESTS  fill:#EAF3DE,stroke:#3B6D11,color:#173404
```

```mermaid
sequenceDiagram
    participant C  as HTTP Client
    participant A  as PpmV2.Api
    participant AP as PpmV2.Application
    participant D  as PpmV2.Domain
    participant I  as PpmV2.Infrastructure
    participant DB as PostgreSQL

    C  ->> A  : HTTP Request
    A  ->> AP : invoke Use Case Handler
    AP ->> D  : validate / apply business rules
    D  -->> AP: entities returned
    AP ->> I  : IRepository call (interface only)
    Note right of AP: Infrastructure resolved at runtime via DI.<br/>Application has no direct reference to EF Core.
    I  ->> DB : SQL query via EF Core
    DB -->> I  : result set
    I  -->> AP : mapped domain entities
    AP -->> A  : return DTO / result
    A  -->> C  : HTTP Response (200 / 4xx / 5xx)
```

> Vollständige Architekturdokumentation: [`src/docs/architecture.md`](src/docs/architecture.md)

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
│   ├── PpmV2.Infrastructure/ # EF Core, ASP.NET Identity, Repository-Implementierungen
│   └── docs/
│       ├── architecture.md
│       ├── screenshots/      # Insomnia-API-Screenshots
│       └── diagrams/         # Architekturdiagramme
├── PpmV2.Tests/              # xUnit + Moq Unit-Tests
└── docker-compose.yml        # App + PostgreSQL
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
- [x] Architekturdokumentation
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