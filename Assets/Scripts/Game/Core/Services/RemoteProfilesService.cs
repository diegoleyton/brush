using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Flowbit.Utilities.Core.Events;
using Game.Core.Data;
using Game.Core.Events;

namespace Game.Core.Services
{
    /// <summary>
    /// Server-backed profile application service that keeps the local client state store in sync.
    /// </summary>
    public sealed class RemoteProfilesService : IProfilesService
    {
        private readonly ClientGameStateStore store_;
        private readonly IAuthService authService_;
        private readonly IChildrenApiClient childrenApiClient_;
        private readonly EventDispatcher dispatcher_;

        public RemoteProfilesService(
            ClientGameStateStore store,
            IAuthService authService,
            IChildrenApiClient childrenApiClient,
            EventDispatcher dispatcher)
        {
            store_ = store;
            authService_ = authService;
            childrenApiClient_ = childrenApiClient;
            dispatcher_ = dispatcher;
        }

        public bool CanUseName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return false;
            }

            string trimmed = name.Trim();
            for (int index = 0; index < store_.AllProfiles.Count; index++)
            {
                Profile profile = store_.AllProfiles[index];
                if (profile != null && string.Equals(profile.Name, trimmed, StringComparison.Ordinal))
                {
                    return false;
                }
            }

            return true;
        }

        public bool CanUsePetName(string name) => !string.IsNullOrWhiteSpace(name);

        public async Task RefreshAsync()
        {
            if (authService_ == null || !authService_.HasSession)
            {
                return;
            }

            string selectedRemoteId = store_.CurrentProfile?.RemoteProfileId;
            IReadOnlyList<Profile> profiles = await childrenApiClient_.ListAsync();
            bool selectionChanged = ReplaceProfiles(profiles, selectedRemoteId);
            dispatcher_?.Send(new ProfileUpdatedEvent());
            if (selectionChanged)
            {
                dispatcher_?.Send(new ProfileSwitchedEvent());
            }

            dispatcher_?.Send(new LocalDataChangedEvent());
        }

        public async Task<Profile> CreateAsync(string name, string petName, int pictureId)
        {
            if (authService_ == null || !authService_.HasSession)
            {
                return null;
            }

            Profile createdProfile = await childrenApiClient_.CreateAsync(name, petName, pictureId);
            if (createdProfile == null)
            {
                return null;
            }

            store_.AddProfile(createdProfile);
            store_.SelectProfile(store_.AllProfiles.Count - 1);
            dispatcher_?.Send(new ProfileUpdatedEvent());
            dispatcher_?.Send(new ProfileSwitchedEvent());
            dispatcher_?.Send(new LocalDataChangedEvent());
            return createdProfile;
        }

        public async Task<bool> DeleteAsync(int profileIndex)
        {
            if (authService_ == null || !authService_.HasSession)
            {
                return false;
            }

            if (profileIndex < 0 || profileIndex >= store_.AllProfiles.Count)
            {
                return false;
            }

            Profile profile = store_.AllProfiles[profileIndex];
            if (profile == null || string.IsNullOrWhiteSpace(profile.RemoteProfileId))
            {
                return false;
            }

            bool deleted = await childrenApiClient_.DeleteAsync(profile.RemoteProfileId);
            if (!deleted)
            {
                return false;
            }

            bool deletingCurrentProfile = profileIndex == store_.CurrentProfileIndex;
            store_.RemoveProfileAt(profileIndex);

            if (store_.AllProfiles.Count > 0)
            {
                int selectedIndex = deletingCurrentProfile
                    ? Math.Max(0, Math.Min(profileIndex, store_.AllProfiles.Count - 1))
                    : store_.CurrentProfileIndex;

                if (selectedIndex >= 0 && selectedIndex < store_.AllProfiles.Count)
                {
                    store_.SelectProfile(selectedIndex);
                }
            }

            dispatcher_?.Send(new ProfileUpdatedEvent());
            if (deletingCurrentProfile)
            {
                dispatcher_?.Send(new ProfileSwitchedEvent());
            }

            dispatcher_?.Send(new LocalDataChangedEvent());
            return true;
        }

        public void SelectProfile(int profileIndex)
        {
            store_.SelectProfile(profileIndex);
            dispatcher_?.Send(new ProfileSwitchedEvent());
            dispatcher_?.Send(new LocalDataChangedEvent());
        }

        private bool ReplaceProfiles(IReadOnlyList<Profile> profiles, string selectedRemoteId)
        {
            string previousSelectedRemoteId = store_.CurrentProfile?.RemoteProfileId;
            Dictionary<string, Profile> existingProfilesByRemoteId = BuildExistingProfilesByRemoteId();
            store_.AllProfiles.Clear();

            int selectedIndex = -1;
            for (int index = 0; index < profiles.Count; index++)
            {
                Profile serverProfile = profiles[index];
                if (serverProfile == null)
                {
                    continue;
                }

                Profile profile = MergeServerProfile(serverProfile, existingProfilesByRemoteId);
                store_.AddProfile(profile);
                if (!string.IsNullOrWhiteSpace(selectedRemoteId) &&
                    string.Equals(profile.RemoteProfileId, selectedRemoteId, StringComparison.Ordinal))
                {
                    selectedIndex = store_.AllProfiles.Count - 1;
                }
            }

            if (store_.AllProfiles.Count == 0)
            {
                store_.Data.CurrentProfile = -1;
                return !string.IsNullOrWhiteSpace(previousSelectedRemoteId);
            }

            if (selectedIndex < 0)
            {
                selectedIndex = Math.Min(store_.CurrentProfileIndex, store_.AllProfiles.Count - 1);
                if (selectedIndex < 0)
                {
                    selectedIndex = 0;
                }
            }

            store_.SelectProfile(selectedIndex);
            string refreshedSelectedRemoteId = store_.CurrentProfile?.RemoteProfileId;
            return !string.Equals(previousSelectedRemoteId, refreshedSelectedRemoteId, StringComparison.Ordinal);
        }

        private Dictionary<string, Profile> BuildExistingProfilesByRemoteId()
        {
            Dictionary<string, Profile> existingProfiles = new Dictionary<string, Profile>(StringComparer.Ordinal);

            for (int index = 0; index < store_.AllProfiles.Count; index++)
            {
                Profile profile = store_.AllProfiles[index];
                if (profile == null || string.IsNullOrWhiteSpace(profile.RemoteProfileId))
                {
                    continue;
                }

                existingProfiles[profile.RemoteProfileId] = profile;
            }

            return existingProfiles;
        }

        private static Profile MergeServerProfile(Profile serverProfile, IReadOnlyDictionary<string, Profile> existingProfilesByRemoteId)
        {
            if (serverProfile == null)
            {
                return null;
            }

            if (existingProfilesByRemoteId == null ||
                string.IsNullOrWhiteSpace(serverProfile.RemoteProfileId) ||
                !existingProfilesByRemoteId.TryGetValue(serverProfile.RemoteProfileId, out Profile existingProfile) ||
                existingProfile == null)
            {
                return serverProfile;
            }

            existingProfile.Name = serverProfile.Name;
            existingProfile.ProfilePictureId = serverProfile.ProfilePictureId;

            if (existingProfile.PetData == null)
            {
                existingProfile.PetData = serverProfile.PetData;
            }
            else if (serverProfile.PetData != null)
            {
                existingProfile.PetData.Name = serverProfile.PetData.Name;
            }

            return existingProfile;
        }
    }
}
