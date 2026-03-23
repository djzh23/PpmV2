# Architekturüberblick — PpmV2

PpmV2 ist ein REST-Backend nach **Clean Architecture**. Das bedeutet: Geschäftsregeln und Datenbankzugriffe sind strikt getrennt. Ein HTTP-Request läuft immer denselben Weg — von oben nach unten durch vier Schichten — und kehrt als Response zurück. Keine Schicht springt dabei eine andere über, und die innerste Schicht (Domain) kennt weder die Datenbank noch das Web-Framework.

---

## Feature-Übersicht und Datenfluss

Das folgende Diagramm zeigt, **welche Funktion in welcher Schicht liegt** und **wie ein Request von oben nach unten fließt**.

```mermaid
graph TD
    CLIENT["HTTP Client"]

    subgraph API ["PpmV2.Api — Controller-Schicht"]
      direction LR
      SHIFT_C["Shifts + Locations
      CRUD · publish · GET by ID"]
      AUTH_C["Auth
      POST /register · POST /login"]
      ADMIN_C["Admin
      GET/PUT users · approve · role · reject"]
    end

    subgraph APP ["PpmV2.Application — Use Cases"]
      direction LR
      SHIFT_U["Shifts Use Cases
      CreateShift · PublishShift · GetShiftDetails"]
      AUTH_U["Auth Use Cases
      RegisterCommand · LoginQuery · IAuthService"]
      ADMIN_U["Admin Use Cases
      ApproveUser · AssignRole · GetPending · GetApproved"]
    end

    subgraph DOMAIN ["PpmV2.Domain — Entitäten"]
      direction LR
      SHIFT_D["Shift Domain
      Shift · ShiftParticipant · ShiftRole · ShiftStatus"]
      LOC_D["Location Domain
      Location · name · district · address"]
      USER_D["User Domain
      UserProfile · UserRole · UserStatus: Pending / Active"]
    end

    subgraph INFRA ["PpmV2.Infrastructure"]
      direction LR
      PERSIST["Persistence
      AppDbContext · Repositories · EF Configurations · Queries"]
      AUTH_I["Auth + Identity
      JwtService · ASP.NET Identity · Migrations: Postgres + SqlServer"]
    end

    DB[("PostgreSQL (Docker)")]

    CLIENT -->|"HTTP Request"| AUTH_C
    CLIENT -->|"HTTP Request"| ADMIN_C
    CLIENT -->|"HTTP Request"| SHIFT_C

    SHIFT_C -->|"delegiert an Use Case"| SHIFT_U
    AUTH_C  -->|"delegiert an Use Case"| AUTH_U
    ADMIN_C -->|"delegiert an Use Case"| ADMIN_U

    SHIFT_U -->|"wendet Geschäftsregeln an"| SHIFT_D
    SHIFT_U -->|"wendet Geschäftsregeln an"| LOC_D
    AUTH_U  -->|"wendet Geschäftsregeln an"| USER_D
    ADMIN_U -->|"wendet Geschäftsregeln an"| USER_D

    SHIFT_D -->|"IRepository call (interface only)"| PERSIST
    LOC_D   -->|"IRepository call (interface only)"| PERSIST
    USER_D  -->|"IRepository call (interface only)"| PERSIST
    USER_D  -->|"Identity / JWT"| AUTH_I

    PERSIST -->|"SQL-Abfrage via EF Core"| DB
    AUTH_I  -->|"SQL-Abfrage via EF Core"| DB

    style CLIENT fill:#E6F1FB,stroke:#185FA5,color:#042C53
    style DB     fill:#E6F1FB,stroke:#185FA5,color:#042C53
```

**Leseanleitung:**
- **Controller-Schicht (Api):** Nimmt HTTP-Requests entgegen und leitet sie weiter — enthält keine Geschäftslogik.
- **Use Cases (Application):** Orchestrieren den Ablauf. Kennen die Domain, aber nicht die Datenbank. Greifen nur über Interfaces auf Persistenz zu.
- **Entitäten (Domain):** Reine Fachlogik — `Shift`, `UserProfile`, `Location`. Kein EF Core, kein HTTP, keine externen Pakete.
- **Infrastructure:** Implementiert die Interfaces aus Application. Hier liegt EF Core, Identity und JWT — alles was mit externen Systemen spricht.
- **Gestrichelte Pfeile (`IRepository call`):** Der Use Case ruft nur das Interface auf. Welche konkrete Klasse dahintersteckt, entscheidet der DI-Container in `Program.cs` zur Laufzeit.

