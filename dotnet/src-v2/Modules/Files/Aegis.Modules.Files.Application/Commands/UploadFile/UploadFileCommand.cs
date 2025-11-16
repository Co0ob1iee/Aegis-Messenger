using Aegis.Shared.Contracts.DTOs.Files;
using Aegis.Shared.Kernel.Results;
using MediatR;

namespace Aegis.Modules.Files.Application.Commands.UploadFile;

public record UploadFileCommand(
    string FileName,
    long FileSize,
    string ContentType,
    Stream FileStream,
    Guid UploadedBy
) : IRequest<Result<FileDto>>;
