namespace OpenSlalom.Data.Entities;

public sealed class Wetter : ISyncEntity
{
    public int Id { get; set; }

    public string Bezeichnung { get; set; } = string.Empty;

    public ICollection<Training> Trainings { get; set; } = new List<Training>();

    public ICollection<Meisterschaft> Meisterschaften { get; set; } = new List<Meisterschaft>();

    public DateTime UpdatedAtUtc { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAtUtc { get; set; }
}
