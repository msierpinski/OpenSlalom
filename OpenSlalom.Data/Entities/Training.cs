namespace OpenSlalom.Data.Entities;

public sealed class Training : ISyncEntity
{
    public int Id { get; set; }

    public int VereinId { get; set; }

    public int DisziplinId { get; set; }

    public int WetterId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Beschreibung { get; set; } = string.Empty;

    public DateOnly Zeitpunkt { get; set; }

    public bool TrainingAbgeschlossen { get; set; }

    public Verein Verein { get; set; } = null!;

    public Disziplin Disziplin { get; set; } = null!;

    public Wetter Wetter { get; set; } = null!;

    public ICollection<FahrerImTraining> FahrerImTrainings { get; set; } = new List<FahrerImTraining>();

    public ICollection<Tstint> Tstints { get; set; } = new List<Tstint>();

    public DateTime UpdatedAtUtc { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAtUtc { get; set; }
}
