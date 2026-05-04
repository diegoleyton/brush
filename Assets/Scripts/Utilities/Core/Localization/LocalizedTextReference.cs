using System;

namespace Flowbit.Utilities.Localization
{
    /// <summary>
    /// Serializable localization reference with fallback text.
    /// </summary>
    [Serializable]
    public sealed class LocalizedTextReference
    {
        public string Key = string.Empty;
        public string Fallback = string.Empty;
    }
}
