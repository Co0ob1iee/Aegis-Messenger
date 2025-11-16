using Aegis.Modules.Files.Domain.Entities;

namespace Aegis.Modules.Files.Domain.Repositories;

public interface IFileMetadataRepository
{
    Task<FileMetadata?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FileMetadata>> GetUserFilesAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<FileMetadata> AddAsync(FileMetadata file, CancellationToken cancellationToken = default);
    void Update(FileMetadata file);
    void Delete(FileMetadata file);
}
