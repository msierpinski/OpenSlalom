namespace OpenSlalom.Data.Entities;

public sealed class DisziplinAltersklasse : ISyncEntity
{
    public int Id { get; set; }

    public int DisziplinId { get; set; }

    public string Bezeichnung { get; set; } = string.Empty;

    public int AlterVon { get; set; }

    public int? AlterBis { get; set; }

    public Disziplin Disziplin { get; set; } = null!;

    public DateTime UpdatedAtUtc { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAtUtc { get; set; }
}
