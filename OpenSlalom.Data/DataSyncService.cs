using Microsoft.EntityFrameworkCore;
using NLog;
using OpenSlalom.Data.Entities;
using System.Linq.Expressions;

namespace OpenSlalom.Data;

public sealed class DataSyncService(
    IDbContextFactory<LocalOpenSlalomDbContext> localDbContextFactory,
    IDbContextFactory<RemoteOpenSlalomDbContext> remoteDbContextFactory)
{
    private const string SyncScopeId = "global";
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public async Task<DataSyncStatus> GetSyncStatusAsync(CancellationToken cancellationToken = default)
    {
        await using var localDb = await localDbContextFactory.CreateDbContextAsync(cancellationToken);
        await using var remoteDb = await remoteDbContextFactory.CreateDbContextAsync(cancellationToken);

        var localConnected = await localDb.Database.CanConnectAsync(cancellationToken);
        var remoteConnected = await remoteDb.Database.CanConnectAsync(cancellationToken);

        if (!localConnected)
        {
            return new DataSyncStatus(false, 0, 0, null, "Lokale SQLite ist nicht erreichbar.");
        }

        if (!remoteConnected)
        {
            return new DataSyncStatus(false, 0, 0, null, "Remote-MySQL ist nicht erreichbar.");
        }

        var localState = await localDb.SyncStates.AsNoTracking().FirstOrDefaultAsync(x => x.Id == SyncScopeId, cancellationToken);
        var remoteState = await remoteDb.SyncStates.AsNoTracking().FirstOrDefaultAsync(x => x.Id == SyncScopeId, cancellationToken);
        var lastSyncUtc = MinNullable(localState?.LastSyncUtc, remoteState?.LastSyncUtc);

        if (lastSyncUtc is null)
        {
            return new DataSyncStatus(true, 1, 1, null, "Synchronisierung noch nie ausgefuehrt.");
        }

        var localPending = await CountPendingChangesAsync(localDb, lastSyncUtc.Value, cancellationToken);
        var remotePending = await CountPendingChangesAsync(remoteDb, lastSyncUtc.Value, cancellationToken);
        var needed = localPending > 0 || remotePending > 0;
        var hasDataDrift = false;

        if (!needed)
        {
            hasDataDrift = await HasDataDriftAsync(localDb, remoteDb, cancellationToken);
            needed = hasDataDrift;
        }

        var message = needed
            ? hasDataDrift
                ? "Synchronisierung noetig (lokale und remote Daten sind unterschiedlich)."
                : $"Synchronisierung noetig (Lokal: {localPending}, Remote: {remotePending})."
            : "Alle Datenbanken sind synchron.";

        return new DataSyncStatus(needed, localPending, remotePending, lastSyncUtc, message);
    }

    public async Task<DataSyncResult> SyncBidirectionalAsync(CancellationToken cancellationToken = default)
    {
        await using var localDb = await localDbContextFactory.CreateDbContextAsync(cancellationToken);
        await using var remoteDb = await remoteDbContextFactory.CreateDbContextAsync(cancellationToken);

        localDb.SuppressSyncTracking = true;
        remoteDb.SuppressSyncTracking = true;

        if (!await localDb.Database.CanConnectAsync(cancellationToken))
        {
            return new DataSyncResult(false, "Lokale SQLite ist nicht erreichbar.");
        }

        if (!await remoteDb.Database.CanConnectAsync(cancellationToken))
        {
            return new DataSyncResult(false, "Remote-MySQL ist aktuell nicht erreichbar.");
        }

        await localDb.Database.MigrateAsync(cancellationToken);
        await remoteDb.Database.MigrateAsync(cancellationToken);

        await using var txLocal = await localDb.Database.BeginTransactionAsync(cancellationToken);
        await using var txRemote = await remoteDb.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            await SyncByIntKeyAsync(localDb, remoteDb, localDb.Disziplinen, remoteDb.Disziplinen,
                x => x.Id,
                x => new Disziplin
                {
                    Id = x.Id,
                    Name = x.Name,
                    ZeitstrafeTorfehler = x.ZeitstrafeTorfehler,
                    ZeitstrafePylonenfehler = x.ZeitstrafePylonenfehler,
                    UpdatedAtUtc = x.UpdatedAtUtc,
                    IsDeleted = x.IsDeleted,
                    DeletedAtUtc = x.DeletedAtUtc
                },
                (target, source) =>
                {
                    target.Name = source.Name;
                    target.ZeitstrafeTorfehler = source.ZeitstrafeTorfehler;
                    target.ZeitstrafePylonenfehler = source.ZeitstrafePylonenfehler;
                    CopySyncFields(target, source);
                }, cancellationToken);

            await SyncByIntKeyAsync(localDb, remoteDb, localDb.DisziplinAltersklassen, remoteDb.DisziplinAltersklassen,
                x => x.Id,
                x => new DisziplinAltersklasse
                {
                    Id = x.Id,
                    DisziplinId = x.DisziplinId,
                    Bezeichnung = x.Bezeichnung,
                    AlterVon = x.AlterVon,
                    AlterBis = x.AlterBis,
                    UpdatedAtUtc = x.UpdatedAtUtc,
                    IsDeleted = x.IsDeleted,
                    DeletedAtUtc = x.DeletedAtUtc
                },
                (target, source) =>
                {
                    target.DisziplinId = source.DisziplinId;
                    target.Bezeichnung = source.Bezeichnung;
                    target.AlterVon = source.AlterVon;
                    target.AlterBis = source.AlterBis;
                    CopySyncFields(target, source);
                }, cancellationToken);

            await SyncByIntKeyAsync(localDb, remoteDb, localDb.Vereine, remoteDb.Vereine,
                x => x.Id,
                x => new Verein
                {
                    Id = x.Id,
                    Vereinsname = x.Vereinsname,
                    MitgliedsNummer = x.MitgliedsNummer,
                    Postleitzahl = x.Postleitzahl,
                    Ort = x.Ort,
                    Adresse = x.Adresse,
                    Logo = x.Logo,
                    UpdatedAtUtc = x.UpdatedAtUtc,
                    IsDeleted = x.IsDeleted,
                    DeletedAtUtc = x.DeletedAtUtc
                },
                (target, source) =>
                {
                    target.Vereinsname = source.Vereinsname;
                    target.MitgliedsNummer = source.MitgliedsNummer;
                    target.Postleitzahl = source.Postleitzahl;
                    target.Ort = source.Ort;
                    target.Adresse = source.Adresse;
                    target.Logo = source.Logo;
                    CopySyncFields(target, source);
                }, cancellationToken);

            await SyncByIntKeyAsync(localDb, remoteDb, localDb.Wetterlagen, remoteDb.Wetterlagen,
                x => x.Id,
                x => new Wetter
                {
                    Id = x.Id,
                    Bezeichnung = x.Bezeichnung,
                    UpdatedAtUtc = x.UpdatedAtUtc,
                    IsDeleted = x.IsDeleted,
                    DeletedAtUtc = x.DeletedAtUtc
                },
                (target, source) =>
                {
                    target.Bezeichnung = source.Bezeichnung;
                    CopySyncFields(target, source);
                }, cancellationToken);

            await localDb.SaveChangesAsync(cancellationToken);
            await remoteDb.SaveChangesAsync(cancellationToken);

            await SyncByIntKeyAsync(localDb, remoteDb, localDb.Fahrer, remoteDb.Fahrer,
                x => x.Id,
                x => new Fahrer
                {
                    Id = x.Id,
                    VereinId = x.VereinId,
                    Vorname = x.Vorname,
                    Nachname = x.Nachname,
                    MitgliedsNummer = x.MitgliedsNummer,
                    Geburtsdatum = x.Geburtsdatum,
                    Geschlecht = x.Geschlecht,
                    UpdatedAtUtc = x.UpdatedAtUtc,
                    IsDeleted = x.IsDeleted,
                    DeletedAtUtc = x.DeletedAtUtc
                },
                (target, source) =>
                {
                    target.VereinId = source.VereinId;
                    target.Vorname = source.Vorname;
                    target.Nachname = source.Nachname;
                    target.MitgliedsNummer = source.MitgliedsNummer;
                    target.Geburtsdatum = source.Geburtsdatum;
                    target.Geschlecht = source.Geschlecht;
                    CopySyncFields(target, source);
                }, cancellationToken);

            await SyncByIntKeyAsync(localDb, remoteDb, localDb.Trainings, remoteDb.Trainings,
                x => x.Id,
                x => new Training
                {
                    Id = x.Id,
                    VereinId = x.VereinId,
                    DisziplinId = x.DisziplinId,
                    WetterId = x.WetterId,
                    Name = x.Name,
                    Beschreibung = x.Beschreibung,
                    Zeitpunkt = x.Zeitpunkt,
                    TrainingAbgeschlossen = x.TrainingAbgeschlossen,
                    UpdatedAtUtc = x.UpdatedAtUtc,
                    IsDeleted = x.IsDeleted,
                    DeletedAtUtc = x.DeletedAtUtc
                },
                (target, source) =>
                {
                    target.VereinId = source.VereinId;
                    target.DisziplinId = source.DisziplinId;
                    target.WetterId = source.WetterId;
                    target.Name = source.Name;
                    target.Beschreibung = source.Beschreibung;
                    target.Zeitpunkt = source.Zeitpunkt;
                    target.TrainingAbgeschlossen = source.TrainingAbgeschlossen;
                    CopySyncFields(target, source);
                }, cancellationToken);

            await SyncByIntKeyAsync(localDb, remoteDb, localDb.Meisterschaften, remoteDb.Meisterschaften,
                x => x.Id,
                x => new Meisterschaft
                {
                    Id = x.Id,
                    GastgeberId = x.GastgeberId,
                    DisziplinId = x.DisziplinId,
                    WetterId = x.WetterId,
                    Name = x.Name,
                    Beschreibung = x.Beschreibung,
                    Zeitpunkt = x.Zeitpunkt,
                    MeisterschaftAbgeschlossen = x.MeisterschaftAbgeschlossen,
                    AktivAusgerichtet = x.AktivAusgerichtet,
                    UpdatedAtUtc = x.UpdatedAtUtc,
                    IsDeleted = x.IsDeleted,
                    DeletedAtUtc = x.DeletedAtUtc
                },
                (target, source) =>
                {
                    target.GastgeberId = source.GastgeberId;
                    target.DisziplinId = source.DisziplinId;
                    target.WetterId = source.WetterId;
                    target.Name = source.Name;
                    target.Beschreibung = source.Beschreibung;
                    target.Zeitpunkt = source.Zeitpunkt;
                    target.MeisterschaftAbgeschlossen = source.MeisterschaftAbgeschlossen;
                    target.AktivAusgerichtet = source.AktivAusgerichtet;
                    CopySyncFields(target, source);
                }, cancellationToken);

            await SyncByIntKeyAsync(localDb, remoteDb, localDb.Karts, remoteDb.Karts,
                x => x.Id,
                x => new Kart
                {
                    Id = x.Id,
                    VereinId = x.VereinId,
                    DisziplinId = x.DisziplinId,
                    Name = x.Name,
                    Motor = x.Motor,
                    Chassis = x.Chassis,
                    UpdatedAtUtc = x.UpdatedAtUtc,
                    IsDeleted = x.IsDeleted,
                    DeletedAtUtc = x.DeletedAtUtc
                },
                (target, source) =>
                {
                    target.VereinId = source.VereinId;
                    target.DisziplinId = source.DisziplinId;
                    target.Name = source.Name;
                    target.Motor = source.Motor;
                    target.Chassis = source.Chassis;
                    CopySyncFields(target, source);
                }, cancellationToken);

            await localDb.SaveChangesAsync(cancellationToken);
            await remoteDb.SaveChangesAsync(cancellationToken);

            await SyncByIntKeyAsync(localDb, remoteDb, localDb.Tstints, remoteDb.Tstints,
                x => x.Id,
                x => new Tstint
                {
                    Id = x.Id,
                    TrainingId = x.TrainingId,
                    FahrerId = x.FahrerId,
                    KartId = x.KartId,
                    AltersklasseSnapshot = x.AltersklasseSnapshot,
                    Datum = x.Datum,
                    UpdatedAtUtc = x.UpdatedAtUtc,
                    IsDeleted = x.IsDeleted,
                    DeletedAtUtc = x.DeletedAtUtc
                },
                (target, source) =>
                {
                    target.TrainingId = source.TrainingId;
                    target.FahrerId = source.FahrerId;
                    target.KartId = source.KartId;
                    target.AltersklasseSnapshot = source.AltersklasseSnapshot;
                    target.Datum = source.Datum;
                    CopySyncFields(target, source);
                }, cancellationToken);

            await SyncByIntKeyAsync(localDb, remoteDb, localDb.Mstints, remoteDb.Mstints,
                x => x.Id,
                x => new Mstint
                {
                    Id = x.Id,
                    MeisterschaftId = x.MeisterschaftId,
                    FahrerId = x.FahrerId,
                    KartId = x.KartId,
                    AltersklasseSnapshot = x.AltersklasseSnapshot,
                    Datum = x.Datum,
                    UpdatedAtUtc = x.UpdatedAtUtc,
                    IsDeleted = x.IsDeleted,
                    DeletedAtUtc = x.DeletedAtUtc
                },
                (target, source) =>
                {
                    target.MeisterschaftId = source.MeisterschaftId;
                    target.FahrerId = source.FahrerId;
                    target.KartId = source.KartId;
                    target.AltersklasseSnapshot = source.AltersklasseSnapshot;
                    target.Datum = source.Datum;
                    CopySyncFields(target, source);
                }, cancellationToken);

            await SyncCompositeFahrerImTrainingAsync(localDb, remoteDb, cancellationToken);
            await SyncCompositeFahrerInDerMeisterschaftAsync(localDb, remoteDb, cancellationToken);

            await localDb.SaveChangesAsync(cancellationToken);
            await remoteDb.SaveChangesAsync(cancellationToken);

            await SyncByIntKeyAsync(localDb, remoteDb, localDb.Trunden, remoteDb.Trunden,
                x => x.Id,
                x => new Trunde
                {
                    Id = x.Id,
                    TstintId = x.TstintId,
                    Runde = x.Runde,
                    Rundenzeit = x.Rundenzeit,
                    Pf = x.Pf,
                    Tf = x.Tf,
                    Ungueltig = x.Ungueltig,
                    UpdatedAtUtc = x.UpdatedAtUtc,
                    IsDeleted = x.IsDeleted,
                    DeletedAtUtc = x.DeletedAtUtc
                },
                (target, source) =>
                {
                    target.TstintId = source.TstintId;
                    target.Runde = source.Runde;
                    target.Rundenzeit = source.Rundenzeit;
                    target.Pf = source.Pf;
                    target.Tf = source.Tf;
                    target.Ungueltig = source.Ungueltig;
                    CopySyncFields(target, source);
                }, cancellationToken);

            await SyncByIntKeyAsync(localDb, remoteDb, localDb.Mrunden, remoteDb.Mrunden,
                x => x.Id,
                x => new Mrunde
                {
                    Id = x.Id,
                    MstintId = x.MstintId,
                    Runde = x.Runde,
                    Rundenzeit = x.Rundenzeit,
                    Pf = x.Pf,
                    Tf = x.Tf,
                    Ungueltig = x.Ungueltig,
                    UpdatedAtUtc = x.UpdatedAtUtc,
                    IsDeleted = x.IsDeleted,
                    DeletedAtUtc = x.DeletedAtUtc
                },
                (target, source) =>
                {
                    target.MstintId = source.MstintId;
                    target.Runde = source.Runde;
                    target.Rundenzeit = source.Rundenzeit;
                    target.Pf = source.Pf;
                    target.Tf = source.Tf;
                    target.Ungueltig = source.Ungueltig;
                    CopySyncFields(target, source);
                }, cancellationToken);

            var syncTime = DateTime.UtcNow;
            await UpdateSyncStateAsync(localDb, syncTime, cancellationToken);
            await UpdateSyncStateAsync(remoteDb, syncTime, cancellationToken);

            await localDb.SaveChangesAsync(cancellationToken);
            await remoteDb.SaveChangesAsync(cancellationToken);

            await txLocal.CommitAsync(cancellationToken);
            await txRemote.CommitAsync(cancellationToken);

            Logger.Info("Bidirektionale Synchronisierung erfolgreich beendet.");
            return new DataSyncResult(true, "Bidirektionale Synchronisierung abgeschlossen.");
        }
        catch (Exception ex)
        {
            await txLocal.RollbackAsync(cancellationToken);
            await txRemote.RollbackAsync(cancellationToken);
            Logger.Error(ex, "Bidirektionale Synchronisierung fehlgeschlagen.");
            return new DataSyncResult(false, $"Synchronisierung fehlgeschlagen: {ex.Message}");
        }
        finally
        {
            localDb.SuppressSyncTracking = false;
            remoteDb.SuppressSyncTracking = false;
        }
    }

    private static async Task<int> CountPendingChangesAsync(OpenSlalomDbContext dbContext, DateTime lastSyncUtc, CancellationToken cancellationToken)
    {
        var total = 0;
        total += await dbContext.Disziplinen.IgnoreQueryFilters().CountAsync(x => x.UpdatedAtUtc > lastSyncUtc, cancellationToken);
        total += await dbContext.DisziplinAltersklassen.IgnoreQueryFilters().CountAsync(x => x.UpdatedAtUtc > lastSyncUtc, cancellationToken);
        total += await dbContext.Vereine.IgnoreQueryFilters().CountAsync(x => x.UpdatedAtUtc > lastSyncUtc, cancellationToken);
        total += await dbContext.Wetterlagen.IgnoreQueryFilters().CountAsync(x => x.UpdatedAtUtc > lastSyncUtc, cancellationToken);
        total += await dbContext.Fahrer.IgnoreQueryFilters().CountAsync(x => x.UpdatedAtUtc > lastSyncUtc, cancellationToken);
        total += await dbContext.Trainings.IgnoreQueryFilters().CountAsync(x => x.UpdatedAtUtc > lastSyncUtc, cancellationToken);
        total += await dbContext.Meisterschaften.IgnoreQueryFilters().CountAsync(x => x.UpdatedAtUtc > lastSyncUtc, cancellationToken);
        total += await dbContext.Karts.IgnoreQueryFilters().CountAsync(x => x.UpdatedAtUtc > lastSyncUtc, cancellationToken);
        total += await dbContext.Tstints.IgnoreQueryFilters().CountAsync(x => x.UpdatedAtUtc > lastSyncUtc, cancellationToken);
        total += await dbContext.Mstints.IgnoreQueryFilters().CountAsync(x => x.UpdatedAtUtc > lastSyncUtc, cancellationToken);
        total += await dbContext.FahrerImTrainings.IgnoreQueryFilters().CountAsync(x => x.UpdatedAtUtc > lastSyncUtc, cancellationToken);
        total += await dbContext.FahrerInDerMeisterschaften.IgnoreQueryFilters().CountAsync(x => x.UpdatedAtUtc > lastSyncUtc, cancellationToken);
        total += await dbContext.Trunden.IgnoreQueryFilters().CountAsync(x => x.UpdatedAtUtc > lastSyncUtc, cancellationToken);
        total += await dbContext.Mrunden.IgnoreQueryFilters().CountAsync(x => x.UpdatedAtUtc > lastSyncUtc, cancellationToken);
        return total;
    }

    private static async Task<bool> HasDataDriftAsync(
        LocalOpenSlalomDbContext localDb,
        RemoteOpenSlalomDbContext remoteDb,
        CancellationToken cancellationToken)
    {
        if (await HasIntKeyDriftAsync(localDb.Disziplinen, remoteDb.Disziplinen, x => x.Id, cancellationToken) ||
            await HasIntKeyDriftAsync(localDb.DisziplinAltersklassen, remoteDb.DisziplinAltersklassen, x => x.Id, cancellationToken) ||
            await HasIntKeyDriftAsync(localDb.Vereine, remoteDb.Vereine, x => x.Id, cancellationToken) ||
            await HasIntKeyDriftAsync(localDb.Wetterlagen, remoteDb.Wetterlagen, x => x.Id, cancellationToken) ||
            await HasIntKeyDriftAsync(localDb.Fahrer, remoteDb.Fahrer, x => x.Id, cancellationToken) ||
            await HasIntKeyDriftAsync(localDb.Trainings, remoteDb.Trainings, x => x.Id, cancellationToken) ||
            await HasIntKeyDriftAsync(localDb.Meisterschaften, remoteDb.Meisterschaften, x => x.Id, cancellationToken) ||
            await HasIntKeyDriftAsync(localDb.Karts, remoteDb.Karts, x => x.Id, cancellationToken) ||
            await HasIntKeyDriftAsync(localDb.Tstints, remoteDb.Tstints, x => x.Id, cancellationToken) ||
            await HasIntKeyDriftAsync(localDb.Mstints, remoteDb.Mstints, x => x.Id, cancellationToken) ||
            await HasIntKeyDriftAsync(localDb.Trunden, remoteDb.Trunden, x => x.Id, cancellationToken) ||
            await HasIntKeyDriftAsync(localDb.Mrunden, remoteDb.Mrunden, x => x.Id, cancellationToken))
        {
            return true;
        }

        var localCompositeKeys = await localDb.FahrerImTrainings
            .IgnoreQueryFilters()
            .Select(x => new { x.TrainingId, x.FahrerId })
            .ToListAsync(cancellationToken);

        var remoteCompositeKeys = await remoteDb.FahrerImTrainings
            .IgnoreQueryFilters()
            .Select(x => new { x.TrainingId, x.FahrerId })
            .ToListAsync(cancellationToken);

        if (localCompositeKeys.Count != remoteCompositeKeys.Count)
        {
            return true;
        }

        var localSet = localCompositeKeys
            .Select(x => (x.TrainingId, x.FahrerId))
            .ToHashSet();

        var remoteSet = remoteCompositeKeys
            .Select(x => (x.TrainingId, x.FahrerId))
            .ToHashSet();

        if (!localSet.SetEquals(remoteSet))
        {
            return true;
        }

        var localMeisterschaftKeys = await localDb.FahrerInDerMeisterschaften
            .IgnoreQueryFilters()
            .Select(x => new { x.MeisterschaftId, x.FahrerId })
            .ToListAsync(cancellationToken);

        var remoteMeisterschaftKeys = await remoteDb.FahrerInDerMeisterschaften
            .IgnoreQueryFilters()
            .Select(x => new { x.MeisterschaftId, x.FahrerId })
            .ToListAsync(cancellationToken);

        if (localMeisterschaftKeys.Count != remoteMeisterschaftKeys.Count)
        {
            return true;
        }

        var localMeisterschaftSet = localMeisterschaftKeys
            .Select(x => (x.MeisterschaftId, x.FahrerId))
            .ToHashSet();

        var remoteMeisterschaftSet = remoteMeisterschaftKeys
            .Select(x => (x.MeisterschaftId, x.FahrerId))
            .ToHashSet();

        return !localMeisterschaftSet.SetEquals(remoteMeisterschaftSet);
    }

    private static async Task<bool> HasIntKeyDriftAsync<TEntity>(
        DbSet<TEntity> localSet,
        DbSet<TEntity> remoteSet,
        Expression<Func<TEntity, int>> keySelector,
        CancellationToken cancellationToken)
        where TEntity : class
    {
        var localKeys = await localSet
            .IgnoreQueryFilters()
            .Select(keySelector)
            .ToListAsync(cancellationToken);

        var remoteKeys = await remoteSet
            .IgnoreQueryFilters()
            .Select(keySelector)
            .ToListAsync(cancellationToken);

        if (localKeys.Count != remoteKeys.Count)
        {
            return true;
        }

        return !localKeys.ToHashSet().SetEquals(remoteKeys);
    }

    private static DateTime? MinNullable(DateTime? left, DateTime? right)
    {
        if (left is null)
        {
            return right;
        }

        if (right is null)
        {
            return left;
        }

        return left.Value < right.Value ? left : right;
    }

    private static void CopySyncFields(ISyncEntity target, ISyncEntity source)
    {
        target.UpdatedAtUtc = source.UpdatedAtUtc;
        target.IsDeleted = source.IsDeleted;
        target.DeletedAtUtc = source.DeletedAtUtc;
    }

    private static async Task UpdateSyncStateAsync(OpenSlalomDbContext dbContext, DateTime syncTimeUtc, CancellationToken cancellationToken)
    {
        var state = await dbContext.SyncStates.FirstOrDefaultAsync(x => x.Id == SyncScopeId, cancellationToken);
        if (state is null)
        {
            dbContext.SyncStates.Add(new SyncState { Id = SyncScopeId, LastSyncUtc = syncTimeUtc });
            return;
        }

        state.LastSyncUtc = syncTimeUtc;
    }

    private static async Task SyncByIntKeyAsync<TEntity>(
        LocalOpenSlalomDbContext localDb,
        RemoteOpenSlalomDbContext remoteDb,
        DbSet<TEntity> localSet,
        DbSet<TEntity> remoteSet,
        Func<TEntity, int> keySelector,
        Func<TEntity, TEntity> clone,
        Action<TEntity, TEntity> apply,
        CancellationToken cancellationToken)
        where TEntity : class, ISyncEntity
    {
        var localItems = await localSet.IgnoreQueryFilters().ToListAsync(cancellationToken);
        var remoteItems = await remoteSet.IgnoreQueryFilters().ToListAsync(cancellationToken);

        var localMap = localItems.ToDictionary(keySelector);
        var remoteMap = remoteItems.ToDictionary(keySelector);

        var allKeys = new HashSet<int>(localMap.Keys);
        allKeys.UnionWith(remoteMap.Keys);

        foreach (var key in allKeys)
        {
            localMap.TryGetValue(key, out var localItem);
            remoteMap.TryGetValue(key, out var remoteItem);

            var winner = ChooseWinner(localItem, remoteItem);
            if (winner is null)
            {
                continue;
            }

            if (localItem is null)
            {
                localSet.Add(clone(winner));
            }
            else
            {
                apply(localItem, winner);
            }

            if (remoteItem is null)
            {
                remoteSet.Add(clone(winner));
            }
            else
            {
                apply(remoteItem, winner);
            }
        }
    }

    private static async Task SyncCompositeFahrerImTrainingAsync(
        LocalOpenSlalomDbContext localDb,
        RemoteOpenSlalomDbContext remoteDb,
        CancellationToken cancellationToken)
    {
        var localItems = await localDb.FahrerImTrainings.IgnoreQueryFilters().ToListAsync(cancellationToken);
        var remoteItems = await remoteDb.FahrerImTrainings.IgnoreQueryFilters().ToListAsync(cancellationToken);

        var localMap = localItems.ToDictionary(x => (x.TrainingId, x.FahrerId));
        var remoteMap = remoteItems.ToDictionary(x => (x.TrainingId, x.FahrerId));

        var allKeys = new HashSet<(int, int)>(localMap.Keys);
        allKeys.UnionWith(remoteMap.Keys);

        foreach (var key in allKeys)
        {
            localMap.TryGetValue(key, out var localItem);
            remoteMap.TryGetValue(key, out var remoteItem);

            var winner = ChooseWinner(localItem, remoteItem);
            if (winner is null)
            {
                continue;
            }

            if (localItem is null)
            {
                localDb.FahrerImTrainings.Add(new FahrerImTraining
                {
                    TrainingId = winner.TrainingId,
                    FahrerId = winner.FahrerId,
                    Reihenfolge = winner.Reihenfolge,
                    UpdatedAtUtc = winner.UpdatedAtUtc,
                    IsDeleted = winner.IsDeleted,
                    DeletedAtUtc = winner.DeletedAtUtc
                });
            }
            else
            {
                localItem.Reihenfolge = winner.Reihenfolge;
                CopySyncFields(localItem, winner);
            }

            if (remoteItem is null)
            {
                remoteDb.FahrerImTrainings.Add(new FahrerImTraining
                {
                    TrainingId = winner.TrainingId,
                    FahrerId = winner.FahrerId,
                    Reihenfolge = winner.Reihenfolge,
                    UpdatedAtUtc = winner.UpdatedAtUtc,
                    IsDeleted = winner.IsDeleted,
                    DeletedAtUtc = winner.DeletedAtUtc
                });
            }
            else
            {
                remoteItem.Reihenfolge = winner.Reihenfolge;
                CopySyncFields(remoteItem, winner);
            }
        }
    }

    private static async Task SyncCompositeFahrerInDerMeisterschaftAsync(
        LocalOpenSlalomDbContext localDb,
        RemoteOpenSlalomDbContext remoteDb,
        CancellationToken cancellationToken)
    {
        var localItems = await localDb.FahrerInDerMeisterschaften.IgnoreQueryFilters().ToListAsync(cancellationToken);
        var remoteItems = await remoteDb.FahrerInDerMeisterschaften.IgnoreQueryFilters().ToListAsync(cancellationToken);

        var localMap = localItems.ToDictionary(x => (x.MeisterschaftId, x.FahrerId));
        var remoteMap = remoteItems.ToDictionary(x => (x.MeisterschaftId, x.FahrerId));

        var allKeys = new HashSet<(int, int)>(localMap.Keys);
        allKeys.UnionWith(remoteMap.Keys);

        foreach (var key in allKeys)
        {
            localMap.TryGetValue(key, out var localItem);
            remoteMap.TryGetValue(key, out var remoteItem);

            var winner = ChooseWinner(localItem, remoteItem);
            if (winner is null)
            {
                continue;
            }

            if (localItem is null)
            {
                localDb.FahrerInDerMeisterschaften.Add(new FahrerInDerMeisterschaft
                {
                    MeisterschaftId = winner.MeisterschaftId,
                    FahrerId = winner.FahrerId,
                    Reihenfolge = winner.Reihenfolge,
                    UpdatedAtUtc = winner.UpdatedAtUtc,
                    IsDeleted = winner.IsDeleted,
                    DeletedAtUtc = winner.DeletedAtUtc
                });
            }
            else
            {
                localItem.Reihenfolge = winner.Reihenfolge;
                CopySyncFields(localItem, winner);
            }

            if (remoteItem is null)
            {
                remoteDb.FahrerInDerMeisterschaften.Add(new FahrerInDerMeisterschaft
                {
                    MeisterschaftId = winner.MeisterschaftId,
                    FahrerId = winner.FahrerId,
                    Reihenfolge = winner.Reihenfolge,
                    UpdatedAtUtc = winner.UpdatedAtUtc,
                    IsDeleted = winner.IsDeleted,
                    DeletedAtUtc = winner.DeletedAtUtc
                });
            }
            else
            {
                remoteItem.Reihenfolge = winner.Reihenfolge;
                CopySyncFields(remoteItem, winner);
            }
        }
    }

    private static TEntity? ChooseWinner<TEntity>(TEntity? localItem, TEntity? remoteItem)
        where TEntity : class, ISyncEntity
    {
        if (localItem is null)
        {
            return remoteItem;
        }

        if (remoteItem is null)
        {
            return localItem;
        }

        return localItem.UpdatedAtUtc >= remoteItem.UpdatedAtUtc ? localItem : remoteItem;
    }
}
