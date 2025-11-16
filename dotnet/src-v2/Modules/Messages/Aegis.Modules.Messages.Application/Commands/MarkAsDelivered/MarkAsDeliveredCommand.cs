using Aegis.Shared.Kernel.Results;
using MediatR;

namespace Aegis.Modules.Messages.Application.Commands.MarkAsDelivered;

/// <summary>
/// Command to mark message as delivered
/// </summary>
public record MarkAsDeliveredCommand(Guid MessageId) : IRequest<Result>;
