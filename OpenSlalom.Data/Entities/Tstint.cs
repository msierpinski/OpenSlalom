namespace OpenSlalom.Data.Entities;

public sealed class Tstint : ISyncEntity
{
    public int Id { get; set; }

    public int TrainingId { get; set; }

    public int FahrerId { get; set; }

    public int? KartId { get; set; }

    public string AltersklasseSnapshot { get; set; } = string.Empty;

    public DateTime Datum { get; set; }

    public Training Training { get; set; } = null!;

    public Fahrer Fahrer { get; set; } = null!;

    public Kart? Kart { get; set; }

    public ICollection<Trunde> Trunden { get; set; } = new List<Trunde>();

    public DateTime UpdatedAtUtc { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAtUtc { get; set; }
}
