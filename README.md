# contentapi

Eine Azure Functions API für **Eagles Jungscharen**, die SharePoint-Listen als strukturierte JSON-Feeds bereitstellt.
Externe Websites (z.B. Wix) können so News-Feeds, Agenda-Feeds und andere Listen einbinden – ohne direkten SharePoint-Zugriff.

---

## Benutzersicht – API-Konsument

### Was die API macht

Die API stellt SharePoint-Listeninhalte über einen einzigen HTTP-Endpunkt bereit.
Ein konfigurierter Kurzname (`short`) bestimmt, welche SharePoint-Liste abgefragt wird.
Die Antwort enthält immer alle Felder der Liste als JSON-Array – ohne Filterung oder Umbenennung.

### Endpunkt

```
GET https://<function-app>.azurewebsites.net/api/content/{short}
```

| Parameter | Beschreibung |
|---|---|
| `short` | Kurzname des Content-Typs (z.B. `news`, `agenda`) |

**Beispiel-Aufruf:**
```
GET /api/content/news
```

**Erfolgreiche Antwort (HTTP 200):**
```json
[
  {
    "Title": "Sommerlager 2026",
    "Datum": "2026-07-10T00:00:00Z",
    "Beschreibung": "Unser jährliches Sommerlager findet statt..."
  },
  {
    "Title": "Elternabend",
    "Datum": "2026-05-03T00:00:00Z",
    "Beschreibung": "Informationsabend für alle Eltern..."
  }
]
```

Die Feldnamen entsprechen den SharePoint-Spaltennamen der jeweiligen Liste.

**Fehlerfall (HTTP 404):**

Wenn der angegebene `short`-Wert nicht in der Konfiguration hinterlegt ist, antwortet die API mit `404 Not Found`.

### Neuen Feed-Typ einrichten

Für jeden Feed-Typ muss ein Eintrag in der Azure Table Storage-Tabelle `ContentConfig` angelegt werden:

| Feld | Wert |
|---|---|
| PartitionKey | `config` |
| RowKey | Kurzname (z.B. `news`) |
| `SiteId` | SharePoint Site-ID |
| `ListId` | SharePoint Listen-ID |

Sobald der Eintrag gespeichert ist, ist der Endpunkt `/api/content/news` sofort verfügbar – ohne Code-Änderung oder Neustart.

### Konfiguration registrieren (Admin)

Über den Admin-Endpunkt kann eine neue Konfiguration programmatisch angelegt oder aktualisiert werden.
Für den Aufruf wird der **Master Key** der Function App benötigt.

```
POST https://<function-app>.azurewebsites.net/api/admin/registerConfiguration
```

**Authentifizierung:** Master Key als HTTP-Header oder Query-Parameter:
- Header: `x-functions-key: <master-key>`
- Query: `?code=<master-key>`

**Request-Body (JSON):**
```json
{
  "Key": "news",
  "SiteId": "<SharePoint Site-ID>",
  "ListId": "<SharePoint Listen-ID>"
}
```

| Feld | Beschreibung |
|---|---|
| `Key` | Kurzname (RowKey), z.B. `news` oder `agenda` |
| `SiteId` | SharePoint Site-ID |
| `ListId` | SharePoint Listen-ID |

**Query-Parameter:**

| Parameter | Beschreibung |
|---|---|
| `update` | Muss auf `true` gesetzt sein, um eine bestehende Konfiguration zu überschreiben |

**Antwortcodes:**

| Code | Bedeutung |
|---|---|
| `201 Created` | Konfiguration erfolgreich gespeichert |
| `400 Bad Request` | Pflichtfelder fehlen oder JSON ist ungültig |
| `409 Conflict` | Konfiguration mit diesem `Key` existiert bereits – `?update=true` setzen |

### Konfiguration löschen (Admin)

Über denselben Admin-Endpunkt kann eine Konfiguration gelöscht werden.
Auch hier wird der **Master Key** benötigt.

```
DELETE https://<function-app>.azurewebsites.net/api/admin/registerConfiguration?short=<kurzname>
```

**Query-Parameter:**

| Parameter | Beschreibung |
|---|---|
| `short` | Kurzname der zu löschenden Konfiguration (Pflicht) |

**Antwortcodes:**

| Code | Bedeutung |
|---|---|
| `204 No Content` | Konfiguration erfolgreich gelöscht |
| `400 Bad Request` | Query-Parameter `short` fehlt |
| `404 Not Found` | Keine Konfiguration mit diesem Kurznamen vorhanden |

### Erforderliche SharePoint-Berechtigungen der Function-Identität

Die Function App liest SharePoint-Listen über die **Microsoft Graph API**. Dafür benötigt die Identität der Function App folgende Konfiguration:

**1. Managed Identity aktivieren**

Die Function App muss eine **System-assigned Managed Identity** besitzen. Diese wird im Azure-Portal unter *Function App → Identität → Vom System zugewiesen* aktiviert.

**2. Microsoft Graph-Berechtigung erteilen**

