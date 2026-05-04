using Game.Core.Data;

namespace Game.Core.Services
{
    /// <summary>
    /// Owns room, pet, and inventory gameplay mutations for the active profile.
    /// </summary>
    public interface IRoomGameplayService
    {
        PetEatStatus CanPetEat { get; }

        bool IsTimeToBrush { get; }

        bool RoomHasObject(int locationId, int itemId);

        bool RoomSurfaceHasPaint(int surfaceId, int paintItemId);

        int GetRoomSurfacePaintId(int surfaceId);

        int GetAppliedPetItemId(InteractionPointType interactionPointType);

        bool IsMarketItemAlreadyOwned(InteractionPointType itemType, int itemId);

        void PetEat();

        bool Brush();

        void ResetPetTimes();

        void SetRoomItem(int targetId, int itemId, InteractionPointType interactionPointType);

        void SetRoomObject(int locationId, int itemId);

        void RemoveRoomObject(int locationId);

        void MoveRoomObject(int sourceLocationId, int targetLocationId);

        void ReturnRoomObjectToInventory(int sourceLocationId);

        void PaintRoomSurface(int surfaceId, int paintItemId);

        void SetPetEyes(int itemId);

        void SetPetHat(int itemId);

        void SetPetSkin(int itemId);

        void SetPetDress(int itemId);

        void FeedPet(int itemId);

        void AddPlaceableObject(int id, int quantity);

        void AddPaint(int id, int quantity);

        void AddFood(int id, int quantity);

        void AddSkin(int id, int quantity);

        void AddHat(int id, int quantity);

        void AddDress(int id, int quantity);

        void AddEyes(int id, int quantity);

        void SetMuted(bool muted);
    }
}
