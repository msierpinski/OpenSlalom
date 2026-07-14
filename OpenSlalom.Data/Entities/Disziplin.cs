namespace OpenSlalom.Data.Entities;

public sealed class Disziplin : ISyncEntity
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public double ZeitstrafeTorfehler { get; set; }

    public double ZeitstrafePylonenfehler { get; set; }

    public ICollection<Kart> Karts { get; set; } = new List<Kart>();

    public ICollection<Training> Trainings { get; set; } = new List<Training>();

    public ICollection<Meisterschaft> Meisterschaften { get; set; } = new List<Meisterschaft>();

    public ICollection<DisziplinAltersklasse> Altersklassen { get; set; } = new List<DisziplinAltersklasse>();

    public DateTime UpdatedAtUtc { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAtUtc { get; set; }
}
