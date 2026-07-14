using Microsoft.EntityFrameworkCore;
using OpenSlalom.Data.Entities;

namespace OpenSlalom.Data;

public class OpenSlalomDbContext : DbContext
{
    public bool SuppressSyncTracking { get; set; }

    public OpenSlalomDbContext(DbContextOptions options)
        : base(options)
    {
    }

    public DbSet<Disziplin> Disziplinen => Set<Disziplin>();

    public DbSet<DisziplinAltersklasse> DisziplinAltersklassen => Set<DisziplinAltersklasse>();

    public DbSet<Fahrer> Fahrer => Set<Fahrer>();

    public DbSet<FahrerImTraining> FahrerImTrainings => Set<FahrerImTraining>();

    public DbSet<Kart> Karts => Set<Kart>();

    public DbSet<Training> Trainings => Set<Training>();

    public DbSet<Trunde> Trunden => Set<Trunde>();

    public DbSet<Tstint> Tstints => Set<Tstint>();

    public DbSet<Verein> Vereine => Set<Verein>();

    public DbSet<Wetter> Wetterlagen => Set<Wetter>();

    public DbSet<SyncState> SyncStates => Set<SyncState>();

    public override int SaveChanges()
    {
        ApplySyncTracking();
        return base.SaveChanges();
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ApplySyncTracking();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplySyncTracking();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        ApplySyncTracking();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Disziplin>(entity =>
        {
            entity.ToTable("disziplin");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Id)
                .HasColumnName("id")
                .ValueGeneratedOnAdd();

            entity.Property(x => x.Name)
                .HasColumnName("disziplin")
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(x => x.ZeitstrafeTorfehler)
                .HasColumnName("tf")
                .HasDefaultValue(0d)
                .IsRequired();

            entity.Property(x => x.ZeitstrafePylonenfehler)
                .HasColumnName("pf")
                .HasDefaultValue(0d)
                .IsRequired();

            ConfigureSyncEntity(entity);
        });

        modelBuilder.Entity<DisziplinAltersklasse>(entity =>
        {
            entity.ToTable("disziplin_altersklassen");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Id)
                .HasColumnName("id")
                .ValueGeneratedOnAdd();

            entity.Property(x => x.DisziplinId)
                .HasColumnName("fk_id_disziplin")
                .IsRequired();

            entity.Property(x => x.Bezeichnung)
                .HasColumnName("bezeichnung")
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(x => x.AlterVon)
                .HasColumnName("alter_von")
                .IsRequired();

            entity.Property(x => x.AlterBis)
                .HasColumnName("alter_bis");

            ConfigureSyncEntity(entity);

            entity.HasOne(x => x.Disziplin)
                .WithMany(x => x.Altersklassen)
                .HasForeignKey(x => x.DisziplinId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_disziplin_altersklassen_disziplin");
        });

