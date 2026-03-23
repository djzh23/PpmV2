# PpmV2 – Test-Einstellungen

## API URLs

| Modus     | HTTPS                      | HTTP                      |
|-----------|----------------------------|---------------------------|
| Localhost | https://localhost:7129     | http://localhost:5105     |

OpenAPI Spec (raw JSON):
  https://localhost:7129/openapi/v1.json
  → Zum Testen im Browser: in https://editor.swagger.io einfügen


---

## Datenbank (Docker)

```
docker-compose up -d
```

| Einstellung | Wert            |
|-------------|-----------------|
| Host        | localhost       |
| Port        | 5433            |
| Datenbank   | ppmv2           |
| User        | ppmv2           |
| Passwort    | ppmv2_password  |

Connection String:
  Host=localhost;Port=5433;Database=ppmv2;Username=ppmv2;Password=ppmv2_password


---

## Test-Benutzer (werden beim Start automatisch angelegt)

Passwort für alle: `Pass123$`

### Admin
| Email           | Rolle |
|-----------------|-------|
| admin@test.com  | Admin |

### Koordinatoren  (dürfen Schichten erstellen)
| Email           |
|-----------------|
| koord1@test.com |
| koord2@test.com |
| koord3@test.com |

### Festmitarbeiter  (dürfen Schichten erstellen)
| Email          |
|----------------|
| fest1@test.com |
| fest2@test.com |
| fest3@test.com |

### Honorarkraft
| Email         |
|---------------|
| hon1@test.com |
| hon2@test.com |
| hon3@test.com |


---

## Endpoints

### Öffentlich (kein Token nötig)

| Methode | URL                   | Body (JSON)                                      |
|---------|-----------------------|--------------------------------------------------|
| POST    | /api/auth/register    | `{ "email": "", "password": "", "firstname": "", "lastname": "" }` |
| POST    | /api/auth/login       | `{ "email": "", "password": "" }`                |

Login-Antwort enthält: `{ "token": "...", "userId": "...", "email": "..." }`

### Authentifiziert (Bearer Token im Header: `Authorization: Bearer <token>`)

| Methode | URL                            | Berechtigung              |
|---------|--------------------------------|---------------------------|
| GET     | /api/locations                 | Alle eingeloggten User    |
| GET     | /api/einsaetze/{id}            | Alle eingeloggten User    |
| POST    | /api/einsaetze                 | Coordinator, Festmitarbeiter |
| GET     | /api/admin/users/pending       | Admin only                |
| GET     | /api/admin/users/approved      | Admin only                |
| GET     | /api/admin/users/rejected      | Admin only                |
| PUT     | /api/admin/users/approve/{id}  | Admin only                |
| PUT     | /api/admin/users/reject/{id}   | Admin only                |
| PUT     | /api/admin/users/{id}/role     | Admin only                |

### POST /api/einsaetze – Beispiel Body

```json
{
  "title": "Nachtschicht Elbpark",
  "description": "Reguläre Nachtschicht",
  "startAtUtc": "2026-04-01T20:00:00Z",
  "endAtUtc": "2026-04-02T06:00:00Z",
  "locationId": "<guid>",
  "participants": [
    { "userId": "<guid>", "role": "Leader" }
  ]
}
```

### PUT /api/admin/users/{id}/role – Beispiel Body

```json
{ "role": "Coordinator" }
```

Verfügbare Rollen: `Admin`, `Coordinator`, `Festmitarbeiter`, `Honorarkraft`


---

## Typischer Test-Ablauf

1. Docker starten:          `docker-compose up -d`
2. API starten:             `dotnet run --project src/PpmV2.Api`
3. Login:                   POST /api/auth/login  →  Token kopieren
4. Locations abrufen:       GET /api/locations  (Token im Header)
5. Schicht erstellen:       POST /api/einsaetze  (als koord1@test.com)
6. Schicht abrufen:         GET /api/einsaetze/{id}
7. Admin-Bereich testen:    Login als admin@test.com  →  GET /api/admin/users/pending
