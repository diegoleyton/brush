using System;

namespace Flowbit.Utilities.Localization
{
    /// <summary>
    /// Serializable key/value pair for lightweight localization tables.
    /// </summary>
    [Serializable]
    public sealed class LocalizationEntry
    {
        public string Key = string.Empty;
        public string Value = string.Empty;
    }
}
