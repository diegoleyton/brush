using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

using Game.Core.Configuration;

namespace Game.Core.Data
{
    /// <summary>
    /// Root save data for the game.
    /// </summary>
    [Serializable]
    public class Data
    {
        public List<Profile> Profiles = new List<Profile>();
        public int CurrentProfile = -1;
    }

    /// <summary>
    /// Persisted profile selected by a player.
    /// </summary>
    [Serializable]
    public class Profile
    {
        public string Name;
        public int ProfilePictureId;
        public int Coins = DefaultProfileState.InitialCoins;
        public float BrushSessionDurationMinutes = DefaultProfileState.DefaultBrushSessionDurationMinutes;
        public Pet PetData = new Pet();
        public Room RoomData = new Room();
        public Inventory InventoryData = new Inventory();
        public bool PendingReward = false;
        public bool Muted = false;
    }

    /// <summary>
    /// Persisted state for the player's pet.
    /// </summary>
    [Serializable]
    public class Pet
    {
        public string Name;
        public long lastEatTime;
        public int eatCount;
        public long lastBrushTime;
        [FormerlySerializedAs("FaceItemId")]
        public int EyesItemId = DefaultProfileState.DefaultPetEyesItemId;
        public int SkinItemId = DefaultProfileState.DefaultPetSkinItemId;
        public int HatItemId = DefaultProfileState.DefaultPetHatItemId;
        public int DressItemId = DefaultProfileState.DefaultPetDressItemId;
    }

    /// <summary>
    /// Persisted room state including placed objects and painted surfaces.
    /// </summary>
    [Serializable]
    public class Room
    {
        [FormerlySerializedAs("Objects")]
        public List<PlacedRoomObjectLocation> PlaceableObjects = new List<PlacedRoomObjectLocation>();
        public List<RoomPaintSurfaceState> PaintedSurfaces = new List<RoomPaintSurfaceState>();
    }

    /// <summary>
    /// A top-level room location that can hold one object.
    /// </summary>
    [Serializable]
    public class PlacedRoomObjectLocation
    {
        public int LocationId;
        public PlacedRoomObject Item;
    }

    /// <summary>
    /// A placed room object that can optionally contain child objects.
    /// </summary>
    [Serializable]
    public class PlacedRoomObject
    {
        public int ItemId;
        public int PaintId = DefaultProfileState.NoPaintItemId;
    }

    /// <summary>
    /// Paint state for a room surface such as a wall or bed.
    /// </summary>
    [Serializable]
    public class RoomPaintSurfaceState
    {
        public int SurfaceId;
        public int PaintId;
    }

    /// <summary>
    /// Player inventory grouped by interaction type.
    /// </summary>
    [Serializable]
    public class Inventory : ISerializationCallbackReceiver
    {
        [NonSerialized]
        [FormerlySerializedAs("Objects")]
        public Dictionary<int, int> PlaceableObjects = new Dictionary<int, int>();
        [NonSerialized]
        public Dictionary<int, int> Paint = new Dictionary<int, int>();
        [NonSerialized]
        public Dictionary<int, int> Food = new Dictionary<int, int>();
        [NonSerialized]
        public Dictionary<int, int> Skin = new Dictionary<int, int>();
        [NonSerialized]
        public Dictionary<int, int> Hat = new Dictionary<int, int>();
        [NonSerialized]
        public Dictionary<int, int> Dress = new Dictionary<int, int>();
        [NonSerialized]
        [FormerlySerializedAs("Face")]
        public Dictionary<int, int> Eyes = new Dictionary<int, int>();

        [SerializeField]
        private List<InventoryItemAmount> placableObjectsSerialized_ = new List<InventoryItemAmount>();

        [SerializeField]
        private List<InventoryItemAmount> paintSerialized_ = new List<InventoryItemAmount>();

        [SerializeField]
        private List<InventoryItemAmount> foodSerialized_ = new List<InventoryItemAmount>();

        [SerializeField]
        private List<InventoryItemAmount> skinSerialized_ = new List<InventoryItemAmount>();

        [SerializeField]
        private List<InventoryItemAmount> hatSerialized_ = new List<InventoryItemAmount>();

        [SerializeField]
        private List<InventoryItemAmount> dressSerialized_ = new List<InventoryItemAmount>();

        [SerializeField]
        [FormerlySerializedAs("faceSerialized_")]
        private List<InventoryItemAmount> eyesSerialized_ = new List<InventoryItemAmount>();

        public void OnBeforeSerialize()
        {
            placableObjectsSerialized_ = SerializeDictionary(PlaceableObjects);
            paintSerialized_ = SerializeDictionary(Paint);
            foodSerialized_ = SerializeDictionary(Food);
            skinSerialized_ = SerializeDictionary(Skin);
            hatSerialized_ = SerializeDictionary(Hat);
            dressSerialized_ = SerializeDictionary(Dress);
            eyesSerialized_ = SerializeDictionary(Eyes);
        }

        public void OnAfterDeserialize()
        {
            PlaceableObjects = DeserializeDictionary(placableObjectsSerialized_);
            Paint = DeserializeDictionary(paintSerialized_);
            Food = DeserializeDictionary(foodSerialized_);
            Skin = DeserializeDictionary(skinSerialized_);
            Hat = DeserializeDictionary(hatSerialized_);
            Dress = DeserializeDictionary(dressSerialized_);
            Eyes = DeserializeDictionary(eyesSerialized_);
        }

        private static List<InventoryItemAmount> SerializeDictionary(Dictionary<int, int> items)
        {
            List<InventoryItemAmount> serialized = new List<InventoryItemAmount>();

            if (items == null)
            {
                return serialized;
            }

            foreach (KeyValuePair<int, int> entry in items)
            {
                serialized.Add(new InventoryItemAmount
                {
                    ItemId = entry.Key,
                    Amount = entry.Value
                });
            }

            return serialized;
        }

        private static Dictionary<int, int> DeserializeDictionary(List<InventoryItemAmount> items)
        {
            Dictionary<int, int> deserialized = new Dictionary<int, int>();

            if (items == null)
            {
                return deserialized;
            }

            foreach (InventoryItemAmount entry in items)
            {
                if (entry == null)
                {
                    continue;
                }

                deserialized[entry.ItemId] = entry.Amount;
            }

            return deserialized;
        }
    }

    /// <summary>
    /// Serializable key-value representation of an inventory entry.
    /// </summary>
    [Serializable]
    public class InventoryItemAmount
    {
        public int ItemId;
        public int Amount;
    }
}
