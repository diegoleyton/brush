using Game.Core.Services;

using Zenject;

namespace Game.Unity.Installers
{
    /// <summary>
    /// Bridges Zenject lifecycle hooks to the core persistent data service.
    /// </summary>
    public sealed class PersistentGameDataLifetime : IInitializable, System.IDisposable
    {
        private readonly PersistentGameDataService service_;

        public PersistentGameDataLifetime(PersistentGameDataService service)
        {
            service_ = service;
        }

        public void Initialize()
        {
            service_.Initialize();
        }

        public void Dispose()
        {
            service_.Dispose();
        }
    }
}
