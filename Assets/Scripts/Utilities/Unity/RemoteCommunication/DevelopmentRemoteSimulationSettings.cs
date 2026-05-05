namespace Flowbit.Utilities.Unity.RemoteCommunication
{
    /// <summary>
    /// Runtime-only developer flags for simulating degraded network conditions in Unity.
    /// </summary>
    public sealed class DevelopmentRemoteSimulationSettings
    {
        public bool SimulateSlowNetwork { get; set; }

        public bool SimulateNetworkFailure { get; set; }

        public bool SimulateUnresponsiveNetwork { get; set; }

        public float SlowNetworkDelaySeconds { get; set; } = 2f;

        public float UnresponsiveNetworkExtraDelaySeconds { get; set; } = 1f;
    }
}
