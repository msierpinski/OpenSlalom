namespace OpenSlalom.Data.Entities;

public sealed class Verein : ISyncEntity
{
    public int Id { get; set; }

    public string Vereinsname { get; set; } = string.Empty;

    public string MitgliedsNummer { get; set; } = string.Empty;

    public string Postleitzahl { get; set; } = string.Empty;

    public string Ort { get; set; } = string.Empty;

    public string Adresse { get; set; } = string.Empty;

    public byte[]? Logo { get; set; }

    public ICollection<Fahrer> Fahrer { get; set; } = new List<Fahrer>();

    public ICollection<Kart> Karts { get; set; } = new List<Kart>();

    public ICollection<Training> Trainings { get; set; } = new List<Training>();

    public ICollection<Meisterschaft> Meisterschaften { get; set; } = new List<Meisterschaft>();

    public DateTime UpdatedAtUtc { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAtUtc { get; set; }
}
