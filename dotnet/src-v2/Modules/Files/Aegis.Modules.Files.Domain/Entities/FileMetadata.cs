using Aegis.Shared.Contracts.Events.Files;
using Aegis.Shared.Kernel.Primitives;
using Aegis.Shared.Kernel.Results;

namespace Aegis.Modules.Files.Domain.Entities;

public class FileMetadata : AggregateRoot<Guid>
{
    public string FileName { get; private set; }
    public long FileSize { get; private set; }
    public string ContentType { get; private set; }
    public string EncryptedPath { get; private set; }
    public byte[] EncryptionKey { get; private set; }
    public Guid UploadedBy { get; private set; }
    public DateTime UploadedAt { get; private set; }
    public bool IsDeleted { get; private set; }

    private FileMetadata() { }

    private FileMetadata(
        Guid id,
        string fileName,
        long fileSize,
        string contentType,
        string encryptedPath,
        byte[] encryptionKey,
        Guid uploadedBy)
    {
        Id = id;
        FileName = fileName;
        FileSize = fileSize;
        ContentType = contentType;
        EncryptedPath = encryptedPath;
        EncryptionKey = encryptionKey;
        UploadedBy = uploadedBy;
        UploadedAt = DateTime.UtcNow;
        IsDeleted = false;
    }

    public static Result<FileMetadata> Create(
        string fileName,
        long fileSize,
        string contentType,
        string encryptedPath,
        byte[] encryptionKey,
        Guid uploadedBy)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return Result.Failure<FileMetadata>(new Error("File.NameRequired", "File name is required"));

        if (fileSize <= 0)
            return Result.Failure<FileMetadata>(new Error("File.InvalidSize", "File size must be greater than 0"));

        if (fileSize > 100 * 1024 * 1024) // 100 MB
            return Result.Failure<FileMetadata>(new Error("File.TooLarge", "File size cannot exceed 100 MB"));

        var file = new FileMetadata(
            Guid.NewGuid(),
            fileName,
            fileSize,
            contentType,
            encryptedPath,
            encryptionKey,
            uploadedBy);

        file.RaiseDomainEvent(new FileUploadedEvent(
            file.Id,
            fileName,
            fileSize,
            uploadedBy,
            DateTime.UtcNow));

        return Result.Success(file);
    }

    public Result Delete()
    {
        if (IsDeleted)
            return Result.Failure(new Error("File.AlreadyDeleted", "File is already deleted"));

        IsDeleted = true;
        return Result.Success();
    }
}
