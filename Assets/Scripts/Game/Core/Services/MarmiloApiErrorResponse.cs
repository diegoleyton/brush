using System;

namespace Game.Core.Services
{
    /// <summary>
    /// Generic API error payload used by Supabase and Marmilo backend calls.
    /// </summary>
    [Serializable]
    public sealed class MarmiloApiErrorResponse
    {
        public string message;
        public string msg;
        public string error;
        public string error_description;

        public string ResolveMessage()
        {
            if (!string.IsNullOrWhiteSpace(message))
            {
                return message;
            }

            if (!string.IsNullOrWhiteSpace(msg))
            {
                return msg;
            }

            if (!string.IsNullOrWhiteSpace(error_description))
            {
                return error_description;
            }

            if (!string.IsNullOrWhiteSpace(error))
            {
                return error;
            }

            return "Unexpected API error.";
        }
    }
}
