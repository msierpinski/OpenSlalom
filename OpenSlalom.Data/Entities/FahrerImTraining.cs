namespace OpenSlalom.Data.Entities;

public sealed class FahrerImTraining : ISyncEntity
{
    public int TrainingId { get; set; }

    public int FahrerId { get; set; }

    public int Reihenfolge { get; set; }

    public Training Training { get; set; } = null!;

    public Fahrer Fahrer { get; set; } = null!;

    public DateTime UpdatedAtUtc { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAtUtc { get; set; }
}
