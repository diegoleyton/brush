using System.Threading.Tasks;

namespace Flowbit.Utilities.RemoteCommunication
{
    /// <summary>
    /// Dispatches remote requests and returns raw responses.
    /// </summary>
    public interface IRemoteRequestDispatcher
    {
        Task<RemoteResponse> SendAsync(RemoteRequest request);
    }
}
