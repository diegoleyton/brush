using Flowbit.Utilities.Core.Logger;

using UnityEngine;

namespace Flowbit.Utilities.Unity.Logger
{
    /// <summary>
    /// Unity-backed logger for game systems.
    /// </summary>
    public sealed class UnityGameLogger : IGameLogger
    {
        public void Log(string message)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log(message);
#endif
        }

        public void LogWarning(string message)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogWarning(message);
#endif
        }
    }
}
