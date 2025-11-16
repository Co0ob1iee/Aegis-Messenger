namespace Aegis.Shared.Contracts.DTOs.Files;

/// <summary>
/// Data transfer object for file metadata
/// </summary>
/// <param name="Id">Unique file identifier</param>
/// <param name="FileName">Original file name</param>
/// <param name="FileSize">File size in bytes</param>
/// <param name="ContentType">MIME type of the file</param>
/// <param name="EncryptedPath">Encrypted storage path</param>
/// <param name="UploadedBy">User ID who uploaded the file</param>
/// <param name="UploadedAt">Upload timestamp</param>
public record FileDto(
    Guid Id,
    string FileName,
    long FileSize,
    string ContentType,
    string EncryptedPath,
    Guid UploadedBy,
    DateTime UploadedAt
);
