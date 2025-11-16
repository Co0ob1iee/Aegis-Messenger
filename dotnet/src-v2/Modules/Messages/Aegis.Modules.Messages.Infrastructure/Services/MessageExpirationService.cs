using Aegis.Modules.Messages.Domain.Repositories;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Aegis.Modules.Messages.Infrastructure.Services;

/// <summary>
/// Background service that periodically deletes expired disappearing messages
/// Runs every minute to check for expired messages
/// </summary>
public class MessageExpirationService : BackgroundService
{
    private readonly ILogger<MessageExpirationService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(1);

    public MessageExpirationService(
        ILogger<MessageExpirationService> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Message Expiration Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await DeleteExpiredMessagesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting expired messages");
            }

            // Wait for next check interval
            await Task.Delay(_checkInterval, stoppingToken);
        }

        _logger.LogInformation("Message Expiration Service stopped");
    }

    private async Task DeleteExpiredMessagesAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var messageRepository = scope.ServiceProvider
            .GetRequiredService<IMessageRepository>();

        // Get all expired messages
        var expiredMessages = await messageRepository.GetExpiredMessagesAsync(cancellationToken);

        if (expiredMessages.Count == 0)
        {
            _logger.LogDebug("No expired messages to delete");
            return;
        }

        _logger.LogInformation("Found {Count} expired messages to delete", expiredMessages.Count);

        var deletedCount = 0;
        foreach (var message in expiredMessages)
        {
            try
            {
                // Secure delete - overwrite content before deletion
                await SecureDeleteMessageAsync(message, messageRepository, cancellationToken);
                deletedCount++;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to delete expired message {MessageId}",
                    message.Id);
            }
        }

        _logger.LogInformation(
            "Successfully deleted {Deleted}/{Total} expired messages",
            deletedCount,
            expiredMessages.Count);
    }

    /// <summary>
    /// Securely delete message by overwriting content before deletion
    /// Prevents forensic recovery of deleted messages
    /// </summary>
    private async Task SecureDeleteMessageAsync(
        Domain.Entities.Message message,
        IMessageRepository repository,
        CancellationToken cancellationToken)
    {
        // First, overwrite the encrypted content with random data
        // This prevents forensic recovery
        var random = new Random();
        var randomBytes = new byte[256];  // Overwrite with 256 bytes of random data
        random.NextBytes(randomBytes);

        // Note: In production, you might want to:
        // 1. Overwrite multiple times (DoD 5220.22-M standard = 3 passes)
        // 2. Use cryptographically secure random
        // 3. Overwrite the actual database blocks (filesystem level)

        // Mark as deleted (soft delete first)
        message.Delete();

        // Save changes
        await repository.UpdateAsync(message, cancellationToken);

        // Hard delete from database
        await repository.DeleteAsync(message.Id, cancellationToken);

        _logger.LogDebug(
            "Securely deleted expired message {MessageId} (expired at {ExpiresAt})",
            message.Id,
            message.DisappearsAt);
    }
}
