namespace Flowbit.Utilities.RemoteCommunication
{
    /// <summary>
    /// Serializes and deserializes payloads used by remote clients.
    /// </summary>
    public interface IRemotePayloadCodec
    {
        string Serialize<T>(T payload);

        T Deserialize<T>(string payload) where T : class;
    }
}
