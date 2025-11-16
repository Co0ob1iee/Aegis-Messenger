namespace Aegis.Shared.Contracts.DTOs.Groups;

/// <summary>
/// Data transfer object for group information
/// </summary>
/// <param name="Id">Unique group identifier</param>
/// <param name="Name">Group name</param>
/// <param name="Description">Group description (optional)</param>
/// <param name="CreatorId">User ID of the group creator</param>
/// <param name="CreatedAt">Group creation timestamp</param>
/// <param name="MemberCount">Number of members in the group</param>
public record GroupDto(
    Guid Id,
    string Name,
    string? Description,
    Guid CreatorId,
    DateTime CreatedAt,
    int MemberCount
);
