# Architecture — PpmV2

PpmV2 follows Clean Architecture across four projects. Business rules and data access are strictly separated. Every HTTP request flows through the same four layers top to bottom and returns as a response. No layer skips another, and the innermost layer (Domain) has zero knowledge of the database or web framework.

---

## Layer Overview and Request Flow

This diagram shows which responsibility belongs to which layer and how a request flows from top to bottom.

```mermaid
graph TD
    CLIENT["HTTP Client"]

    subgraph API ["PpmV2.Api — Controller Layer"]
      direction LR
      SHIFT_C["Shifts + Locations
      Create · GET list · GET by ID · workflow"]
      AUTH_C["Auth
      /register · /login · /refresh · /logout"]
      ADMIN_C["Admin
      GET/PUT users · approve · role · reject"]
    end

    subgraph APP ["PpmV2.Application — Use Cases"]
      direction LR
      SHIFT_U["Shifts Use Cases
      CreateShift · GetShiftDetails · GetShiftList"]
      AUTH_U["Auth Use Cases
      RegisterCommand · LoginQuery · IAuthService
      IRefreshTokenRepository"]
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
      UserProfile · UserRole · UserStatus: Pending / Approved / Rejected
      Auth: RefreshToken"]
    end

    subgraph INFRA ["PpmV2.Infrastructure"]
      direction LR
      PERSIST["Persistence
      AppDbContext · Repositories · EF Configurations · Queries"]
      AUTH_I["Auth + Identity
      JwtService · ASP.NET Identity · Migrations: Postgres + SqlServer"]
    end

    DB[("PostgreSQL")]

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

    SHIFT_U -->|"IRepository call (interface only)"| PERSIST
    AUTH_U  -->|"IRepository call (interface only)"| PERSIST
    AUTH_U  -->|"Identity / JWT"| AUTH_I
    ADMIN_U -->|"IRepository call (interface only)"| PERSIST

    PERSIST -->|"SQL query via EF Core"| DB
    AUTH_I  -->|"SQL query via EF Core"| DB

    style CLIENT fill:#E6F1FB,stroke:#185FA5,color:#042C53
    style DB     fill:#E6F1FB,stroke:#185FA5,color:#042C53
```

- **Controller Layer (Api):** Receives HTTP requests and delegates to handlers. No business logic here.
- **Use Cases (Application):** Orchestrate the flow. They know the Domain but not the database. Persistence is called through interfaces only.
- **Entities (Domain):** Pure business logic — `Shift`, `UserProfile`, `Location`, `RefreshToken`. No EF Core, no HTTP, no external packages.
- **Infrastructure:** Implements the interfaces from Application. EF Core, Identity, and JWT live here — everything that talks to external systems.
- **IRepository call (interface only):** The use case only knows the interface. Which concrete class is behind it is decided by the DI container in `Program.cs` at runtime — Application knows nothing about it.

---

## Project Dependencies

This diagram shows the compile-time dependencies between .NET projects — which project references which. Arrows always point inward: who knows whom.

```mermaid
graph TD
    API["PpmV2.Api
    AuthController · AdminUsersController
    ShiftsController · LocationsController · Middleware"]

    APP["PpmV2.Application
    Auth · Admin · Shifts · Locations
    Commands · Queries · Handlers · DTOs
    IShiftRepository · IAuthService · IRefreshTokenRepository"]

    INFRA["PpmV2.Infrastructure
    Auth · Identity · Admin · Persistence
    AppDbContext · Repositories · Queries
    Migrations: Postgres + SqlServer"]

    DOMAIN["PpmV2.Domain
    Shifts: Shift · ShiftParticipant · ShiftRole · ShiftStatus
    Users: UserProfile · UserRole · UserStatus
    Auth: RefreshToken
    Locations: Location — no external dependencies"]

    TESTS["PpmV2.Tests
    Admin · Auth · Shifts · Infrastructure · Integration
    xUnit · Moq · WebApplicationFactory"]

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

- **`ProjectReference`:** Hard compile-time dependency. The project knows the other directly.
- **`ProjectReference (DI root only)`:** `Api` references `Infrastructure` exclusively to bind interfaces to concrete implementations in `Program.cs`. Infrastructure is never used directly for business logic.
- **`implements interfaces from`:** `Infrastructure` knows the interfaces from `Application` (e.g. `IShiftRepository`) and provides the concrete EF Core implementation. `Application` itself knows nothing about it.
- **`PpmV2.Tests` → `Api` + `Infrastructure`:** Tests reference both to reach all layers through the DI container in the test context.

`PpmV2.Domain` has no outgoing arrows — it knows no other layer and has no external NuGet packages.

---

## Request Flow — Step by Step

Example: a user fetches a shift by ID.

```mermaid
sequenceDiagram
    participant C  as HTTP Client
    participant A  as PpmV2.Api
    participant AP as PpmV2.Application
    participant D  as PpmV2.Domain
    participant I  as PpmV2.Infrastructure
    participant DB as PostgreSQL

    C  ->> A  : GET /api/shifts/:id
    A  ->> AP : invoke GetShiftDetailsHandler
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

1. The **Controller** receives the request and calls the handler. No logic in the controller itself.
2. The **Handler** (Application layer) coordinates the flow: first domain validation, then data access.
3. The **Domain** checks business rules and returns entities.
4. The handler calls `IShiftRepository.GetByIdAsync()` — the interface only, not EF Core directly.
5. The **DI container** bound `IShiftRepository` to `ShiftRepository` (EF Core) on startup in `Program.cs`.
6. **Infrastructure** runs the SQL query and returns mapped entities.
7. The handler builds a `ShiftDetailsDto` and returns it to the controller.
8. The **Controller** sends the HTTP response.

---

## Layer Summary

| Layer | Responsibility | External NuGet packages |
|---|---|---|
| `PpmV2.Domain` | Entities and business rules | None |
| `PpmV2.Application` | Use cases, interfaces, DTOs | None |
| `PpmV2.Infrastructure` | EF Core, Identity, JWT, Migrations | EF Core, Npgsql, Identity |
| `PpmV2.Api` | Controllers, middleware, DI root | JwtBearer, OpenApi |
| `PpmV2.Tests` | Unit and integration tests | xUnit, Moq, WebApplicationFactory, real PostgreSQL |
