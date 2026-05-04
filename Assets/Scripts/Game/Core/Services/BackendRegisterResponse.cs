using System;

namespace Game.Core.Services
{
    /// <summary>
    /// Minimal backend register response.
    /// </summary>
    [Serializable]
    public sealed class BackendRegisterResponse
    {
        public bool IsSuccess = true;
        public string ErrorMessage;
        public ParentSummary parentUser;
        public FamilySummary family;
    }

    [Serializable]
    public sealed class ParentSummary
    {
        public string id;
        public string authUserId;
        public string email;
    }

    [Serializable]
    public sealed class FamilySummary
    {
        public string id;
        public string name;
    }
}
