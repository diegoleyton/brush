namespace Flowbit.Utilities.Core.Logger
{
    /// <summary>
    /// Framework-agnostic logger used by core systems.
    /// </summary>
    public interface IGameLogger
    {
        void Log(string message);
        void LogWarning(string message);
    }
}
