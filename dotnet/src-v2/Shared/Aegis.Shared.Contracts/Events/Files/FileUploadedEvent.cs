using Aegis.Shared.Kernel.Interfaces;

namespace Aegis.Shared.Contracts.Events.Files;

/// <summary>
/// Domain event raised when a file is uploaded
/// </summary>
/// <param name="FileId">Unique identifier of the file</param>
/// <param name="FileName">Original file name</param>
/// <param name="FileSize">File size in bytes</param>
/// <param name="UploadedBy">User ID who uploaded the file</param>
/// <param name="OccurredAt">Timestamp when the event occurred</param>
public record FileUploadedEvent(
    Guid FileId,
    string FileName,
    long FileSize,
    Guid UploadedBy,
    DateTime OccurredAt
) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
}
