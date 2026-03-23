# Architekturüberblick — PpmV2

PpmV2 ist ein REST-Backend nach **Clean Architecture**. Geschäftsregeln und Datenbankzugriffe sind strikt getrennt. Ein HTTP-Request durchläuft immer dieselben vier Schichten von oben nach unten — und kehrt als Response zurück. Keine Schicht überspringt eine andere, und die innerste Schicht (Domain) kennt weder die Datenbank noch das Web-Framework.

---

## Feature-Übersicht und Datenfluss

Das folgende Diagramm zeigt, **welche Funktion in welcher Schicht liegt** und **wie ein Request von oben nach unten fließt**.

```mermaid
graph TD
    CLIENT["HTTP Client"]

    subgraph API ["PpmV2.Api — Controller Layer"]
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

    subgraph DOMAIN ["PpmV2.Domain — Entities"]
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

    SHIFT_C -->|"delegates to Use Case"| SHIFT_U
    AUTH_C  -->|"delegates to Use Case"| AUTH_U
    ADMIN_C -->|"delegates to Use Case"| ADMIN_U

    SHIFT_U -->|"applies business rules"| SHIFT_D
    SHIFT_U -->|"applies business rules"| LOC_D
    AUTH_U  -->|"applies business rules"| USER_D
    ADMIN_U -->|"applies business rules"| USER_D

    SHIFT_D -->|"IRepository call (interface only)"| PERSIST
    LOC_D   -->|"IRepository call (interface only)"| PERSIST
    USER_D  -->|"IRepository call (interface only)"| PERSIST
    USER_D  -->|"Identity / JWT"| AUTH_I

    PERSIST -->|"SQL query via EF Core"| DB
    AUTH_I  -->|"SQL query via EF Core"| DB

    style CLIENT fill:#E6F1FB,stroke:#185FA5,color:#042C53
    style DB     fill:#E6F1FB,stroke:#185FA5,color:#042C53
```

**Leseanleitung:**
- **Controller Layer (Api):** Nimmt HTTP-Requests entgegen und leitet sie weiter — enthält keine Geschäftslogik.
- **Use Cases (Application):** Orchestrieren den Ablauf. Sie kennen die Domain, aber nicht die Datenbank. Persistenz wird nur über Interfaces aufgerufen.
- **Entities (Domain):** Reine Fachlogik — `Shift`, `UserProfile`, `Location`. Kein EF Core, kein HTTP, keine externen Pakete.
- **Infrastructure:** Implementiert die Interfaces aus Application. Hier liegt EF Core, Identity und JWT — alles was mit externen Systemen kommuniziert.
- **`IRepository call (interface only)`:** Der Use Case kennt nur das Interface. Welche konkrete Klasse dahintersteckt, entscheidet der DI-Container in `Program.cs` zur Laufzeit — Application weiß davon nichts.

---

## Projektabhängigkeiten

Dieses Diagramm zeigt die **compile-time Abhängigkeiten** zwischen den .NET-Projekten — also welches Projekt welches andere referenziert. Die Pfeile zeigen immer **nach innen**: wer wen kennt.

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
    Locations: Location — no external dependencies"]

    TESTS["PpmV2.Tests
    Admin · Auth · Shifts
    xUnit · Moq · Mvc.Testing"]

    API   -->|"ProjectReference"| APP
    API   -->|"ProjectReference (DI root only)"| INFRA
    APP   -->|"ProjectReference"| DOMAIN
    INFRA -->|"implements interfaces from"| APP
    INFRA -->|"ProjectReference"| DOMAIN
    TESTS -->|"ProjectReference"| API
    TESTS -->|"ProjectReference"| INFRA

    style API    fill:#EEEDFE,stroke:#534AB7,color:#26215C
    style APP    fill:#E1F5EE,stroke:#0F6E56,color:#04342C
    style INFRA  fill:#E1F5EE,stroke:#0F6E56,color:#04342C
    style DOMAIN fill:#FAECE7,stroke:#993C1D,color:#4A1B0C
    style TESTS  fill:#EAF3DE,stroke:#3B6D11,color:#173404
```

**Erklärung der Pfeile:**
- **`ProjectReference`** — harte compile-time Abhängigkeit. Das Projekt kennt das andere direkt.
- **`ProjectReference (DI root only)`** — `Api` referenziert `Infrastructure` ausschließlich um dort die Interfaces an konkrete Implementierungen zu binden (`Program.cs`). Für Geschäftslogik wird Infrastructure nie direkt genutzt.
- **`implements interfaces from`** — `Infrastructure` kennt die Interfaces aus `Application` (z. B. `IShiftRepository`) und liefert die konkrete EF-Core-Implementierung. `Application` selbst weiß davon nichts.
- **`PpmV2.Tests` → `Api` + `Infrastructure`** — Tests referenzieren beide, um über den DI-Container alle Schichten im Testkontext erreichbar zu machen.

**Wichtige Regel:** `PpmV2.Domain` hat keine ausgehenden Pfeile — es kennt keine andere Schicht und hat keine externen NuGet-Pakete.

---

## Request Flow — Schritt für Schritt

Beispiel: Ein Benutzer ruft einen Einsatz per ID ab.

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
    Note right of AP: Infrastructure is resolved at runtime via DI.<br/>Application has no direct reference to EF Core.
    I  ->> DB : SELECT via EF Core
    DB -->> I  : result set
    I  -->> AP : mapped Shift entity
    AP -->> A  : ShiftDetailsDto
    A  -->> C  : HTTP 200 OK
```

**Erklärung Schritt für Schritt:**
1. Der **Controller** empfängt den Request und ruft den zuständigen Handler auf — keine Logik im Controller selbst.
2. Der **Handler** (Application-Schicht) koordiniert den Ablauf: zuerst Domain-Validierung, dann Datenzugriff.
3. Die **Domain** prüft Geschäftsregeln und gibt Entitäten zurück.
4. Der Handler ruft `IShiftRepository.GetByIdAsync()` — nur das Interface, nicht EF Core direkt.
5. Der **DI-Container** hat beim Start `IShiftRepository` an `ShiftRepository` (EF Core) gebunden — das passiert in `Program.cs`.
6. **Infrastructure** führt die SQL-Abfrage aus und gibt gemappte Entitäten zurück.
7. Der Handler baut ein `ShiftDetailsDto` und gibt es an den Controller zurück.
8. Der **Controller** sendet die HTTP-Response.

---

## Zusammenfassung

| Schicht | Verantwortung | Externe NuGet-Pakete |
|---|---|---|
| `PpmV2.Domain` | Entitäten und Geschäftsregeln | keine |
| `PpmV2.Application` | Use Cases, Interfaces, DTOs | keine |
| `PpmV2.Infrastructure` | EF Core, Identity, JWT, Migrations | EF Core · Npgsql · Identity |
| `PpmV2.Api` | Controller, Middleware, DI-Root | JwtBearer · OpenApi |
| `PpmV2.Tests` | Unit- und Integrationstests | xUnit · Moq · Mvc.Testing |