using System;
using System.Collections.Generic;

using Flowbit.Utilities.Localization;

namespace Flowbit.Utilities.Localization
{
    /// <summary>
    /// In-memory localization service with graceful fallback behavior.
    /// </summary>
    public sealed class LocalizationService : ILocalizationService
    {
        private readonly Dictionary<string, string> entries_ =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public LocalizationService(IEnumerable<LocalizationEntry> entries = null)
        {
            Replace(entries);
        }

        public string GetText(string key, string fallback = null)
        {
            if (!string.IsNullOrWhiteSpace(key) &&
                entries_.TryGetValue(key.Trim(), out string value) &&
                !string.IsNullOrWhiteSpace(value))
            {
                return value;
            }

            if (!string.IsNullOrWhiteSpace(fallback))
            {
                return fallback;
            }

            return key ?? string.Empty;
        }

        public void Replace(IEnumerable<LocalizationEntry> entries)
        {
            entries_.Clear();

            if (entries == null)
            {
                return;
            }

            foreach (LocalizationEntry entry in entries)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.Key))
                {
                    continue;
                }

                entries_[entry.Key.Trim()] = entry.Value ?? string.Empty;
            }
        }
    }
}
