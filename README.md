# PpmV2 – Modernes .NET Backend mit Clean Architecture

![.NET](https://img.shields.io/badge/.NET-10-blueviolet)
![Architecture](https://img.shields.io/badge/Clean%20Architecture-On-5B2E90)
![Tests](https://img.shields.io/badge/Unit%20Tests-xUnit%20%2B%20Moq-brightgreen)
![License](https://img.shields.io/badge/License-MIT-green)
![Status](https://img.shields.io/badge/Status-Active%20Development-yellow)

Dieses Projekt ist die Neuentwicklung eines Backends, das ursprünglich mit Laravel entwickelt war.  
Der Wechsel von Laravel zu **.NET 10** dient dem Ziel, ein produktionsreifes Backend nach  
Best Practices zu erstellen. Der Fokus liegt auf einer robusten Architektur, die Testbarkeit  
und zukünftige Erweiterbarkeit maximiert.

---

## 🏗️ Architektur

Das Projekt folgt konsequent den Prinzipien der **Clean Architecture**, um eine klare  
Trennung der Verantwortlichkeiten sicherzustellen:

- **Api** – Präsentationsschicht; definiert REST-Endpunkte  
- **Application** – Anwendungslogik, Use Cases, DTOs  
- **Domain** – Geschäftslogik, Entitäten, Repository-Abstraktionen  
- **Infrastructure** – EF Core, ASP.NET Core Identity, technische Implementierungen  

---

## ✨ Kernfunktionen

- **Sichere Authentifizierung**  
  Benutzerregistrierung und Login via ASP.NET Core Identity

- **Saubere Domänenstruktur**  
  Separates `UserProfile`-Modell entkoppelt fachliche Logik vom Identity-System

- **Flexible Datenhaltung**  
  Repository-Pattern zur Entkopplung von EF Core und zur Verbesserung der Testbarkeit

- **Qualitätssicherung**  
  Unit Tests mit **xUnit** und **Moq**

---

** 🎯 Nächste Schritte:**

- Implementierung von **JWT** für zustandslose Authentifizierung  
- Ausbau von **rollenbasierten Zugriffskontrollen (RBAC)**  
- Einführung eines **globalen Error-Handling-Mechanismus**  
- Aufbau einer **CI/CD-Pipeline** für automatisierte Builds und Tests  

---

Aktiv in Entwicklung – Feedback, Hinweise oder Beiträge sind jederzeit willkommen.
