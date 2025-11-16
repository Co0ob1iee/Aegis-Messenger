using Aegis.Modules.Files.Domain.Entities;
using Aegis.Modules.Files.Domain.Repositories;
using Aegis.Shared.Contracts.DTOs.Files;
using Aegis.Shared.Cryptography.Interfaces;
using Aegis.Shared.Infrastructure.EventBus;
using Aegis.Shared.Infrastructure.Persistence;
using Aegis.Shared.Kernel.Results;
using MediatR;

namespace Aegis.Modules.Files.Application.Commands.UploadFile;

public class UploadFileCommandHandler : IRequestHandler<UploadFileCommand, Result<FileDto>>
{
    private readonly IFileMetadataRepository _repository;
    private readonly IAesEncryption _encryption;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEventBus _eventBus;

    public UploadFileCommandHandler(
        IFileMetadataRepository repository,
        IAesEncryption encryption,
        IUnitOfWork unitOfWork,
        IEventBus eventBus)
    {
        _repository = repository;
        _encryption = encryption;
        _unitOfWork = unitOfWork;
        _eventBus = eventBus;
    }

    public async Task<Result<FileDto>> Handle(UploadFileCommand request, CancellationToken cancellationToken)
    {
        // Generate encryption key
        var encryptionKey = _encryption.GenerateKey();

        // Read file content
        using var memoryStream = new MemoryStream();
        await request.FileStream.CopyToAsync(memoryStream, cancellationToken);
        var fileContent = memoryStream.ToArray();

        // Encrypt file
        var encryptedContent = await _encryption.EncryptAsync(fileContent, encryptionKey);

        // Generate storage path (in production, save to blob storage)
        var encryptedPath = $"files/{Guid.NewGuid()}.encrypted";

        // TODO: Save encrypted content to storage
        // await _storage.SaveAsync(encryptedPath, encryptedContent);

        // Create metadata
        var fileResult = FileMetadata.Create(
            request.FileName,
            request.FileSize,
            request.ContentType,
            encryptedPath,
            encryptionKey,
            request.UploadedBy);

        if (fileResult.IsFailure)
            return Result.Failure<FileDto>(fileResult.Error);

        var file = fileResult.Value;

        await _repository.AddAsync(file, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);

        await _eventBus.PublishManyAsync(file.DomainEvents, cancellationToken);
        file.ClearDomainEvents();

        var dto = new FileDto(
            file.Id,
            file.FileName,
            file.FileSize,
            file.ContentType,
            file.EncryptedPath,
            file.UploadedBy,
            file.UploadedAt);

        return Result.Success(dto);
    }
}
