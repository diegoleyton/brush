namespace Game.Core.Configuration
{
    /// <summary>
    /// Shared ids for room locations, paintable surfaces, and other fixed game references.
    /// </summary>
    public static class GameIds
    {
        public const int TerminalLocationId = 100;
        public const int FurnitureLeftLocationId = 101;
        public const int FurnitureRightLocationId = 102;

        public const int ChairSurfaceId = 1;
        public const int BedSurfaceId = 2;
        public const int StoolSurfaceId = 3;
        public const int RugSurfaceId = 4;
        public const int WallSurfaceId = 5;
        public const int FrameSurfaceId = 6;

        public const int ProfilePictureCount = 9;

        public static bool IsRoomObjectLocationId(int id) =>
            id >= TerminalLocationId && id <= FurnitureRightLocationId;

        public static bool IsRoomSurfaceId(int id) => id >= ChairSurfaceId && id <= FrameSurfaceId;
    }
}
