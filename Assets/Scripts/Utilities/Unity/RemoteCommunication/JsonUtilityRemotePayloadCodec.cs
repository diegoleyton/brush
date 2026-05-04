using Flowbit.Utilities.RemoteCommunication;
using UnityEngine;

namespace Flowbit.Utilities.Unity.RemoteCommunication
{
    /// <summary>
    /// JSON codec backed by Unity JsonUtility.
    /// </summary>
    public sealed class JsonUtilityRemotePayloadCodec : IRemotePayloadCodec
    {
        public string Serialize<T>(T payload) => JsonUtility.ToJson(payload);

        public T Deserialize<T>(string payload) where T : class
        {
            if (string.IsNullOrWhiteSpace(payload))
            {
                return null;
            }

            return JsonUtility.FromJson<T>(payload);
        }
    }
}
