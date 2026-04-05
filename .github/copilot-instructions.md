# GitHub Copilot Instructions – contentapi

## Projektkontext

`contentapi` ist eine Azure Functions API für **Eagles Jungscharen**.
Sie liest SharePoint-Listen über Microsoft Graph aus und gibt die Daten als strukturiertes JSON zurück.
Externe Websites (z.B. Wix) können so News-Feeds, Agenda-Feeds und andere Listen einbinden – ohne direkten SharePoint-Zugriff.

## Sprachkonvention

| Bereich | Sprache |
|---|---|
| Code, Variablen, Bezeichner, Namespaces, Klassen, Methoden | **Englisch** |
| Kommentare, XML-Dokumentation | **Deutsch** |
| Fehlermeldungen (Exception-Texte, Log-Messages) | **Deutsch** |
| README, Dokumentation, Commit-Messages | **Deutsch** |

## Technischer Stack

- **.NET 10** – Azure Functions v4, Isolated Worker Model (`OutputType=Exe`)
- **Azure Functions** – HTTP-Trigger, anonyme Authentifizierung, Route `api/content/{short}`
- **Microsoft Graph SDK** (`Microsoft.Graph` v5) – Zugriff auf SharePoint-Listen via Graph API
- **Azure Table Storage** – Konfigurationsspeicher für Content-Typ-Zuordnungen (`ContentConfig`-Tabelle)
- **`GuedesPlace.AzureTools`** – `ExtendedAzureTableClientService` und `TypedAzureTableClient<T>` für typisierte Table Storage-Zugriffe
- **`DefaultAzureCredential`** (`Azure.Identity`) – Authentifizierung gegen Graph (lokal: `az login`, in Azure: Managed Identity)
- **Application Insights** – Telemetrie (Requests werden immer geloggt, Sampling nur auf anderen Typen)

## Projektstruktur

```
contentapi/
├── Program.cs                        # Host-Setup, DI-Registrierung
├── host.json                         # Functions-Host-Konfiguration
├── local.settings.json               # Lokale Einstellungen (nicht committen!)
├── Functions/
│   ├── Content.cs                    # Azure Function (HTTP Trigger, anonym)
│   └── RegisterConfiguration.cs     # Azure Function (HTTP Trigger, Admin)
├── Models/
│   └── ContentTypeConfig.cs          # Table Storage Entity – Zuordnung short → SiteId + ListId
└── Services/
    └── SharepointListService.cs      # Graph-Zugriff auf SharePoint-Listen
```

## Namespace-Konvention

```
EaglesJungscharen.Azure.ContentApi
EaglesJungscharen.Azure.ContentApi.Models
EaglesJungscharen.Azure.ContentApi.Services
```

## Architekturmuster

### HTTP-Trigger (Function)

- Autorisierung: `Anonymous`
- HTTP-Methode: `GET`
- Route-Muster: `api/content/{short}` – `short` ist der Lookup-Schlüssel
- Primary Constructor Injection für Abhängigkeiten
- Lookup in Table Storage: PartitionKey = `"config"`, RowKey = `short`
- Rückgabe `404 NotFound` wenn kein Config-Eintrag gefunden
- Rückgabe `200 OK` mit `List<Dictionary<string, object?>>` bei Erfolg

**Vorlage für neue Functions:**
```csharp
public class MyFunction(
    ILogger<MyFunction> logger,
    ExtendedAzureTableClientService tableClientService,
    SharepointListService sharepointListService)
{
    [Function("MyFunction")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "api/my/{short}")] HttpRequest req,
        string @short)
    {
        // Implementierung
    }
}
```

### Table Storage Entity (Model)

- Erbt implizit die Table Storage Entity-Konventionen von `GuedesPlace.AzureTools`
- Pflichtfeld `Key` (string, required) – entspricht dem RowKey
- Registrierung in `Program.cs` via `tableClientService.CreateAndRegisterTableClient<T>("TabellenName")`

**Vorlage für neue Models:**
```csharp
namespace EaglesJungscharen.Azure.ContentApi.Models;

public class MyConfig
{
    public required string Key { get; set; }
    // weitere Felder
}
```

### Services

- Registrierung als Singleton in `Program.cs`
- Graph-Zugriff ausschliesslich über `GraphServiceClient` (injiziert)
- Paginierung immer über `PageIterator<TItem, TCollection>.CreatePageIterator(...)` – nie manuell

**Vorlage für neue Services:**
```csharp
public class MyService(GraphServiceClient graphClient)
{
    private readonly GraphServiceClient _graphClient = graphClient;
    // Implementierung
}
```

### DI-Registrierung (Program.cs)

Neue Services werden am Ende des bestehenden `builder.Services`-Blocks ergänzt:
```csharp
builder.Services.AddSingleton<MyService>();
```

Neue Table Storage Clients werden vor `builder.Build()` registriert:
```csharp
tableClientService.CreateAndRegisterTableClient<MyConfig>("MeineTabellenName");
```

## Konfiguration

### Pflicht-Umgebungsvariablen

| Variable | Zweck |
|---|---|
| `AzureWebJobsStorage` | Azure Storage Connection String – wird beim Start validiert, fehlt er → `InvalidOperationException` |
| `FUNCTIONS_WORKER_RUNTIME` | Muss `dotnet-isolated` sein |

### ContentConfig Table Storage

| Feld | Wert |
|---|---|
| PartitionKey | `"config"` (immer fest) |
| RowKey | `short` (der URL-Parameter) |
| `SiteId` | SharePoint Site-ID |
| `ListId` | SharePoint Listen-ID |

## Was Copilot vermeiden soll

- Keine `HttpClient`-basierten Graph-Aufrufe – immer den `GraphServiceClient` verwenden
- Kein manuelles Paging – immer `PageIterator` nutzen
- Keine hardcodierten Credentials – immer `DefaultAzureCredential`
- Keine Felder aus SharePoint-Antworten filtern oder umbenennen – Rückgabe immer als rohes `Dictionary<string, object?>`
- Keine `FunctionAuthorizationLevel.Function` oder höher – alle Endpunkte sind `Anonymous`
- `local.settings.json` nicht in Versionsverwaltung aufnehmen (enthält Connection Strings)
