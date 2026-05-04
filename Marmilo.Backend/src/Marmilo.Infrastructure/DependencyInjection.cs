using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Marmilo.Infrastructure;

using Marmilo.Infrastructure.Persistence;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        string connectionString =
            configuration.GetConnectionString("MarmiloDatabase")
            ?? configuration.GetConnectionString("BrushDatabase")
            ?? throw new InvalidOperationException(
                "Connection string 'MarmiloDatabase' was not found. Legacy key 'BrushDatabase' is also supported.");

        services.AddDbContext<MarmiloDbContext>(options =>
            options.UseNpgsql(
                connectionString,
                npgsqlOptions => npgsqlOptions.MigrationsAssembly(typeof(MarmiloDbContext).Assembly.FullName)));

        return services;
    }
}