Die Managed Identity benötigt die folgende **Application Permission** (nicht Delegated) auf Microsoft Graph:

| Berechtigung | Beschreibung |
|---|---|
| `Sites.Read.All` | Lesezugriff auf alle SharePoint-Sites und deren Listen |

Alternativ kann `Sites.Selected` verwendet werden, um den Zugriff auf einzelne, explizit freigegebene Sites zu beschränken (geringstes Privileg).

**3. Admin Consent erteilen**

Application Permissions müssen durch einen **Entra ID Administrator** bestätigt werden:
- Im Azure-Portal unter *Entra ID → App-Registrierungen → API-Berechtigungen → Administratorzustimmung erteilen*
- Oder via Azure CLI: `az ad app permission admin-consent --id <client-id>`

---

## Entwicklersicht

### Voraussetzungen

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Azure Functions Core Tools v4](https://learn.microsoft.com/azure/azure-functions/functions-run-local)
- Azure-Konto mit Zugriff auf das Storage-Konto und die SharePoint-Sites
- Azure CLI (`az login`) für die lokale Graph-Authentifizierung

### Lokales Setup

1. `local.settings.json` befüllen (nicht in die Versionsverwaltung aufnehmen):

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "<Azure Storage Connection String>",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated"
  }
}
```

2. Mit Azure anmelden (wird von `DefaultAzureCredential` für den Graph-Zugriff genutzt):

```bash
az login
```

3. Die Function App starten:

```bash
dotnet build
func start --dotnet-isolated
```

Die API ist anschliessend unter `http://localhost:7071/api/content/{short}` erreichbar.

### Projektstruktur

```
contentapi/
├── Content.cs                   # Azure Function – HTTP Trigger (GET /api/content/{short})
├── Program.cs                   # Host-Setup, DI-Registrierung aller Services
├── host.json                    # Functions-Host-Konfiguration (Logging, App Insights)
├── local.settings.json          # Lokale Einstellungen (nicht committen!)
├── Models/
│   └── ContentTypeConfig.cs     # Table Storage Entity – Zuordnung short → SiteId + ListId
└── Services/
    └── SharepointListService.cs # Microsoft Graph-Zugriff auf SharePoint-Listen
```

### Abhängigkeiten

| Paket | Zweck |
|---|---|
| `Microsoft.Graph` v5 | Zugriff auf SharePoint-Listen via Microsoft Graph API |
| `Azure.Identity` | `DefaultAzureCredential` – lokal `az login`, in Azure Managed Identity |
| `GuedesPlace.AzureTools` | Typisierter Azure Table Storage-Zugriff (`ExtendedAzureTableClientService`) |
| `Microsoft.ApplicationInsights.WorkerService` | Application Insights Telemetrie |
| `Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore` | ASP.NET Core HTTP-Integration für den Isolated Worker |

### Ablauf einer Anfrage

```
HTTP GET /api/content/{short}
    │
    ▼
Content.cs (Azure Function)
    │  Lookup PartitionKey="config", RowKey=short
    ▼
Azure Table Storage (ContentConfig)
    │  → SiteId, ListId
    ▼
SharepointListService.GetListItemsAsync(siteId, listId)
    │  GET /sites/{siteId}/lists/{listId}/items?$expand=fields
    │  Paginierung via PageIterator (alle Seiten)
    ▼
List<Dictionary<string, object?>>
    │
    ▼
HTTP 200 JSON
```

### Neuen Service hinzufügen

1. Klasse in `Services/` anlegen (Primary Constructor mit `GraphServiceClient`):

```csharp
public class MyService(GraphServiceClient graphClient)
{
    private readonly GraphServiceClient _graphClient = graphClient;
}
```

2. In `Program.cs` als Singleton registrieren:

```csharp
builder.Services.AddSingleton<MyService>();
```

### Neue Table Storage Entity hinzufügen

1. Klasse in `Models/` anlegen:

```csharp
namespace EaglesJungscharen.Azure.ContentApi.Models;

public class MyConfig
{
    public required string Key { get; set; }
    // weitere Felder
}
```

2. In `Program.cs` vor `builder.Build()` registrieren:

```csharp
tableClientService.CreateAndRegisterTableClient<MyConfig>("MeineTabellenName");
```

### Deployment (Azure)

- Die Function App benötigt eine **System-assigned Managed Identity**.
- Der Managed Identity muss Lesezugriff auf die betreffenden SharePoint-Sites über die **Microsoft Graph API** gewährt werden (`Sites.Read.All` oder enger gefasst).
- `AzureWebJobsStorage` muss als App Setting im Azure Portal hinterlegt sein.
- Application Insights: Requests werden immer vollständig geloggt (kein Sampling auf Request-Typ).

### Sprachkonvention

| Bereich | Sprache |
|---|---|
| Code, Variablen, Bezeichner, Namespaces | Englisch |
| Kommentare, XML-Dokumentation | Deutsch |
| Fehlermeldungen, Log-Messages | Deutsch |
| README, Commit-Messages | Deutsch |

