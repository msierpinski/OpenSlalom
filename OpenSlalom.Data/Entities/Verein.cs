namespace OpenSlalom.Data.Entities;

public sealed class Verein : ISyncEntity
{
    public int Id { get; set; }

    public string Vereinsname { get; set; } = string.Empty;

    public string MitgliedsNummer { get; set; } = string.Empty;

    public ICollection<Fahrer> Fahrer { get; set; } = new List<Fahrer>();

    public ICollection<Kart> Karts { get; set; } = new List<Kart>();

    public ICollection<Training> Trainings { get; set; } = new List<Training>();

    public DateTime UpdatedAtUtc { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAtUtc { get; set; }
}
