using Marmilo.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Marmilo.Api.Tests.Infrastructure;

public sealed class MarmiloApiFactory : WebApplicationFactory<Program>
{
    private const string AdminConnectionString = "Host=localhost;Port=5432;Database=postgres;Username=Diego";
    private readonly string testDatabaseName_ = $"marmilo_api_tests_{Guid.NewGuid():N}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((_, configurationBuilder) =>
        {
            Dictionary<string, string?> overrides = new()
            {
                ["ConnectionStrings:MarmiloDatabase"] = BuildTestConnectionString(),
                ["Supabase:ProjectUrl"] = string.Empty,
                ["Supabase:AllowDevelopmentHeaderFallback"] = "true"
            };

            configurationBuilder.AddInMemoryCollection(overrides);
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<MarmiloDbContext>>();
            services.RemoveAll<MarmiloDbContext>();

            services.AddDbContext<MarmiloDbContext>(options =>
                options.UseNpgsql(
                    BuildTestConnectionString(),
                    npgsqlOptions => npgsqlOptions.MigrationsAssembly(typeof(MarmiloDbContext).Assembly.FullName)));
        });
    }

    public async Task ResetDatabaseAsync()
    {
        await CreateDatabaseIfNeededAsync();

        using IServiceScope scope = Services.CreateScope();
        MarmiloDbContext dbContext = scope.ServiceProvider.GetRequiredService<MarmiloDbContext>();

        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.EnsureCreatedAsync();
    }

    private string BuildTestConnectionString()
    {
        return $"Host=localhost;Port=5432;Database={testDatabaseName_};Username=Diego";
    }

    private async Task CreateDatabaseIfNeededAsync()
    {
        await using Npgsql.NpgsqlConnection connection = new(AdminConnectionString);
        await connection.OpenAsync();

        await using Npgsql.NpgsqlCommand command = connection.CreateCommand();
        command.CommandText = $"CREATE DATABASE \"{testDatabaseName_}\"";

        try
        {
            await command.ExecuteNonQueryAsync();
        }
        catch (Npgsql.PostgresException ex) when (ex.SqlState == "42P04")
        {
        }
    }
}
