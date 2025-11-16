using Aegis.Modules.Users.Domain.Enums;
using Aegis.Shared.Kernel.Results;
using MediatR;

namespace Aegis.Modules.Users.Application.Commands.SetOnlineStatus;

public record SetOnlineStatusCommand(Guid UserId, OnlineStatus Status) : IRequest<Result>;
