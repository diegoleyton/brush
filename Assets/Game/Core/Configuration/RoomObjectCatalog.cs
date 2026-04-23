using System;
using System.Collections.Generic;

namespace Game.Core.Configuration
{
    /// <summary>
    /// Catalog describing which room objects support paint and child slots.
    /// </summary>
    public static class RoomObjectCatalog
    {
        private static readonly RoomObjectDefinition defaultDefinition_ =
            new RoomObjectDefinition(false, Array.Empty<int>());

        private static readonly IReadOnlyDictionary<int, RoomObjectDefinition> all_ =
            new Dictionary<int, RoomObjectDefinition>
            {
                // Configure container and paintable object ids here.
                // Example: { 42, new RoomObjectDefinition(true, new[] { 1, 2 }) }
            };

        public static RoomObjectDefinition Get(int itemId)
        {
            if (all_.TryGetValue(itemId, out RoomObjectDefinition definition))
            {
                return definition;
            }

            return defaultDefinition_;
        }

        public static bool SupportsPaint(int itemId) => Get(itemId).SupportsPaint;

        public static bool SupportsChildSlot(int itemId, int slotId)
        {
            IReadOnlyList<int> childSlotIds = Get(itemId).ChildSlotIds;

            for (int index = 0; index < childSlotIds.Count; index++)
            {
                if (childSlotIds[index] == slotId)
                {
                    return true;
                }
            }

            return false;
        }
    }

    /// <summary>
    /// Configuration for a room object type.
    /// </summary>
    public sealed class RoomObjectDefinition
    {
        public RoomObjectDefinition(bool supportsPaint, IReadOnlyList<int> childSlotIds)
        {
            SupportsPaint = supportsPaint;
            ChildSlotIds = childSlotIds ?? Array.Empty<int>();
        }

        public bool SupportsPaint { get; }

        public IReadOnlyList<int> ChildSlotIds { get; }
    }
}
