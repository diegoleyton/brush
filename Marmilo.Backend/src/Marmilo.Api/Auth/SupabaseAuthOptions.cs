namespace Marmilo.Api.Auth;

public sealed class SupabaseAuthOptions
{
    public const string SectionName = "Supabase";

    public string ProjectUrl { get; set; } = string.Empty;

    public string JwtAudience { get; set; } = "authenticated";

    public bool AllowDevelopmentHeaderFallback { get; set; } = true;
}
