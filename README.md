# PpmV2 – Modernes .NET Backend mit Clean Architecture

[![.NET](https://img.shields.io/badge/.NET_10-5C2D91?style=for-the-badge&logo=.net&logoColor=white)](https://dotnet.microsoft.com/)
[![ASP.NET Core](https://img.shields.io/badge/ASP.NET_Core-5C2D91?style=for-the-badge&logo=.net&logoColor=white)](https://docs.microsoft.com/en-us/aspnet/core/)
[![Entity Framework Core](https://img.shields.io/badge/Entity_Framework_Core-512BD4?style=for-the-badge&logo=entityframework&logoColor=white)](https://docs.microsoft.com/en-us/ef/core/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-316192?style=for-the-badge&logo=postgresql&logoColor=white)](https://www.postgresql.org/)
[![Docker](https://img.shields.io/badge/Docker-0db7ed?style=for-the-badge&logo=docker&logoColor=white)](https://www.docker.com/)
[![xUnit](https://img.shields.io/badge/xUnit-5C2D91?style=for-the-badge&logo=.net&logoColor=white)](https://xunit.net/)

Dieses Projekt ist die Neuentwicklung eines Backends, das ursprünglich mit Laravel entwickelt war.  
Der Wechsel von Laravel zu **.NET 10** dient dem Ziel, ein produktionsreifes Backend nach  
Best Practices zu erstellen. Der Fokus liegt auf einer robusten Architektur, die Testbarkeit  
und zukünftige Erweiterbarkeit maximiert.

---

## 🏗️ Architektur

Das Projekt folgt konsequent den Prinzipien der **Clean Architecture**, um eine klare  
Trennung der Verantwortlichkeiten sicherzustellen:

- **Api :** Präsentationsschicht; definiert REST-Endpunkte  
- **Application :** Anwendungslogik, Use Cases, DTOs  
- **Domain :** Geschäftslogik, Entitäten, Repository-Abstraktionen  
- **Infrastructure :** EF Core, ASP.NET Core Identity, technische Implementierungen  

### Dokumentation

- Architektur: [docs/architecture.md](src/docs/architecture.md)

---

## ✨ Kernfunktionen
 
### Authentifizierung & Identität
- **ASP.NET Core Identity :**  sichere Benutzerregistrierung und Login
- **Sauberes Domänenmodell :**  `UserProfile`-Entität entkoppelt fachliche Logik vom Identity-System
- **JWT :** zustandslose API-Authentifizierung *(in Entwicklung)*
 
### Architektur-Features
- **Repository Pattern :** EF Core von der Geschäftslogik entkoppelt, bessere Testbarkeit
- **Dependency Injection :** durchgehend in allen Schichten
- **Unit Tests :** xUnit + Moq für Kernlogik
 
### Infrastruktur
- **PostgreSQL** als primäre Datenbank
- **Docker Compose :** vollständig containerisiert, ein Befehl zum Starten
- **EF Core Migrations :** versionierte Datenbankschema-Verwaltung
- **Identity Seeding :** automatische Initialdaten beim ersten Start

---

## 🎯 Nächste Schritte:

- Implementierung von **JWT** für zustandslose Authentifizierung  
- Ausbau von **rollenbasierten Zugriffskontrollen (RBAC)**  
- Einführung eines **globalen Error-Handling-Mechanismus**  
- Aufbau einer **CI/CD-Pipeline** für automatisierte Builds und Tests  

---

Aktiv in Entwicklung – Feedback, Hinweise oder Beiträge sind jederzeit willkommen.
