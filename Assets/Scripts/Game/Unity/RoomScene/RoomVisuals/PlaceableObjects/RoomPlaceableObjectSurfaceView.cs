using UnityEngine;

namespace Game.Unity.RoomScene
{
    /// <summary>
    /// Always-visible surface that hosts a top-level placed room object.
    /// </summary>
    public sealed class RoomPlaceableObjectSurfaceView : RoomPlaceableObjectSurfaceBase
    {
        [SerializeField]
        private int locationId_;

        public int LocationId => locationId_;
    }
}
