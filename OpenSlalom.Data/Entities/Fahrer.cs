namespace OpenSlalom.Data.Entities;

public sealed class Fahrer : ISyncEntity
{
    public int Id { get; set; }

    public int VereinId { get; set; }

    public string Vorname { get; set; } = string.Empty;

    public string? Nachname { get; set; }

    public DateOnly? Geburtsdatum { get; set; }

    public Verein Verein { get; set; } = null!;

    public ICollection<FahrerImTraining> FahrerImTrainings { get; set; } = new List<FahrerImTraining>();

    public ICollection<Tstint> Tstints { get; set; } = new List<Tstint>();

    public DateTime UpdatedAtUtc { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAtUtc { get; set; }
}
