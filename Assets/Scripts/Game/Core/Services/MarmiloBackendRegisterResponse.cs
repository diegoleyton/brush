using System;

namespace Game.Core.Services
{
    /// <summary>
    /// Minimal backend register response.
    /// </summary>
    [Serializable]
    public sealed class MarmiloBackendRegisterResponse
    {
        public bool IsSuccess = true;
        public string ErrorMessage;
        public MarmiloParentSummary parentUser;
        public MarmiloFamilySummary family;
    }

    [Serializable]
    public sealed class MarmiloParentSummary
    {
        public string id;
        public string authUserId;
        public string email;
    }

    [Serializable]
    public sealed class MarmiloFamilySummary
    {
        public string id;
        public string name;
    }
}