---

## Projektabhängigkeiten

Dieses Diagramm zeigt die **compile-time Abhängigkeiten** zwischen den .NET-Projekten — also welches Projekt welches andere referenziert.

```mermaid
graph TD
    API["PpmV2.Api
    AuthController · AdminUsersController
    ShiftsController · LocationsController · Middleware"]

    APP["PpmV2.Application
    Auth · Admin · Shifts · Locations
    Commands · Queries · Handlers · DTOs
    IShiftRepository · IAuthService"]

    INFRA["PpmV2.Infrastructure
    Auth · Identity · Admin · Persistence
    AppDbContext · Repositories · Queries
    Migrations: Postgres + SqlServer"]

    DOMAIN["PpmV2.Domain
    Shifts: Shift · ShiftParticipant · ShiftRole · ShiftStatus
    Users: UserProfile · UserRole · UserStatus
    Locations: Location — keine externen Abhängigkeiten"]

    TESTS["PpmV2.Tests
    Admin · Auth · Shifts
    xUnit · Moq · Mvc.Testing"]

    API   -->|"ProjectReference"| APP
    API   -->|"ProjectReference (DI-Root)"| INFRA
    APP   -->|"ProjectReference"| DOMAIN
    INFRA -->|"implementiert Interfaces aus"| APP
    INFRA -->|"ProjectReference"| DOMAIN
    TESTS -->|"ProjectReference"| API
    TESTS -->|"ProjectReference"| INFRA

    style API    fill:#EEEDFE,stroke:#534AB7,color:#26215C
    style APP    fill:#E1F5EE,stroke:#0F6E56,color:#04342C
    style INFRA  fill:#E1F5EE,stroke:#0F6E56,color:#04342C
    style DOMAIN fill:#FAECE7,stroke:#993C1D,color:#4A1B0C
    style TESTS  fill:#EAF3DE,stroke:#3B6D11,color:#173404
```

**Wichtige Regel:** Abhängigkeiten zeigen immer **nach innen** — `Api → Application → Domain`. Die Pfeilrichtung zeigt an, wer wen kennt. `Domain` kennt niemanden — deshalb hat es keine ausgehenden Pfeile.

`PpmV2.Infrastructure` zeigt einen Pfeil zu `PpmV2.Application` (`implementiert Interfaces aus`), weil Infrastructure die dort definierten Repository-Interfaces konkret umsetzt — aber Application weiß davon nichts.

---

## Request Flow — Schritt für Schritt

```mermaid
sequenceDiagram
    participant C  as HTTP Client
    participant A  as PpmV2.Api
    participant AP as PpmV2.Application
    participant D  as PpmV2.Domain
    participant I  as PpmV2.Infrastructure
    participant DB as PostgreSQL

    C  ->> A  : GET /api/einsaetze/:id
    A  ->> AP : invoke GetShiftDetails Handler
    AP ->> D  : validate / apply business rules
    D  -->> AP: Shift entity returned
    AP ->> I  : IShiftRepository.GetByIdAsync()
    Note right of AP: Infrastructure resolved at runtime via DI.<br/>Application has no direct reference to EF Core.
    I  ->> DB : SELECT via EF Core
    DB -->> I  : result set
    I  -->> AP : mapped Shift entity
    AP -->> A  : ShiftDetailsDto
    A  -->> C  : HTTP 200 OK
```

---

## Zusammenfassung

| Schicht | Verantwortung | Externe Pakete |
|---|---|---|
| `PpmV2.Domain` | Entitäten und Geschäftsregeln | keine |
| `PpmV2.Application` | Use Cases, Interfaces, DTOs | keine |
| `PpmV2.Infrastructure` | EF Core, Identity, JWT, Migrations | EF Core · Npgsql · Identity |
| `PpmV2.Api` | Controller, Middleware, DI-Root | JwtBearer · OpenApi |
| `PpmV2.Tests` | Unit- und Integrationstests | xUnit · Moq · Mvc.Testing |