using Aegis.Shared.Kernel.Results;
using MediatR;

namespace Aegis.Modules.Messages.Application.Commands.MarkAsRead;

/// <summary>
/// Command to mark message as read
/// </summary>
public record MarkAsReadCommand(Guid MessageId) : IRequest<Result>;
