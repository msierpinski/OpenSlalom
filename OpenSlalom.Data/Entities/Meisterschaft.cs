namespace OpenSlalom.Data.Entities;

public sealed class Meisterschaft : ISyncEntity
{
    public int Id { get; set; }

    public int GastgeberId { get; set; }

    public int DisziplinId { get; set; }

    public int WetterId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Beschreibung { get; set; } = string.Empty;

    public DateOnly Zeitpunkt { get; set; }

    public bool MeisterschaftAbgeschlossen { get; set; }

    public bool AktivAusgerichtet { get; set; }

    public Verein Gastgeber { get; set; } = null!;

    public Disziplin Disziplin { get; set; } = null!;

    public Wetter Wetter { get; set; } = null!;

    public ICollection<FahrerInDerMeisterschaft> FahrerInDerMeisterschaften { get; set; } = new List<FahrerInDerMeisterschaft>();

    public ICollection<Mstint> Mstints { get; set; } = new List<Mstint>();

    public DateTime UpdatedAtUtc { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAtUtc { get; set; }
}
