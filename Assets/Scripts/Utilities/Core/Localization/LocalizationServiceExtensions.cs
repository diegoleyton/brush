namespace Flowbit.Utilities.Localization
{
    /// <summary>
    /// Helper methods for localization service consumers.
    /// </summary>
    public static class LocalizationServiceExtensions
    {
        public static string GetText(this ILocalizationService service, LocalizedTextReference reference)
        {
            if (reference == null)
            {
                return string.Empty;
            }

            return service != null
                ? service.GetText(reference.Key, reference.Fallback)
                : LocalizationServiceLocator.GetText(reference.Key, reference.Fallback);
        }
    }
}
