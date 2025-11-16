using Aegis.Modules.Messages.Application.Abstractions;
using Aegis.Modules.Messages.Domain.Repositories;
using Aegis.Modules.Messages.Infrastructure.Persistence;
using Aegis.Modules.Messages.Infrastructure.Persistence.Repositories;
using Aegis.Modules.Messages.Infrastructure.Services;
using Aegis.Shared.Cryptography.Interfaces;
using Aegis.Shared.Cryptography.SignalProtocol;
using Aegis.Shared.Cryptography.Storage;
using Aegis.Shared.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Aegis.Modules.Messages.Infrastructure;

/// <summary>
/// Dependency injection configuration for Messages Infrastructure layer
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddMessagesInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Database
        services.AddDbContext<MessagesDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("MessagesDatabase") ??
                configuration.GetConnectionString("DefaultConnection"),
                b => b.MigrationsHistoryTable("__EFMigrationsHistory", "messages")));

        // Unit of Work
        services.AddScoped<IUnitOfWork>(sp =>
        {
            var context = sp.GetRequiredService<MessagesDbContext>();
            return new UnitOfWork(context);
        });

        // Repositories
        services.AddScoped<IMessageRepository, MessageRepository>();
        services.AddScoped<IConversationRepository, ConversationRepository>();

        // Signal Protocol services
        services.AddScoped<ISignalProtocol, SignalProtocolService>();
        services.AddSingleton<IKeyStore, InMemoryKeyStore>();

        // Encryption service
        services.AddScoped<IEncryptionService, SignalProtocolEncryptionService>();

        return services;
    }
}
