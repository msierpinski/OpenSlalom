namespace OpenSlalom.Data.Entities;

public interface ISyncEntity
{
    DateTime UpdatedAtUtc { get; set; }

    bool IsDeleted { get; set; }

    DateTime? DeletedAtUtc { get; set; }
}
