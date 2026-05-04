namespace Flowbit.Utilities.Localization
{
    /// <summary>
    /// Lightweight global access point for UI localization consumers.
    /// </summary>
    public static class LocalizationServiceLocator
    {
        public static ILocalizationService Current { get; set; }

        public static string GetText(string key, string fallback = null)
        {
            if (Current != null)
            {
                return Current.GetText(key, fallback);
            }

            if (!string.IsNullOrWhiteSpace(fallback))
            {
                return fallback;
            }

            return key ?? string.Empty;
        }

        public static string GetText(LocalizedTextReference reference)
        {
            if (reference == null)
            {
                return string.Empty;
            }

            return GetText(reference.Key, reference.Fallback);
        }
    }
}
