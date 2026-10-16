# PPM Backend

[![CI](https://github.com/djzh23/ppm-backend/actions/workflows/ci.yml/badge.svg?branch=main)](https://github.com/djzh23/ppm-backend/actions/workflows/ci.yml)

Play Pal Manager (PPM): Einsatzplanung als REST-API für Benutzer, Standorte und Einsätze.

**Status:** Live · in Entwicklung
**Demo:** [ppmv2.vercel.app](https://ppmv2.vercel.app) · **Frontend:** [ppm-frontend](https://github.com/djzh23/ppm-frontend)
**Betrieb:** Render (API), Neon (PostgreSQL), Vercel (Frontend). Demo-System mit Beispieldaten, keine echten Personen.

Nachfolger meiner Abschlussarbeit (Laravel, .NET MAUI), komplett neu in .NET 10 gebaut.

## Was es kann
- Registrierung mit Admin-Freigabe, Rollen (Admin, Koordinator, Festmitarbeiter, Honorarkraft), JWT mit einmaliger Refresh-Token-Rotation
- Einsatz-Workflow als Zustandsautomat: Entwurf → Freigabe → Geplant → Aktiv → Abgeschlossen; Start und Abschluss nur durch den Leiter des Einsatzes
- Standorte, Teilnehmer und Einladungen mit Zu- und Absage
- Startschutz: Production startet nicht mit dem Entwicklungs-Schlüssel

## Stack
.NET 10 · ASP.NET Core · EF Core 10 · PostgreSQL · JWT · xUnit, Moq · Docker Compose · GitHub Actions

## Starten
```bash
docker-compose up -d postgres
dotnet run --project src/PpmV2.Api   # http://localhost:5105
dotnet test
```
Konfiguration: siehe `.env.example`.

## Tests und CI
116 Tests (67 Unit, 49 Integration gegen PostgreSQL), bei jedem Push und Pull Request auf `main` in GitHub Actions.

## Architektur
Clean Architecture (Domain, Application, Infrastructure, Api): [src/docs/architecture.md](src/docs/architecture.md)

## Roadmap
- Automatischer Deploy nach grünen Tests
- Health-Endpunkt
