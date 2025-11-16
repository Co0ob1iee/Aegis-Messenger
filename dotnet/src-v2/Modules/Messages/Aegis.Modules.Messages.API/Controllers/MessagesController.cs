using Aegis.Modules.Messages.Application.Commands.MarkAsDelivered;
using Aegis.Modules.Messages.Application.Commands.MarkAsRead;
using Aegis.Modules.Messages.Application.Commands.SendMessage;
using Aegis.Modules.Messages.Application.Queries.GetConversationMessages;
using Aegis.Modules.Messages.Domain.Enums;
using Aegis.Shared.Contracts.DTOs.Messages;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Aegis.Modules.Messages.API.Controllers;

/// <summary>
/// Messages endpoints for sending and retrieving encrypted messages
/// </summary>
[ApiController]
[Route("api/messages")]
[Authorize]
[Produces("application/json")]
public class MessagesController : ControllerBase
{
    private readonly IMediator _mediator;

    public MessagesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Send encrypted message to user
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(MessageDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SendMessage([FromBody] SendMessageRequest request)
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var senderId))
        {
            return Unauthorized();
        }

        var command = new SendMessageCommand(
            senderId,
            request.RecipientId,
            System.Text.Encoding.UTF8.GetString(request.EncryptedContent),
            (MessageType)request.IsGroup ? MessageType.Text : MessageType.Text,
            request.IsGroup,
            request.GroupId,
            request.ReplyToMessageId);

        var result = await _mediator.Send(command);

        if (result.IsFailure)
        {
            return BadRequest(new { error = result.Error.Message });
        }

        return CreatedAtAction(
            nameof(GetConversationMessages),
            new { conversationId = Guid.Empty },
            result.Value);
    }

    /// <summary>
    /// Get messages for a conversation
    /// </summary>
    [HttpGet("conversation/{conversationId}")]
    [ProducesResponseType(typeof(IReadOnlyList<MessageDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetConversationMessages(
        Guid conversationId,
        [FromQuery] int limit = 50,
        [FromQuery] DateTime? before = null)
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            return Unauthorized();
        }

        var query = new GetConversationMessagesQuery(conversationId, userId, limit, before);
        var result = await _mediator.Send(query);

        if (result.IsFailure)
        {
            return NotFound(new { error = result.Error.Message });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Mark message as delivered
    /// </summary>
    [HttpPost("{messageId}/delivered")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkAsDelivered(Guid messageId)
    {
        var command = new MarkAsDeliveredCommand(messageId);
        var result = await _mediator.Send(command);

        if (result.IsFailure)
        {
            return NotFound(new { error = result.Error.Message });
        }

        return Ok();
    }

    /// <summary>
    /// Mark message as read
    /// </summary>
    [HttpPost("{messageId}/read")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkAsRead(Guid messageId)
    {
        var command = new MarkAsReadCommand(messageId);
        var result = await _mediator.Send(command);

        if (result.IsFailure)
        {
            return NotFound(new { error = result.Error.Message });
        }

        return Ok();
    }
}
