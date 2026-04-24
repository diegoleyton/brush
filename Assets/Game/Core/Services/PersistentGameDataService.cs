using Flowbit.Utilities.Core.Events;
using Flowbit.Utilities.Core.Logger;
using Flowbit.Utilities.Storage;

using Game.Core.Data;
using Game.Core.Events;
namespace Game.Core.Services
{
    /// <summary>
    /// Hooks the runtime repository to persistent local storage.
    /// </summary>
    public sealed class PersistentGameDataService : System.IDisposable
    {
        public const string GameDataStorageKey = "game_data";

        private readonly EventDispatcher dispatcher_;
        private readonly IDataStorage dataStorage_;
        private readonly DataRepository repository_;
        private readonly IGameLogger logger_;

        public PersistentGameDataService(
            EventDispatcher dispatcher,
            IDataStorage dataStorage,
            DataRepository repository,
            IGameLogger logger)
        {
            dispatcher_ = dispatcher;
            dataStorage_ = dataStorage;
            repository_ = repository;
            logger_ = logger;

            dispatcher_.Subscribe<LocalDataChangedEvent>(OnLocalDataChanged);
        }

        public void Initialize()
        {
            logger_?.Log("[Persistence] Initialized persistent game data service.");
            dispatcher_.Send(new DataLoadedEvent());
            dispatcher_.Send(new AppReadyEvent());
        }

        public void Dispose()
        {
            dispatcher_.Unsubscribe<LocalDataChangedEvent>(OnLocalDataChanged);
        }

        private void OnLocalDataChanged(LocalDataChangedEvent _)
        {
            dataStorage_.SaveAsync(GameDataStorageKey, repository_.Data)
                .GetAwaiter()
                .GetResult();
            logger_?.Log("[Persistence] Saved game data.");
        }
    }
}
