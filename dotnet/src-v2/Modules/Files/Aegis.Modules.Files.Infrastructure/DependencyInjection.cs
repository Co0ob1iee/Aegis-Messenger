using Aegis.Modules.Files.Domain.Repositories;
using Aegis.Modules.Files.Infrastructure.Persistence;
using Aegis.Modules.Files.Infrastructure.Persistence.Repositories;
using Aegis.Shared.Cryptography.Encryption;
using Aegis.Shared.Cryptography.Interfaces;
using Aegis.Shared.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Aegis.Modules.Files.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddFilesInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<FilesDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("FilesDatabase") ??
                configuration.GetConnectionString("DefaultConnection"),
                b => b.MigrationsHistoryTable("__EFMigrationsHistory", "files")));

        services.AddScoped<IUnitOfWork>(sp =>
        {
            var context = sp.GetRequiredService<FilesDbContext>();
            return new UnitOfWork(context);
        });

        services.AddScoped<IFileMetadataRepository, FileMetadataRepository>();
        services.AddScoped<IAesEncryption, AesGcmEncryptionService>();

        return services;
    }
}
