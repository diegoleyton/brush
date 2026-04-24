using System;

namespace Flowbit.Utilities.Navigation
{
    /// <summary>
    /// Represents a navigation destination.
    /// </summary>
    public sealed class NavigationTarget
    {
        /// <summary>
        /// Creates a new navigation target.
        /// </summary>
        public NavigationTarget(string id, NavigationTargetType targetType)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("Target id cannot be null or empty.", nameof(id));
            }

            Id = id;
            TargetType = targetType;
        }

        /// <summary>
        /// Gets the destination identifier.
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Gets the destination type.
        /// </summary>
        public NavigationTargetType TargetType { get; }

        /// <summary>
        /// Returns a readable string representation of the target.
        /// </summary>
        public override string ToString()
        {
            return $"{TargetType}:{Id}";
        }
    }
}