namespace OpenSlalom.Data.Entities;

public sealed class Mstint : ISyncEntity
{
    public int Id { get; set; }

    public int MeisterschaftId { get; set; }

    public int FahrerId { get; set; }

    public int? KartId { get; set; }

    public string AltersklasseSnapshot { get; set; } = string.Empty;

    public DateTime Datum { get; set; }

    public Meisterschaft Meisterschaft { get; set; } = null!;

    public Fahrer Fahrer { get; set; } = null!;

    public Kart? Kart { get; set; }

    public ICollection<Mrunde> Mrunden { get; set; } = new List<Mrunde>();

    public DateTime UpdatedAtUtc { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAtUtc { get; set; }
}
