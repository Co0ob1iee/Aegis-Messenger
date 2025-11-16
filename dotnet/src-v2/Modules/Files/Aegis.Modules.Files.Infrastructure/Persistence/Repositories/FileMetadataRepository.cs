using Aegis.Modules.Files.Domain.Entities;
using Aegis.Modules.Files.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Aegis.Modules.Files.Infrastructure.Persistence.Repositories;

public class FileMetadataRepository : IFileMetadataRepository
{
    private readonly FilesDbContext _context;

    public FileMetadataRepository(FilesDbContext context)
    {
        _context = context;
    }

    public async Task<FileMetadata?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Files.FindAsync(new object[] { id }, cancellationToken);
    }

    public async Task<IReadOnlyList<FileMetadata>> GetUserFilesAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.Files
            .Where(f => f.UploadedBy == userId && !f.IsDeleted)
            .OrderByDescending(f => f.UploadedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<FileMetadata> AddAsync(FileMetadata file, CancellationToken cancellationToken = default)
    {
        await _context.Files.AddAsync(file, cancellationToken);
        return file;
    }

    public void Update(FileMetadata file)
    {
        _context.Files.Update(file);
    }

    public void Delete(FileMetadata file)
    {
        _context.Files.Remove(file);
    }
}
