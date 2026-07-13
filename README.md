# OpenSlalom.Data

Diese Library bildet die vorhandene MariaDB/MySQL-Datenbankstruktur mit Entity Framework Core ab.
Die initiale Struktur ist als Migration enthalten, damit die Datenbank bei einer neuen Installation automatisch erzeugt werden kann.

## Setup in einer Anwendung

1. Projekt referenzieren.
2. Connection Strings fuer MySQL (remote) und SQLite (lokal) bereitstellen.
3. Beide Datenbanken registrieren.
4. Beim Start beide Verbindungen initialisieren.

```csharp
using OpenSlalom.Data;

var builder = WebApplication.CreateBuilder(args);

var remote = builder.Configuration.GetConnectionString("OpenSlalomRemote")
    ?? throw new InvalidOperationException("Connection string 'OpenSlalomRemote' fehlt.");

var local = builder.Configuration.GetConnectionString("OpenSlalomLocal")
    ?? "Data Source=open_slalom_local.db";

builder.Services.AddOpenSlalomDualData(local, remote);

var app = builder.Build();

await app.InitializeOpenSlalomDualDatabasesAsync();

app.Run();
```

Die UI schreibt zuerst immer in SQLite. Ueber `DataSyncService.SyncLocalToRemoteAsync()` werden lokale Daten spaeter nach MySQL synchronisiert.

## Neue Datenbankaenderungen

Alle zukuenftigen Schema-Aenderungen sollten ueber EF-Migrationen in diesem Projekt gepflegt werden.

Beispiel fuer eine neue Migration:

```powershell
dotnet dotnet-ef migrations add AddNeueSpalte --project .\OpenSlalom.Data\OpenSlalom.Data.csproj --context OpenSlalomDbContext
```

Beispiel fuer eine neue SQLite-Migration:

```powershell
dotnet dotnet-ef migrations add AddNeueSpalteSqlite --project .\OpenSlalom.Data\OpenSlalom.Data.csproj --context LocalOpenSlalomDbContext --output-dir Migrations\Sqlite
```

Migrationen zur Laufzeit werden ueber `InitializeOpenSlalomDualDatabasesAsync()` automatisch angewendet.

## Design-Time Connection String

Fuer EF-CLI-Befehle kann optional die Umgebungsvariable `OPENSLALOM_CONNECTION_STRING` gesetzt werden.
Wenn sie nicht gesetzt ist, wird ein lokaler Standard verwendet:

`Server=127.0.0.1;Port=3306;Database=open_slalom;User=root;Password=root;`

Fuer SQLite-CLI-Befehle kann optional `OPENSLALOM_SQLITE_CONNECTION_STRING` verwendet werden.
