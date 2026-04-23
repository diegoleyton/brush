using System.Collections.Generic;

using Game.Core.Data;
using Game.Core.Services;
using Game.Unity.Settings;
using Flowbit.Utilities.Unity.Instantiator;

using UnityEngine;

namespace Game.Unity.RoomScene
{
    /// <summary>
    /// Applies persisted placeable room objects to the room scene visuals.
    /// </summary>
    public sealed class PlaceableObjectRoomViewStrategy
    {
        private readonly DataRepository repository_;
        private readonly IReadOnlyList<RoomObjectDropArea> dropAreas_;
        private readonly RoomSettings roomSettings_;
        private readonly IObjectInstantiator instantiator_;

        public PlaceableObjectRoomViewStrategy(
            DataRepository repository,
            IReadOnlyList<RoomObjectDropArea> dropAreas,
            RoomSettings roomSettings,
            IObjectInstantiator instantiator)
        {
            repository_ = repository;
            dropAreas_ = dropAreas;
            roomSettings_ = roomSettings;
            instantiator_ = instantiator;
        }

        public void Refresh()
        {
            ClearAllPlaceableObjectAreas();

            List<PlacedRoomObjectLocation> placeableObjects = repository_?.CurrentProfile?.RoomData?.PlaceableObjects;
            if (placeableObjects == null)
            {
                return;
            }

            for (int index = 0; index < placeableObjects.Count; index++)
            {
                PlacedRoomObjectLocation location = placeableObjects[index];
                if (location?.Item == null)
                {
                    continue;
                }

                RoomObjectDropArea dropArea = FindDropArea(location.LocationId);
                if (dropArea == null)
                {
                    continue;
                }

                RectTransform visualInstance = CreateVisualInstance(location.Item.ItemId);
                if (visualInstance == null)
                {
                    continue;
                }

                dropArea.SetPlacedVisual(visualInstance);
            }
        }

        private void ClearAllPlaceableObjectAreas()
        {
            if (dropAreas_ == null)
            {
                return;
            }

            for (int index = 0; index < dropAreas_.Count; index++)
            {
                RoomObjectDropArea dropArea = dropAreas_[index];
                if (dropArea != null && dropArea.SupportedInventoryType == InteractionPointType.PLACEABLE_OBJECT)
                {
                    dropArea.ClearPlacedVisual();
                }
            }
        }

        private RoomObjectDropArea FindDropArea(int targetId)
        {
            if (dropAreas_ == null)
            {
                return null;
            }

            for (int index = 0; index < dropAreas_.Count; index++)
            {
                RoomObjectDropArea dropArea = dropAreas_[index];
                if (dropArea == null)
                {
                    continue;
                }

                if (dropArea.TargetId == targetId &&
                    dropArea.SupportedInventoryType == InteractionPointType.PLACEABLE_OBJECT)
                {
                    return dropArea;
                }
            }

            return null;
        }

        private RectTransform CreateVisualInstance(int itemId)
        {
            RectTransform prefab = roomSettings_?.PlaceableObjectPrefab;
            if (prefab == null)
            {
                return null;
            }

            RectTransform instance = instantiator_ != null
                ? instantiator_.InstantiatePrefab(prefab)
                : UnityEngine.Object.Instantiate(prefab);
            RoomPlaceableObjectView placeableObjectView = instance.GetComponent<RoomPlaceableObjectView>();
            if (placeableObjectView != null)
            {
                placeableObjectView.SetColor(
                    RoomItemVisuals.GetItemColor(InteractionPointType.PLACEABLE_OBJECT, itemId));
            }

            return instance;
        }
    }
}
