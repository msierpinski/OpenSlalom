namespace OpenSlalom.Data.Entities;

public sealed class Fahrer : ISyncEntity
{
    public int Id { get; set; }

    public int VereinId { get; set; }

    public string Vorname { get; set; } = string.Empty;

    public string? Nachname { get; set; }

    public string MitgliedsNummer { get; set; } = string.Empty;

    public DateOnly? Geburtsdatum { get; set; }

    public string Geschlecht { get; set; } = string.Empty;

    public Verein Verein { get; set; } = null!;

    public ICollection<FahrerImTraining> FahrerImTrainings { get; set; } = new List<FahrerImTraining>();

    public ICollection<FahrerInDerMeisterschaft> FahrerInDerMeisterschaften { get; set; } = new List<FahrerInDerMeisterschaft>();

    public ICollection<Tstint> Tstints { get; set; } = new List<Tstint>();

    public ICollection<Mstint> Mstints { get; set; } = new List<Mstint>();

    public DateTime UpdatedAtUtc { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAtUtc { get; set; }
}
