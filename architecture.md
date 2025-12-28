## Architekturüberblick

Das Backend von **PpmV2 (v2)** folgt einer **Clean-Architecture-nahen, schichtenbasierten Struktur**.  
Ziel ist eine klare Trennung von Verantwortlichkeiten, hohe Testbarkeit sowie eine langfristig wartbare und erweiterbare Codebasis.

Die Architektur unterscheidet strikt zwischen **fachlicher Logik**, **Anwendungslogik**, **technischer Implementierung** und **Hosting/Presentation**.

---

## Schichten und Abhängigkeiten

Die Anwendung besteht aus folgenden logischen Schichten:

- **Domain (`PpmV2.Domain`)**  
  Enthält die fachlichen Kernmodelle (Entities, Enums) und Domänenregeln.  
  Diese Schicht ist vollständig unabhängig und hat **keine Abhängigkeiten** zu anderen Schichten.

- **Application (`PpmV2.Application`)**  
  Enthält die Use Cases der Anwendung (Commands, Queries, Handler), DTOs sowie Schnittstellen (Ports).  
  Die Application-Schicht kennt die Domain, aber **keine Infrastrukturdetails**.

- **Infrastructure (`PpmV2.Infrastructure`)**  
  Enthält technische Implementierungen wie EF Core, Repositories, Query-Services, Auth/JWT, Identity und Seeding.  
  Diese Schicht implementiert die in der Application definierten Interfaces und kennt sowohl **Application als auch Domain**.

- **API (`PpmV2.Api`)**  
  Stellt die HTTP-Schnittstelle bereit (Controller, Middleware, Konfiguration).  
  Die API enthält **keine Business-Logik** und fungiert als **Composition Root** für Dependency Injection.

- **Tests (`PpmV2.Tests`)**  
  Enthält Unit Tests für Application-Use-Cases und Domain-Logik, unabhängig von Infrastruktur und Web.

### Abhängigkeitsregeln

- Domain → keine Abhängigkeiten  
- Application → Domain  
- Infrastructure → Application + Domain  
- API → Application (fachlich)  
- API → Infrastructure **nur für DI/Wiring (Composition Root)**  
- Tests → Application + Domain  

Die folgende Grafik zeigt diese Abhängigkeitsbeziehungen auf Projektebene:

*(Diagramm: Layered Architecture / Abhängigkeitsdiagramm)*

---

## Composition Root (API)

Die API-Schicht ist bewusst der einzige Ort, an dem **konkrete Implementierungen an Interfaces gebunden werden**:

- Repositories
- Query-Services
- Auth-Services
- DbContext
- Security (JWT, Identity)

Dadurch bleibt die Application-Schicht unabhängig und testbar.  
Die API „nutzt“ Infrastructure nicht fachlich, sondern **verdrahtet sie lediglich**.

---

## Request Flow – Beispiel: Shift-Erstellung (Command)

Der folgende Ablauf zeigt einen typischen **Write-Use-Case** (Command) anhand der Erstellung eines Shifts:

*(Diagramm: Sequence Diagram – Controller → Handler → Repository → Datenbank)*

### Ablaufbeschreibung

1. Ein **Controller** empfängt einen HTTP-Request und delegiert an einen Use Case.
2. Der **Handler** in der Application-Schicht verarbeitet den Command.
3. Der Handler greift ausschließlich über ein **Interface (`IShiftRepository`)** auf Persistenzfunktionen zu.
4. Die konkrete Implementierung (`ShiftRepository`) wird zur Laufzeit per **Dependency Injection** bereitgestellt.
5. Die **Infrastructure-Schicht** führt Datenbankzugriffe über EF Core aus.
6. Der Handler steuert Validierungen, Erzeugung der Domain-Entitäten und den Transaktionsabschluss.
7. Das Ergebnis (z. B. `ShiftId`) wird an den Controller zurückgegeben.

Wichtig:  
Der Handler kennt **nur das Interface**, nicht die konkrete Implementierung oder die Datenbank.

---

## Zusammenfassung

Diese Architektur stellt sicher, dass:

- fachliche Logik von technischen Details entkoppelt ist
- Änderungen an Datenbank, Auth oder Infrastruktur keine Auswirkungen auf Use Cases haben
- Unit Tests ohne Webserver oder Datenbank möglich sind
- das System schrittweise erweitert (z. B. weitere Query-Modelle, andere Persistenz) werden kann

Die gewählte Struktur ist bewusst **pragmatisch**, orientiert sich aber klar an den Prinzipien der Clean Architecture und an bewährten .NET-Backend-Patterns.
