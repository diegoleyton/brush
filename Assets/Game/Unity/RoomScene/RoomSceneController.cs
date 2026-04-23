using Flowbit.Utilities.Navigation;

using Game.Unity.Definitions;
using Game.Unity.Scenes;

using UnityEngine;

namespace Game.Unity.RoomScene
{
    /// <summary>
    /// Navigation parameters for the room scene.
    /// </summary>
    public sealed class RoomSceneParams : NavigationParams
    {
    }

    /// <summary>
    /// Scene entry point for the room scene. Handles scene initialization and parameters.
    /// </summary>
    public sealed class RoomSceneController : SceneBase
    {
        [SerializeField]
        private RoomController roomController_;

        [SerializeField]
        private RoomViewController roomViewController_;

        private void Reset()
        {
            sceneType_ = SceneType.RoomScene;
        }

        protected override void Initialize()
        {
            if (roomController_ == null)
            {
                roomController_ = GetComponentInChildren<RoomController>(true);
            }

            if (roomController_ == null)
            {
                throw new System.InvalidOperationException(
                    $"{nameof(RoomSceneController)} requires a {nameof(RoomController)} reference.");
            }

            if (roomViewController_ == null)
            {
                roomViewController_ = GetComponentInChildren<RoomViewController>(true);
            }

            if (roomViewController_ == null)
            {
                throw new System.InvalidOperationException(
                    $"{nameof(RoomSceneController)} requires a {nameof(RoomViewController)} reference.");
            }

            roomController_.Initialize();
            roomViewController_.SetDropAreas(roomController_.DropAreas);
            roomViewController_.Initialize();
        }
    }
}
