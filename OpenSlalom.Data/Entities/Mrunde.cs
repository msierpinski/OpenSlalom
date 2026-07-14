namespace OpenSlalom.Data.Entities;

public sealed class Mrunde : ISyncEntity
{
    public int Id { get; set; }

    public int? MstintId { get; set; }

    public int? Runde { get; set; }

    public double? Rundenzeit { get; set; }

    public int? Pf { get; set; }

    public int? Tf { get; set; }

    public bool Ungueltig { get; set; }

    public Mstint? Mstint { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAtUtc { get; set; }
}
