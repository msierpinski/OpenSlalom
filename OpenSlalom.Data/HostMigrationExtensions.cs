using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NLog;
using System.Reflection;

namespace OpenSlalom.Data;

public static class HostMigrationExtensions
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private const string SyncDefaultTimestamp = "'1970-01-01 00:00:00'";
    private const string SqliteSyncMetadataMigrationId = "20260713103700_AddBidirectionalSyncMetadataSqlite";
    private const string MySqlSyncMetadataMigrationId = "20260713103701_AddBidirectionalSyncMetadata";
    private static readonly string[] SyncTables =
    [
        "disziplin",
        "disziplin_altersklassen",
        "vereine",
        "wetter",
        "fahrer",
        "training",
        "meisterschaften",
        "karts",
        "tstints",
        "mstints",
        "fahrer_im_training",
        "fahrer_inder_meisterschaft",
        "trunden",
        "mrunden"
    ];

    public static async Task ApplyOpenSlalomMigrationsAsync(this IHost host, CancellationToken cancellationToken = default)
    {
        using var scope = host.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OpenSlalomDbContext>();

        if (dbContext.Database.IsSqlite())
        {
            await dbContext.Database.EnsureCreatedAsync(cancellationToken);
            return;
        }

        await dbContext.Database.MigrateAsync(cancellationToken);
    }

    public static async Task<DatabaseInitializationResult> InitializeOpenSlalomDualDatabasesAsync(
        this IHost host,
        CancellationToken cancellationToken = default)
    {
        using var scope = host.Services.CreateScope();

        var localFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<LocalOpenSlalomDbContext>>();
        var remoteMigrationFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<OpenSlalomDbContext>>();
        var remoteFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<RemoteOpenSlalomDbContext>>();

        var localTask = InitializeLocalAsync(localFactory, cancellationToken);
        var remoteTask = InitializeRemoteAsync(remoteMigrationFactory, remoteFactory, cancellationToken);

        await Task.WhenAll(localTask, remoteTask);

        return new DatabaseInitializationResult(
            localTask.Result.Success,
            remoteTask.Result.Success,
            localTask.Result.Error,
            remoteTask.Result.Error);
    }

    private static async Task<InitializationState> InitializeLocalAsync(
        IDbContextFactory<LocalOpenSlalomDbContext> localFactory,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var localDbContext = await localFactory.CreateDbContextAsync(cancellationToken);
            await EnsureLocalDatabaseMigratedAsync(localDbContext, cancellationToken);
            await EnsureCurrentLocalSchemaAsync(localDbContext, cancellationToken);
            var connected = await localDbContext.Database.CanConnectAsync(cancellationToken);
            return connected
                ? InitializationState.Connected()
                : InitializationState.Failed("SQLite konnte nach der Initialisierung nicht verbunden werden.");
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "SQLite-Initialisierung fehlgeschlagen.");
            return InitializationState.Failed(ex.Message);
        }
    }

    private static async Task<InitializationState> InitializeRemoteAsync(
        IDbContextFactory<OpenSlalomDbContext> remoteMigrationFactory,
        IDbContextFactory<RemoteOpenSlalomDbContext> remoteFactory,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var migrationDbContext = await remoteMigrationFactory.CreateDbContextAsync(cancellationToken);
            var connectionString = migrationDbContext.Database.GetConnectionString();
            if (!string.IsNullOrWhiteSpace(connectionString) && !MySqlEndpointProbe.CanReach(connectionString))
            {
                return InitializationState.Failed("Remote-MySQL Host ist nicht erreichbar.");
            }

            if (!await migrationDbContext.Database.CanConnectAsync(cancellationToken))
            {
                return InitializationState.Failed("Remote-MySQL ist nicht erreichbar.");
            }

            await RepairPartiallyAppliedRemoteSyncMetadataMigrationAsync(migrationDbContext, cancellationToken);
            await migrationDbContext.Database.MigrateAsync(cancellationToken);
            await EnsureCurrentRemoteSchemaAsync(migrationDbContext, cancellationToken);

            await using var remoteDbContext = await remoteFactory.CreateDbContextAsync(cancellationToken);
            if (!await remoteDbContext.Database.CanConnectAsync(cancellationToken))
            {
                return InitializationState.Failed("Remote-MySQL ist nach Migration nicht erreichbar.");
            }

            return InitializationState.Connected();
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Remote-MySQL-Initialisierung fehlgeschlagen.");
            return InitializationState.Failed(ex.Message);
        }
    }

    private static async Task EnsureLocalDatabaseMigratedAsync(
        LocalOpenSlalomDbContext localDbContext,
        CancellationToken cancellationToken)
    {
        var historyExists = await localDbContext.Database.SqlQueryRaw<int>(
                "SELECT COUNT(1) AS Value FROM sqlite_master WHERE type='table' AND name='__EFMigrationsHistory'")
            .SingleAsync(cancellationToken) > 0;

        if (!historyExists)
        {
            var hasAppTables = await localDbContext.Database.SqlQueryRaw<int>(
                    "SELECT COUNT(1) AS Value FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%' AND name <> '__EFMigrationsHistory'")
                .SingleAsync(cancellationToken) > 0;

            if (hasAppTables)
            {
                await ApplyLegacyLocalSchemaPatchesAsync(localDbContext, cancellationToken);
                await BootstrapLegacySqliteMigrationHistoryAsync(localDbContext, cancellationToken);
                return;
            }
        }

        await RepairPartiallyAppliedSyncMetadataMigrationAsync(localDbContext, cancellationToken);

        await localDbContext.Database.MigrateAsync(cancellationToken);
    }

    private static async Task EnsureCurrentLocalSchemaAsync(
        LocalOpenSlalomDbContext localDbContext,
        CancellationToken cancellationToken)
    {
        if (await TableExistsAsync(localDbContext, "training", cancellationToken))
        {
            await EnsureColumnAsync(localDbContext, "training", "uuid", "ALTER TABLE training ADD COLUMN uuid TEXT NULL;", cancellationToken);
            await EnsureColumnAsync(localDbContext, "training", "ist_veroeffentlicht", "ALTER TABLE training ADD COLUMN ist_veroeffentlicht INTEGER NOT NULL DEFAULT 0;", cancellationToken);
            await EnsureColumnAsync(localDbContext, "training", "aktiver_fahrer_zeitnahme_1_id", "ALTER TABLE training ADD COLUMN aktiver_fahrer_zeitnahme_1_id INTEGER NULL;", cancellationToken);
            await EnsureColumnAsync(localDbContext, "training", "aktiver_fahrer_zeitnahme_2_id", "ALTER TABLE training ADD COLUMN aktiver_fahrer_zeitnahme_2_id INTEGER NULL;", cancellationToken);
            await EnsureColumnAsync(localDbContext, "training", "naechster_fahrer_zeitnahme_1_id", "ALTER TABLE training ADD COLUMN naechster_fahrer_zeitnahme_1_id INTEGER NULL;", cancellationToken);
            await EnsureColumnAsync(localDbContext, "training", "naechster_fahrer_zeitnahme_2_id", "ALTER TABLE training ADD COLUMN naechster_fahrer_zeitnahme_2_id INTEGER NULL;", cancellationToken);
            await localDbContext.Database.ExecuteSqlRawAsync(
                "UPDATE training SET uuid = lower(hex(randomblob(4))) || '-' || lower(hex(randomblob(2))) || '-4' || substr(lower(hex(randomblob(2))), 2) || '-' || substr('89ab', (random() & 3) + 1, 1) || substr(lower(hex(randomblob(2))), 2) || '-' || lower(hex(randomblob(6))) WHERE uuid IS NULL;",
                cancellationToken);
            await EnsureLocalIndexAsync(localDbContext, "training", "IX_training_uuid", "uuid", true, cancellationToken);
        }

        if (await TableExistsAsync(localDbContext, "meisterschaften", cancellationToken))
        {
            await EnsureColumnAsync(localDbContext, "meisterschaften", "uuid", "ALTER TABLE meisterschaften ADD COLUMN uuid TEXT NULL;", cancellationToken);
            await localDbContext.Database.ExecuteSqlRawAsync(
                "UPDATE meisterschaften SET uuid = lower(hex(randomblob(4))) || '-' || lower(hex(randomblob(2))) || '-4' || substr(lower(hex(randomblob(2))), 2) || '-' || substr('89ab', (random() & 3) + 1, 1) || substr(lower(hex(randomblob(2))), 2) || '-' || lower(hex(randomblob(6))) WHERE uuid IS NULL;",
                cancellationToken);
            await EnsureLocalIndexAsync(localDbContext, "meisterschaften", "IX_meisterschaften_uuid", "uuid", true, cancellationToken);
        }

        foreach (var tableName in SyncTables)
        {
            if (await TableExistsAsync(localDbContext, tableName, cancellationToken))
            {
                await EnsureLocalIndexAsync(localDbContext, tableName, $"IX_{tableName}_updated_at_utc", "updated_at_utc", false, cancellationToken);
            }
        }
    }

    private static async Task EnsureCurrentRemoteSchemaAsync(
        OpenSlalomDbContext remoteDbContext,
        CancellationToken cancellationToken)
    {
        if (await RemoteTableExistsAsync(remoteDbContext, "training", cancellationToken))
        {
            await EnsureRemoteColumnAsync(remoteDbContext, "training", "uuid", "ALTER TABLE `{0}` ADD COLUMN `uuid` char(36) COLLATE ascii_general_ci NULL;", cancellationToken);
            await EnsureRemoteColumnAsync(remoteDbContext, "training", "ist_veroeffentlicht", "ALTER TABLE `{0}` ADD COLUMN `ist_veroeffentlicht` tinyint(1) NOT NULL DEFAULT 0;", cancellationToken);
            await EnsureRemoteColumnAsync(remoteDbContext, "training", "aktiver_fahrer_zeitnahme_1_id", "ALTER TABLE `{0}` ADD COLUMN `aktiver_fahrer_zeitnahme_1_id` int NULL;", cancellationToken);
            await EnsureRemoteColumnAsync(remoteDbContext, "training", "aktiver_fahrer_zeitnahme_2_id", "ALTER TABLE `{0}` ADD COLUMN `aktiver_fahrer_zeitnahme_2_id` int NULL;", cancellationToken);
            await EnsureRemoteColumnAsync(remoteDbContext, "training", "naechster_fahrer_zeitnahme_1_id", "ALTER TABLE `{0}` ADD COLUMN `naechster_fahrer_zeitnahme_1_id` int NULL;", cancellationToken);
            await EnsureRemoteColumnAsync(remoteDbContext, "training", "naechster_fahrer_zeitnahme_2_id", "ALTER TABLE `{0}` ADD COLUMN `naechster_fahrer_zeitnahme_2_id` int NULL;", cancellationToken);
            await remoteDbContext.Database.ExecuteSqlRawAsync("UPDATE `training` SET `uuid` = UUID() WHERE `uuid` IS NULL;", cancellationToken);
            await EnsureRemoteIndexAsync(remoteDbContext, "training", "IX_training_uuid", "uuid", true, cancellationToken);
        }

        if (await RemoteTableExistsAsync(remoteDbContext, "meisterschaften", cancellationToken))
        {
            await EnsureRemoteColumnAsync(remoteDbContext, "meisterschaften", "uuid", "ALTER TABLE `{0}` ADD COLUMN `uuid` char(36) COLLATE ascii_general_ci NULL;", cancellationToken);
            await remoteDbContext.Database.ExecuteSqlRawAsync("UPDATE `meisterschaften` SET `uuid` = UUID() WHERE `uuid` IS NULL;", cancellationToken);
            await EnsureRemoteIndexAsync(remoteDbContext, "meisterschaften", "IX_meisterschaften_uuid", "uuid", true, cancellationToken);
        }

        foreach (var tableName in SyncTables)
        {
            if (await RemoteTableExistsAsync(remoteDbContext, tableName, cancellationToken))
            {
                await EnsureRemoteIndexAsync(remoteDbContext, tableName, $"IX_{tableName}_updated_at_utc", "updated_at_utc", false, cancellationToken);
            }
        }
    }

    private static async Task ApplyLegacyLocalSchemaPatchesAsync(
        LocalOpenSlalomDbContext localDbContext,
        CancellationToken cancellationToken)
    {
        await EnsureColumnAsync(localDbContext, "vereine", "mitglieds_nummer", "ALTER TABLE vereine ADD COLUMN mitglieds_nummer TEXT NOT NULL DEFAULT '';", cancellationToken);
        await EnsureColumnAsync(localDbContext, "vereine", "postleitzahl", "ALTER TABLE vereine ADD COLUMN postleitzahl TEXT NOT NULL DEFAULT '';", cancellationToken);
        await EnsureColumnAsync(localDbContext, "vereine", "ort", "ALTER TABLE vereine ADD COLUMN ort TEXT NOT NULL DEFAULT '';", cancellationToken);
        await EnsureColumnAsync(localDbContext, "vereine", "adresse", "ALTER TABLE vereine ADD COLUMN adresse TEXT NOT NULL DEFAULT '';", cancellationToken);
        await EnsureColumnAsync(localDbContext, "vereine", "logo", "ALTER TABLE vereine ADD COLUMN logo BLOB NULL;", cancellationToken);
        await EnsureColumnAsync(localDbContext, "fahrer", "mitglieds_nummer", "ALTER TABLE fahrer ADD COLUMN mitglieds_nummer TEXT NOT NULL DEFAULT '';", cancellationToken);
        await EnsureColumnAsync(localDbContext, "fahrer", "geburtsdatum", "ALTER TABLE fahrer ADD COLUMN geburtsdatum TEXT NULL;", cancellationToken);
        await EnsureColumnAsync(localDbContext, "fahrer", "geschlecht", "ALTER TABLE fahrer ADD COLUMN geschlecht TEXT NOT NULL DEFAULT '';", cancellationToken);
        await EnsureColumnAsync(localDbContext, "training", "training_abgeschlossen", "ALTER TABLE training ADD COLUMN training_abgeschlossen INTEGER NOT NULL DEFAULT 0;", cancellationToken);
        await EnsureColumnAsync(localDbContext, "training", "ist_veroeffentlicht", "ALTER TABLE training ADD COLUMN ist_veroeffentlicht INTEGER NOT NULL DEFAULT 0;", cancellationToken);
        await EnsureColumnAsync(localDbContext, "training", "aktiver_fahrer_zeitnahme_1_id", "ALTER TABLE training ADD COLUMN aktiver_fahrer_zeitnahme_1_id INTEGER NULL;", cancellationToken);
        await EnsureColumnAsync(localDbContext, "training", "aktiver_fahrer_zeitnahme_2_id", "ALTER TABLE training ADD COLUMN aktiver_fahrer_zeitnahme_2_id INTEGER NULL;", cancellationToken);
        await EnsureColumnAsync(localDbContext, "training", "naechster_fahrer_zeitnahme_1_id", "ALTER TABLE training ADD COLUMN naechster_fahrer_zeitnahme_1_id INTEGER NULL;", cancellationToken);
        await EnsureColumnAsync(localDbContext, "training", "naechster_fahrer_zeitnahme_2_id", "ALTER TABLE training ADD COLUMN naechster_fahrer_zeitnahme_2_id INTEGER NULL;", cancellationToken);
        await EnsureColumnAsync(localDbContext, "training", "uuid", "ALTER TABLE training ADD COLUMN uuid TEXT NULL;", cancellationToken);
        var hasMeisterschaften = await TableExistsAsync(localDbContext, "meisterschaften", cancellationToken);
        if (hasMeisterschaften)
        {
            await EnsureColumnAsync(localDbContext, "meisterschaften", "uuid", "ALTER TABLE meisterschaften ADD COLUMN uuid TEXT NULL;", cancellationToken);
        }
        await EnsureColumnAsync(localDbContext, "fahrer_im_training", "reihenfolge", "ALTER TABLE fahrer_im_training ADD COLUMN reihenfolge INTEGER NOT NULL DEFAULT 0;", cancellationToken);
        await EnsureColumnAsync(localDbContext, "fahrer_im_training", "fahrer_faehrt", "ALTER TABLE fahrer_im_training ADD COLUMN fahrer_faehrt INTEGER NOT NULL DEFAULT 1;", cancellationToken);
        await EnsureColumnAsync(localDbContext, "tstints", "altersklasse_snapshot", "ALTER TABLE tstints ADD COLUMN altersklasse_snapshot TEXT NOT NULL DEFAULT '';", cancellationToken);

        await EnsureSyncMetadataColumnsAsync(localDbContext, cancellationToken);

        foreach (var tableName in SyncTables)
        {
            if (await TableExistsAsync(localDbContext, tableName, cancellationToken))
            {
#pragma warning disable EF1002 // Table names come exclusively from the fixed SyncTables allowlist.
                await localDbContext.Database.ExecuteSqlRawAsync(
                    $"CREATE INDEX IF NOT EXISTS IX_{tableName}_updated_at_utc ON {tableName} (updated_at_utc);",
                    cancellationToken);
#pragma warning restore EF1002
            }
        }

        await localDbContext.Database.ExecuteSqlRawAsync(
            "UPDATE training SET uuid = lower(hex(randomblob(4))) || '-' || lower(hex(randomblob(2))) || '-4' || substr(lower(hex(randomblob(2))), 2) || '-' || substr('89ab', (random() & 3) + 1, 1) || substr(lower(hex(randomblob(2))), 2) || '-' || lower(hex(randomblob(6))) WHERE uuid IS NULL;",
            cancellationToken);
        await localDbContext.Database.ExecuteSqlRawAsync(
            "CREATE UNIQUE INDEX IF NOT EXISTS IX_training_uuid ON training (uuid);",
            cancellationToken);
        if (hasMeisterschaften)
        {
            await localDbContext.Database.ExecuteSqlRawAsync(
                "UPDATE meisterschaften SET uuid = lower(hex(randomblob(4))) || '-' || lower(hex(randomblob(2))) || '-4' || substr(lower(hex(randomblob(2))), 2) || '-' || substr('89ab', (random() & 3) + 1, 1) || substr(lower(hex(randomblob(2))), 2) || '-' || lower(hex(randomblob(6))) WHERE uuid IS NULL;",
                cancellationToken);
            await localDbContext.Database.ExecuteSqlRawAsync(
                "CREATE UNIQUE INDEX IF NOT EXISTS IX_meisterschaften_uuid ON meisterschaften (uuid);",
                cancellationToken);
        }

        await localDbContext.Database.ExecuteSqlRawAsync(
            "CREATE TABLE IF NOT EXISTS sync_state (id TEXT NOT NULL CONSTRAINT PK_sync_state PRIMARY KEY, last_sync_utc TEXT NOT NULL);",
            cancellationToken);
    }

    private static async Task EnsureSyncMetadataColumnsAsync(
        LocalOpenSlalomDbContext localDbContext,
        CancellationToken cancellationToken)
    {
        foreach (var tableName in SyncTables)
        {
            if (!await TableExistsAsync(localDbContext, tableName, cancellationToken))
            {
                continue;
            }

            await EnsureColumnAsync(
                localDbContext,
                tableName,
                "updated_at_utc",
                $"ALTER TABLE {tableName} ADD COLUMN updated_at_utc TEXT NOT NULL DEFAULT {SyncDefaultTimestamp};",
                cancellationToken);
            await EnsureColumnAsync(localDbContext, tableName, "is_deleted", $"ALTER TABLE {tableName} ADD COLUMN is_deleted INTEGER NOT NULL DEFAULT 0;", cancellationToken);
            await EnsureColumnAsync(localDbContext, tableName, "deleted_at_utc", $"ALTER TABLE {tableName} ADD COLUMN deleted_at_utc TEXT NULL;", cancellationToken);

#pragma warning disable EF1002
            await localDbContext.Database.ExecuteSqlRawAsync(
                $"UPDATE {tableName} SET updated_at_utc = CURRENT_TIMESTAMP WHERE updated_at_utc IS NULL OR updated_at_utc = {SyncDefaultTimestamp};",
                cancellationToken);
#pragma warning restore EF1002
        }
    }

    private static async Task RepairPartiallyAppliedSyncMetadataMigrationAsync(
        LocalOpenSlalomDbContext localDbContext,
        CancellationToken cancellationToken)
    {
        if (await HasMigrationHistoryEntryAsync(localDbContext, SqliteSyncMetadataMigrationId, cancellationToken))
        {
            return;
        }

        var hasPartialSyncSchema = await HasAnySyncSchemaArtifactAsync(localDbContext, cancellationToken);
        if (!hasPartialSyncSchema)
        {
            return;
        }

        Logger.Warn("Unvollstaendige SQLite-Sync-Migration erkannt. Repariere lokales Schema und setze Migrationshistorie.");

        await EnsureSyncMetadataColumnsAsync(localDbContext, cancellationToken);
        await localDbContext.Database.ExecuteSqlRawAsync(
            "CREATE TABLE IF NOT EXISTS sync_state (id TEXT NOT NULL CONSTRAINT PK_sync_state PRIMARY KEY, last_sync_utc TEXT NOT NULL);",
            cancellationToken);

        var efProductVersion = typeof(DbContext).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion?
            .Split('+')[0] ?? "8.0.2";

        await localDbContext.Database.ExecuteSqlInterpolatedAsync(
            $"INSERT OR IGNORE INTO __EFMigrationsHistory (MigrationId, ProductVersion) VALUES ({SqliteSyncMetadataMigrationId}, {efProductVersion});",
            cancellationToken);
    }

    private static async Task<bool> HasAnySyncSchemaArtifactAsync(
        LocalOpenSlalomDbContext localDbContext,
        CancellationToken cancellationToken)
    {
        if (await TableExistsAsync(localDbContext, "sync_state", cancellationToken))
        {
            return true;
        }

        foreach (var tableName in SyncTables)
        {
            if (await ColumnExistsAsync(localDbContext, tableName, "updated_at_utc", cancellationToken) ||
                await ColumnExistsAsync(localDbContext, tableName, "is_deleted", cancellationToken) ||
                await ColumnExistsAsync(localDbContext, tableName, "deleted_at_utc", cancellationToken))
            {
                return true;
            }
        }

        return false;
    }

    private static async Task RepairPartiallyAppliedRemoteSyncMetadataMigrationAsync(
        OpenSlalomDbContext remoteDbContext,
        CancellationToken cancellationToken)
    {
        var hasHistoryEntry = await HasRemoteMigrationHistoryEntryAsync(remoteDbContext, MySqlSyncMetadataMigrationId, cancellationToken);
        var hasAnySyncSchemaArtifact = await HasAnyRemoteSyncSchemaArtifactAsync(remoteDbContext, cancellationToken);
        var hasCompleteSyncSchema = await HasCompleteRemoteSyncSchemaAsync(remoteDbContext, cancellationToken);

        if (hasHistoryEntry && hasCompleteSyncSchema)
        {
            return;
        }

        if (!hasHistoryEntry && !hasAnySyncSchemaArtifact)
        {
            return;
        }

        Logger.Warn("Inkonsistente Remote-MySQL-Sync-Migration erkannt. Repariere Schema und setze Migrationshistorie.");

        await EnsureRemoteSyncMetadataColumnsAsync(remoteDbContext, cancellationToken);
        await EnsureRemoteSyncStateTableAsync(remoteDbContext, cancellationToken);

        if (!hasHistoryEntry)
        {
            var efProductVersion = typeof(DbContext).Assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                .InformationalVersion?
                .Split('+')[0] ?? "8.0.2";

            await remoteDbContext.Database.ExecuteSqlInterpolatedAsync(
                $"INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion) VALUES ({MySqlSyncMetadataMigrationId}, {efProductVersion}) ON DUPLICATE KEY UPDATE ProductVersion = ProductVersion;",
                cancellationToken);
        }
    }

    private static async Task EnsureRemoteSyncMetadataColumnsAsync(
        OpenSlalomDbContext remoteDbContext,
        CancellationToken cancellationToken)
    {
        foreach (var tableName in SyncTables)
        {
            if (!await RemoteTableExistsAsync(remoteDbContext, tableName, cancellationToken))
            {
                continue;
            }

            await EnsureRemoteColumnAsync(remoteDbContext, tableName, "updated_at_utc", "ALTER TABLE `{0}` ADD COLUMN `updated_at_utc` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP;", cancellationToken);
            await EnsureRemoteColumnAsync(remoteDbContext, tableName, "is_deleted", "ALTER TABLE `{0}` ADD COLUMN `is_deleted` tinyint(1) NOT NULL DEFAULT 0;", cancellationToken);
            await EnsureRemoteColumnAsync(remoteDbContext, tableName, "deleted_at_utc", "ALTER TABLE `{0}` ADD COLUMN `deleted_at_utc` datetime NULL;", cancellationToken);

#pragma warning disable EF1002
            await remoteDbContext.Database.ExecuteSqlRawAsync(
                $"UPDATE `{tableName}` SET `updated_at_utc` = CURRENT_TIMESTAMP WHERE `updated_at_utc` IS NULL;",
                cancellationToken);
#pragma warning restore EF1002
        }
    }

    private static async Task EnsureRemoteSyncStateTableAsync(
        OpenSlalomDbContext remoteDbContext,
        CancellationToken cancellationToken)
    {
        await remoteDbContext.Database.ExecuteSqlRawAsync(
            "CREATE TABLE IF NOT EXISTS `sync_state` (`id` varchar(50) NOT NULL, `last_sync_utc` datetime NOT NULL, PRIMARY KEY (`id`)) CHARACTER SET utf8mb4;",
            cancellationToken);
    }

    private static async Task EnsureRemoteColumnAsync(
        OpenSlalomDbContext remoteDbContext,
        string tableName,
        string columnName,
        string alterStatementFormat,
        CancellationToken cancellationToken)
    {
        if (await RemoteColumnExistsAsync(remoteDbContext, tableName, columnName, cancellationToken))
        {
            return;
        }

#pragma warning disable EF1002
        await remoteDbContext.Database.ExecuteSqlRawAsync(string.Format(alterStatementFormat, tableName), cancellationToken);
#pragma warning restore EF1002
}

    private static async Task EnsureLocalIndexAsync(
        LocalOpenSlalomDbContext localDbContext,
        string tableName,
        string indexName,
        string columnName,
        bool unique,
        CancellationToken cancellationToken)
    {
#pragma warning disable EF1002
        var matchingIndex = await localDbContext.Database.SqlQueryRaw<int>(
                $"SELECT COUNT(1) AS Value FROM pragma_index_list('{tableName}') il INNER JOIN pragma_index_info(il.name) ii ON 1 = 1 WHERE il.name = '{indexName}' AND il.\"unique\" = {(unique ? 1 : 0)} AND ii.name = '{columnName}'")
            .SingleAsync(cancellationToken);
#pragma warning restore EF1002

        if (matchingIndex > 0)
        {
            return;
        }

        if (await LocalIndexExistsAsync(localDbContext, tableName, indexName, cancellationToken))
        {
#pragma warning disable EF1002
            await localDbContext.Database.ExecuteSqlRawAsync($"DROP INDEX IF EXISTS {indexName};", cancellationToken);
#pragma warning restore EF1002
        }

#pragma warning disable EF1002
        await localDbContext.Database.ExecuteSqlRawAsync(
            $"CREATE {(unique ? "UNIQUE " : string.Empty)}INDEX {indexName} ON {tableName} ({columnName});",
            cancellationToken);
#pragma warning restore EF1002
    }

    private static async Task<bool> LocalIndexExistsAsync(
        LocalOpenSlalomDbContext localDbContext,
        string tableName,
        string indexName,
        CancellationToken cancellationToken)
    {
#pragma warning disable EF1002
        return await localDbContext.Database.SqlQueryRaw<int>(
                $"SELECT COUNT(1) AS Value FROM pragma_index_list('{tableName}') WHERE name = '{indexName}'")
            .SingleAsync(cancellationToken) > 0;
#pragma warning restore EF1002
    }

    private static async Task EnsureRemoteIndexAsync(
        OpenSlalomDbContext remoteDbContext,
        string tableName,
        string indexName,
        string columnName,
        bool unique,
        CancellationToken cancellationToken)
    {
#pragma warning disable EF1002
        var matchingIndex = await remoteDbContext.Database.SqlQueryRaw<int>(
                $"SELECT COUNT(1) AS Value FROM INFORMATION_SCHEMA.STATISTICS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = '{tableName}' AND INDEX_NAME = '{indexName}' AND COLUMN_NAME = '{columnName}' AND NON_UNIQUE = {(unique ? 0 : 1)}")
            .SingleAsync(cancellationToken);
#pragma warning restore EF1002

        if (matchingIndex > 0)
        {
            return;
        }

        if (await RemoteIndexExistsAsync(remoteDbContext, tableName, indexName, cancellationToken))
        {
#pragma warning disable EF1002
            await remoteDbContext.Database.ExecuteSqlRawAsync($"DROP INDEX `{indexName}` ON `{tableName}`;", cancellationToken);
#pragma warning restore EF1002
        }

#pragma warning disable EF1002
        await remoteDbContext.Database.ExecuteSqlRawAsync(
            $"CREATE {(unique ? "UNIQUE " : string.Empty)}INDEX `{indexName}` ON `{tableName}` (`{columnName}`);",
            cancellationToken);
#pragma warning restore EF1002
    }

    private static async Task<bool> RemoteIndexExistsAsync(
        OpenSlalomDbContext remoteDbContext,
        string tableName,
        string indexName,
        CancellationToken cancellationToken)
    {
#pragma warning disable EF1002
        return await remoteDbContext.Database.SqlQueryRaw<int>(
                $"SELECT COUNT(1) AS Value FROM INFORMATION_SCHEMA.STATISTICS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = '{tableName}' AND INDEX_NAME = '{indexName}'")
            .SingleAsync(cancellationToken) > 0;
#pragma warning restore EF1002
    }

    private static async Task<bool> HasAnyRemoteSyncSchemaArtifactAsync(
        OpenSlalomDbContext remoteDbContext,
        CancellationToken cancellationToken)
    {
        if (await RemoteTableExistsAsync(remoteDbContext, "sync_state", cancellationToken))
        {
            return true;
        }

        foreach (var tableName in SyncTables)
        {
            if (await RemoteColumnExistsAsync(remoteDbContext, tableName, "updated_at_utc", cancellationToken) ||
                await RemoteColumnExistsAsync(remoteDbContext, tableName, "is_deleted", cancellationToken) ||
                await RemoteColumnExistsAsync(remoteDbContext, tableName, "deleted_at_utc", cancellationToken))
            {
                return true;
            }
        }

        return false;
    }

    private static async Task<bool> HasCompleteRemoteSyncSchemaAsync(
        OpenSlalomDbContext remoteDbContext,
        CancellationToken cancellationToken)
    {
        if (!await RemoteTableExistsAsync(remoteDbContext, "sync_state", cancellationToken))
        {
            return false;
        }

        foreach (var tableName in SyncTables)
        {
            if (!await RemoteColumnExistsAsync(remoteDbContext, tableName, "updated_at_utc", cancellationToken) ||
                !await RemoteColumnExistsAsync(remoteDbContext, tableName, "is_deleted", cancellationToken) ||
                !await RemoteColumnExistsAsync(remoteDbContext, tableName, "deleted_at_utc", cancellationToken))
            {
                return false;
            }
        }

        return true;
    }

    private static async Task<bool> RemoteTableExistsAsync(
        OpenSlalomDbContext remoteDbContext,
        string tableName,
        CancellationToken cancellationToken)
    {
#pragma warning disable EF1002
        return await remoteDbContext.Database.SqlQueryRaw<int>(
                $"SELECT COUNT(1) AS Value FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = '{tableName}'")
            .SingleAsync(cancellationToken) > 0;
#pragma warning restore EF1002
    }

    private static async Task<bool> RemoteColumnExistsAsync(
        OpenSlalomDbContext remoteDbContext,
        string tableName,
        string columnName,
        CancellationToken cancellationToken)
    {
#pragma warning disable EF1002
        return await remoteDbContext.Database.SqlQueryRaw<int>(
                $"SELECT COUNT(1) AS Value FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = '{tableName}' AND COLUMN_NAME = '{columnName}'")
            .SingleAsync(cancellationToken) > 0;
#pragma warning restore EF1002
    }

    private static async Task<bool> HasRemoteMigrationHistoryEntryAsync(
        OpenSlalomDbContext remoteDbContext,
        string migrationId,
        CancellationToken cancellationToken)
    {
        if (!await RemoteTableExistsAsync(remoteDbContext, "__EFMigrationsHistory", cancellationToken))
        {
            return false;
        }

#pragma warning disable EF1002
        return await remoteDbContext.Database.SqlQueryRaw<int>(
                $"SELECT COUNT(1) AS Value FROM __EFMigrationsHistory WHERE MigrationId = '{migrationId}'")
            .SingleAsync(cancellationToken) > 0;
#pragma warning restore EF1002
    }

    private static async Task EnsureColumnAsync(
        LocalOpenSlalomDbContext localDbContext,
        string tableName,
        string columnName,
        string alterStatement,
        CancellationToken cancellationToken)
    {
        var exists = await ColumnExistsAsync(localDbContext, tableName, columnName, cancellationToken);

        if (!exists)
        {
            await localDbContext.Database.ExecuteSqlRawAsync(alterStatement, cancellationToken);
        }
    }

    private static async Task<bool> ColumnExistsAsync(
        LocalOpenSlalomDbContext localDbContext,
        string tableName,
        string columnName,
        CancellationToken cancellationToken)
    {
#pragma warning disable EF1002
        return await localDbContext.Database.SqlQueryRaw<int>(
                $"SELECT COUNT(1) AS Value FROM pragma_table_info('{tableName}') WHERE name='{columnName}'")
            .SingleAsync(cancellationToken) > 0;
#pragma warning restore EF1002
    }

    private static async Task<bool> TableExistsAsync(
        LocalOpenSlalomDbContext localDbContext,
        string tableName,
        CancellationToken cancellationToken)
    {
#pragma warning disable EF1002
        return await localDbContext.Database.SqlQueryRaw<int>(
                $"SELECT COUNT(1) AS Value FROM sqlite_master WHERE type='table' AND name='{tableName}'")
            .SingleAsync(cancellationToken) > 0;
#pragma warning restore EF1002
    }

    private static async Task<bool> HasMigrationHistoryEntryAsync(
        LocalOpenSlalomDbContext localDbContext,
        string migrationId,
        CancellationToken cancellationToken)
    {
        if (!await TableExistsAsync(localDbContext, "__EFMigrationsHistory", cancellationToken))
        {
            return false;
        }

#pragma warning disable EF1002
        return await localDbContext.Database.SqlQueryRaw<int>(
                $"SELECT COUNT(1) AS Value FROM __EFMigrationsHistory WHERE MigrationId = '{migrationId}'")
            .SingleAsync(cancellationToken) > 0;
#pragma warning restore EF1002
    }

    private static async Task BootstrapLegacySqliteMigrationHistoryAsync(
        LocalOpenSlalomDbContext localDbContext,
        CancellationToken cancellationToken)
    {
        await localDbContext.Database.ExecuteSqlRawAsync(
            "CREATE TABLE IF NOT EXISTS __EFMigrationsHistory (MigrationId TEXT NOT NULL CONSTRAINT PK___EFMigrationsHistory PRIMARY KEY, ProductVersion TEXT NOT NULL);",
            cancellationToken);

        var allMigrations = localDbContext.Database.GetMigrations();
        var efProductVersion = typeof(DbContext).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion?
            .Split('+')[0] ?? "8.0.2";

        foreach (var migrationId in allMigrations)
        {
            await localDbContext.Database.ExecuteSqlInterpolatedAsync(
                $"INSERT OR IGNORE INTO __EFMigrationsHistory (MigrationId, ProductVersion) VALUES ({migrationId}, {efProductVersion});",
                cancellationToken);
        }
    }

    private sealed record InitializationState(bool Success, string? Error)
    {
        public static InitializationState Connected() => new(true, null);

        public static InitializationState Failed(string? error) => new(false, error ?? "Unbekannter Fehler.");
    }
}
