using System;
using System.Collections.Generic;
using Game.Core.Data;
using GameState = Game.Core.Data.Data;

namespace Game.Core.Services
{
    /// <summary>
    /// Client-side state store used by the Unity presentation layer.
    /// The server is intended to become the source of truth over time.
    /// </summary>
    public sealed class ClientGameStateStore
    {
        public ClientGameStateStore(GameState data)
        {
            Data = data ?? new GameState();
            NormalizeCurrentProfileIndex();
        }

        public GameState Data { get; }

        public List<Profile> AllProfiles => Data.Profiles;

        public int CurrentProfileIndex => Data.CurrentProfile;

        public Profile CurrentProfile =>
            Data.CurrentProfile >= 0 && Data.CurrentProfile < Data.Profiles.Count
                ? Data.Profiles[Data.CurrentProfile]
                : null;

        public void SelectProfile(int index)
        {
            if (index < 0 || index >= Data.Profiles.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index), "Profile index is out of range.");
            }

            Data.CurrentProfile = index;
        }

        public void AddProfile(Profile profile)
        {
            if (profile == null)
            {
                throw new ArgumentNullException(nameof(profile));
            }

            Data.Profiles.Add(profile);
        }

        public void RemoveProfileAt(int index)
        {
            if (index < 0 || index >= Data.Profiles.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index), "Profile index is out of range.");
            }

            Data.Profiles.RemoveAt(index);
            NormalizeCurrentProfileIndex();
        }

        private void NormalizeCurrentProfileIndex()
        {
            if (Data.Profiles == null)
            {
                Data.Profiles = new List<Profile>();
            }

            if (Data.Profiles.Count == 0)
            {
                Data.CurrentProfile = -1;
                return;
            }

            if (Data.CurrentProfile < -1)
            {
                Data.CurrentProfile = -1;
                return;
            }

            if (Data.CurrentProfile >= Data.Profiles.Count)
            {
                Data.CurrentProfile = Data.Profiles.Count - 1;
            }
        }
    }
}
