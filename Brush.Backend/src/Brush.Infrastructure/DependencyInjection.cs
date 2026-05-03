using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Brush.Infrastructure;

using Brush.Infrastructure.Persistence;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        string connectionString =
            configuration.GetConnectionString("BrushDatabase")
            ?? throw new InvalidOperationException("Connection string 'BrushDatabase' was not found.");

        services.AddDbContext<BrushDbContext>(options =>
            options.UseNpgsql(
                connectionString,
                npgsqlOptions => npgsqlOptions.MigrationsAssembly(typeof(BrushDbContext).Assembly.FullName)));

        return services;
    }
}