        modelBuilder.Entity<Verein>(entity =>
        {
            entity.ToTable("vereine");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Id)
                .HasColumnName("id")
                .ValueGeneratedOnAdd();

            entity.Property(x => x.Vereinsname)
                .HasColumnName("vereinsname")
                .HasMaxLength(100)
                .HasDefaultValue(string.Empty)
                .IsRequired();

            entity.Property(x => x.MitgliedsNummer)
                .HasColumnName("mitglieds_nummer")
                .HasMaxLength(50)
                .HasDefaultValue(string.Empty)
                .IsRequired();

            ConfigureSyncEntity(entity);
        });

        modelBuilder.Entity<Wetter>(entity =>
        {
            entity.ToTable("wetter");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Id)
                .HasColumnName("id")
                .ValueGeneratedOnAdd();

            entity.Property(x => x.Bezeichnung)
                .HasColumnName("wetter")
                .HasMaxLength(50)
                .IsRequired();

            ConfigureSyncEntity(entity);
        });

        modelBuilder.Entity<Fahrer>(entity =>
        {
            entity.ToTable("fahrer");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Id)
                .HasColumnName("id")
                .ValueGeneratedOnAdd();

            entity.Property(x => x.VereinId)
                .HasColumnName("fk_id_verein")
                .IsRequired();

            entity.Property(x => x.Vorname)
                .HasColumnName("vorname")
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(x => x.Nachname)
                .HasColumnName("nachname")
                .HasMaxLength(100);

            entity.Property(x => x.Geburtsdatum)
                .HasColumnName("geburtsdatum")
                .HasColumnType("date");

            entity.Property(x => x.Geschlecht)
                .HasColumnName("geschlecht")
                .HasMaxLength(20)
                .HasDefaultValue(string.Empty)
                .IsRequired();

            ConfigureSyncEntity(entity);

            entity.HasOne(x => x.Verein)
                .WithMany(x => x.Fahrer)
                .HasForeignKey(x => x.VereinId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_fahrer_vereine");
        });

        modelBuilder.Entity<FahrerImTraining>(entity =>
        {
            entity.ToTable("fahrer_im_training");
            entity.HasKey(x => new { x.TrainingId, x.FahrerId });

            entity.Property(x => x.TrainingId)
                .HasColumnName("fk_id_training")
                .IsRequired();

            entity.Property(x => x.FahrerId)
                .HasColumnName("fk_id_fahrer")
                .IsRequired();

            entity.Property(x => x.Reihenfolge)
                .HasColumnName("reihenfolge")
                .HasDefaultValue(0)
                .IsRequired();

            entity.HasOne(x => x.Training)
                .WithMany(x => x.FahrerImTrainings)
                .HasForeignKey(x => x.TrainingId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_fahrer_im_training_training");

            entity.HasOne(x => x.Fahrer)
                .WithMany(x => x.FahrerImTrainings)
                .HasForeignKey(x => x.FahrerId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_fahrer_im_training_fahrer");

            ConfigureSyncEntity(entity);
        });

        modelBuilder.Entity<Kart>(entity =>
        {
            entity.ToTable("karts");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Id)
                .HasColumnName("id")
                .ValueGeneratedOnAdd();

            entity.Property(x => x.VereinId)
                .HasColumnName("fk_id_verein")
                .IsRequired();

            entity.Property(x => x.DisziplinId)
                .HasColumnName("fk_id_disziplin")
                .IsRequired();

            entity.Property(x => x.Name)
                .HasColumnName("Name")
                .HasMaxLength(100);

            entity.Property(x => x.Motor)
                .HasColumnName("Motor")
                .HasMaxLength(100);

            entity.Property(x => x.Chassis)
                .HasColumnName("Chassis")
                .HasMaxLength(100);

            ConfigureSyncEntity(entity);

            entity.HasOne(x => x.Verein)
                .WithMany(x => x.Karts)
                .HasForeignKey(x => x.VereinId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_karts_vereine");

            entity.HasOne(x => x.Disziplin)
                .WithMany(x => x.Karts)
                .HasForeignKey(x => x.DisziplinId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_karts_disziplin");
        });

        modelBuilder.Entity<Training>(entity =>
        {
            entity.ToTable("training");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Id)
                .HasColumnName("id")
                .ValueGeneratedOnAdd();

            entity.Property(x => x.VereinId)
                .HasColumnName("fk_id_verein")
                .IsRequired();

            entity.Property(x => x.DisziplinId)
                .HasColumnName("fk_id_disziplin")
                .IsRequired();

            entity.Property(x => x.WetterId)
                .HasColumnName("fk_id_wetter")
                .IsRequired();

            entity.Property(x => x.Name)
                .HasColumnName("name")
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(x => x.Beschreibung)
                .HasColumnName("beschreibung")
                .HasMaxLength(250)
                .IsRequired();

            entity.Property(x => x.Zeitpunkt)
                .HasColumnName("zeitpunkt")
                .HasColumnType("date")
                .IsRequired();

            entity.Property(x => x.TrainingAbgeschlossen)
                .HasColumnName("training_abgeschlossen")
                .HasDefaultValue(false)
                .IsRequired();

            ConfigureSyncEntity(entity);

            entity.HasOne(x => x.Verein)
                .WithMany(x => x.Trainings)
                .HasForeignKey(x => x.VereinId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_training_vereine");

            entity.HasOne(x => x.Disziplin)
                .WithMany(x => x.Trainings)
                .HasForeignKey(x => x.DisziplinId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_training_disziplin");

            entity.HasOne(x => x.Wetter)
                .WithMany(x => x.Trainings)
                .HasForeignKey(x => x.WetterId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_training_wetter");
        });

        modelBuilder.Entity<Tstint>(entity =>
        {
            entity.ToTable("tstints");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Id)
                .HasColumnName("id")
                .ValueGeneratedOnAdd();

            entity.Property(x => x.TrainingId)
                .HasColumnName("fk_id_training")
                .IsRequired();

            entity.Property(x => x.FahrerId)
                .HasColumnName("fk_id_fahrer")
                .IsRequired();

            entity.Property(x => x.KartId)
                .HasColumnName("fk_id_kart");

            entity.Property(x => x.AltersklasseSnapshot)
                .HasColumnName("altersklasse_snapshot")
                .HasMaxLength(100)
                .HasDefaultValue(string.Empty)
                .IsRequired();

            entity.Property(x => x.Datum)
                .HasColumnName("datum")
                .HasColumnType("datetime")
                .IsRequired();

            ConfigureSyncEntity(entity);

            entity.HasOne(x => x.Training)
                .WithMany(x => x.Tstints)
                .HasForeignKey(x => x.TrainingId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_tstints_training");

            entity.HasOne(x => x.Fahrer)
                .WithMany(x => x.Tstints)
                .HasForeignKey(x => x.FahrerId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_tstints_fahrer");

            entity.HasOne(x => x.Kart)
                .WithMany()
                .HasForeignKey(x => x.KartId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_tstints_karts");
        });

        modelBuilder.Entity<Trunde>(entity =>
        {
            entity.ToTable("trunden");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Id)
                .HasColumnName("id")
                .ValueGeneratedOnAdd();

            entity.Property(x => x.TstintId)
                .HasColumnName("fk_id_tstint");

            entity.Property(x => x.Runde)
                .HasColumnName("runde");

            entity.Property(x => x.Rundenzeit)
                .HasColumnName("rundenzeit");

            entity.Property(x => x.Pf)
                .HasColumnName("pf");

            entity.Property(x => x.Tf)
                .HasColumnName("tf");

            entity.Property(x => x.Ungueltig)
                .HasColumnName("ungueltig")
                .HasDefaultValue(false)
                .IsRequired();

            entity.HasOne(x => x.Tstint)
                .WithMany(x => x.Trunden)
                .HasForeignKey(x => x.TstintId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_trunden_tstints");

            ConfigureSyncEntity(entity);
        });

        modelBuilder.Entity<SyncState>(entity =>
        {
            entity.ToTable("sync_state");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Id)
                .HasColumnName("id")
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(x => x.LastSyncUtc)
                .HasColumnName("last_sync_utc")
                .HasColumnType("datetime")
                .IsRequired();
        });
    }

    private void ApplySyncTracking()
    {
        if (SuppressSyncTracking)
        {
            return;
        }

        var now = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries<ISyncEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.UpdatedAtUtc = now;
                    entry.Entity.IsDeleted = false;
                    entry.Entity.DeletedAtUtc = null;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAtUtc = now;
                    if (!entry.Entity.IsDeleted)
                    {
                        entry.Entity.DeletedAtUtc = null;
                    }

                    break;
                case EntityState.Deleted:
                    entry.State = EntityState.Modified;
                    entry.Entity.IsDeleted = true;
                    entry.Entity.DeletedAtUtc = now;
                    entry.Entity.UpdatedAtUtc = now;
                    break;
            }
        }
    }

    private static void ConfigureSyncEntity<TEntity>(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<TEntity> entity)
        where TEntity : class, ISyncEntity
    {
        entity.Property(x => x.UpdatedAtUtc)
            .HasColumnName("updated_at_utc")
            .HasColumnType("datetime")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        entity.Property(x => x.IsDeleted)
            .HasColumnName("is_deleted")
            .HasDefaultValue(false)
            .IsRequired();

        entity.Property(x => x.DeletedAtUtc)
            .HasColumnName("deleted_at_utc")
            .HasColumnType("datetime");

        entity.HasQueryFilter(x => !x.IsDeleted);
    }
}
