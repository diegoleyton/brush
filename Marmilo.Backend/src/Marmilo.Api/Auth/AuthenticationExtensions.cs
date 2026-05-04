using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace Marmilo.Api.Auth;

public static class AuthenticationExtensions
{
    public static IServiceCollection AddMarmiloAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<SupabaseAuthOptions>(configuration.GetSection(SupabaseAuthOptions.SectionName));

        SupabaseAuthOptions authOptions = configuration
            .GetSection(SupabaseAuthOptions.SectionName)
            .Get<SupabaseAuthOptions>() ?? new SupabaseAuthOptions();

        string projectUrl = authOptions.ProjectUrl.TrimEnd('/');
        if (!string.IsNullOrWhiteSpace(projectUrl))
        {
            string authority = $"{projectUrl}/auth/v1";

            services
                .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.Authority = authority;
                    options.Audience = authOptions.JwtAudience;
                    options.MapInboundClaims = false;
                    options.RequireHttpsMetadata = true;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = authority,
                        ValidateAudience = true,
                        ValidAudience = authOptions.JwtAudience,
                        ValidateLifetime = true,
                        NameClaimType = "sub",
                        RoleClaimType = "role"
                    };
                });
        }
        else
        {
            services.AddAuthentication();
        }

        services.AddAuthorization();

        return services;
    }
}
