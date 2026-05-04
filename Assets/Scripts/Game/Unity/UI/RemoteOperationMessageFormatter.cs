using System;
using Flowbit.Utilities.Localization;

namespace Game.Unity.UI
{
    /// <summary>
    /// Maps low-level remote operation failures to concise player-facing messages.
    /// </summary>
    public static class RemoteOperationMessageFormatter
    {
        public static string Format(Exception exception, string fallbackMessage)
        {
            string message = exception?.GetBaseException()?.Message ?? string.Empty;
            if (string.IsNullOrWhiteSpace(message))
            {
                return fallbackMessage;
            }

            if (Contains(message, "Cannot connect to destination host") ||
                Contains(message, "timed out") ||
                Contains(message, "network") ||
                Contains(message, "offline"))
            {
                return LocalizationServiceLocator.GetText(
                    "errors.network.offline",
                    "No connection. Please try again.");
            }

            if (Contains(message, "401") || Contains(message, "Unauthorized"))
            {
                return LocalizationServiceLocator.GetText(
                    "errors.auth.session_expired",
                    "Your session expired. Please log in again.");
            }

            return fallbackMessage;
        }

        private static bool Contains(string value, string fragment)
        {
            return value.IndexOf(fragment, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
