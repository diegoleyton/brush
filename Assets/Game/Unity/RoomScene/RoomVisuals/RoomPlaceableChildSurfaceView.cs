using UnityEngine;

namespace Game.Unity.RoomScene
{
    /// <summary>
    /// Always-visible surface that hosts a child placed object inside a parent room object.
    /// </summary>
    public sealed class RoomPlaceableChildSurfaceView : RoomPlaceableObjectSurfaceBase
    {
        [SerializeField]
        private int slotId_;

        public int SlotId => slotId_;
    }
}
