namespace Flowbit.Utilities.Localization
{
    /// <summary>
    /// Resolves localized text from a key with optional fallback content.
    /// </summary>
    public interface ILocalizationService
    {
        string GetText(string key, string fallback = null);
    }
}
