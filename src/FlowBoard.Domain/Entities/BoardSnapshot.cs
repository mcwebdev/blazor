using FlowBoard.Domain.Common;

namespace FlowBoard.Domain.Entities;

public class BoardSnapshot : AuditableEntity
{
    public Guid BoardId { get; set; }
    public long ActivityLogSequenceNumber { get; set; }
    public string SnapshotJson { get; set; } = string.Empty;

    // Navigation properties
    public Board Board { get; set; } = null!;
}
