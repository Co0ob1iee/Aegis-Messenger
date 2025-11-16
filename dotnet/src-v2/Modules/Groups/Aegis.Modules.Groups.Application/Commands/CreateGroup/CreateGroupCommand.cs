using Aegis.Shared.Contracts.DTOs.Groups;
using Aegis.Shared.Kernel.Results;
using MediatR;

namespace Aegis.Modules.Groups.Application.Commands.CreateGroup;

public record CreateGroupCommand(Guid CreatedBy, string Name, string? Description) : IRequest<Result<GroupDto>>;
