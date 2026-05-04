using System;
using Flowbit.Utilities.Core.Events;
using Game.Core.Configuration;
using Game.Core.Data;
using Game.Core.Events;

namespace Game.Core.Services
{
    /// <summary>
    /// Temporary local implementation of profile flows.
    /// This will later be replaced by a server-backed implementation.
    /// </summary>
    public sealed class LocalProfileService : IProfileService
    {
        private readonly ClientGameStateStore store_;
        private readonly EventDispatcher dispatcher_;

        public LocalProfileService(ClientGameStateStore store, EventDispatcher dispatcher)
        {
            store_ = store;
            dispatcher_ = dispatcher;
        }

        public bool CanUseName(string name) =>
            (store_.CurrentProfile != null && name == store_.CurrentProfile.Name) ||
            (!string.IsNullOrWhiteSpace(name) && !store_.AllProfiles.Exists(item => item.Name == name));

        public bool CanUsePetName(string name) => !string.IsNullOrWhiteSpace(name);

        public Profile CreateProfile(string name, string petName, int pictureId)
        {
            if (!CanUseName(name))
            {
                dispatcher_?.Send(new DataSavedEvent(DataSaveStatus.NAME_EXIST));
                return null;
            }

            Profile profile = new Profile
            {
                Name = name,
                ProfilePictureId = pictureId,
                BrushSessionDurationMinutes = DefaultProfileState.DefaultBrushSessionDurationMinutes,
                PetData = new Pet
                {
                    Name = petName,
                    lastEatTime = -1,
                    eatCount = 0,
                    lastBrushTime = -1,
                    EyesItemId = DefaultProfileState.DefaultPetEyesItemId,
                    SkinItemId = DefaultProfileState.DefaultPetSkinItemId,
                    HatItemId = DefaultProfileState.DefaultPetHatItemId,
                    DressItemId = DefaultProfileState.DefaultPetDressItemId
                },
                RoomData = new Room(),
                InventoryData = DefaultProfileState.CreateInventory(),
                PendingReward = DefaultProfileState.InitialPendingReward
            };

            store_.AddProfile(profile);
            SwitchProfile(store_.AllProfiles.Count - 1);
            return profile;
        }

        public void SwitchProfile(int index)
        {
            store_.SelectProfile(index);
            NotifyDataChanged();
            dispatcher_?.Send(new ProfileSwitchedEvent());
        }

        public bool DeleteProfile(int index)
        {
            if (index < 0 || index >= store_.AllProfiles.Count)
            {
                return false;
            }

            if (store_.AllProfiles.Count <= 1)
            {
                return false;
            }

            bool deletingCurrentProfile = index == store_.CurrentProfileIndex;
            store_.RemoveProfileAt(index);

            if (deletingCurrentProfile)
            {
                int replacementIndex = Math.Max(0, Math.Min(index, store_.AllProfiles.Count - 1));
                store_.SelectProfile(replacementIndex);
                dispatcher_?.Send(new ProfileSwitchedEvent());
            }
            else if (store_.CurrentProfileIndex > index)
            {
                store_.SelectProfile(store_.CurrentProfileIndex - 1);
            }

            dispatcher_?.Send(new ProfileUpdatedEvent());
            NotifyDataChanged();
            return true;
        }

        public void ModifyCurrentProfile(string name, string petName, int pictureId)
        {
            if (!CanUseName(name))
            {
                dispatcher_?.Send(new DataSavedEvent(DataSaveStatus.NAME_EXIST));
                return;
            }

            if (store_.CurrentProfile == null)
            {
                dispatcher_?.Send(new DataSavedEvent(DataSaveStatus.NO_CURRENT_PROFILE));
                return;
            }

            store_.CurrentProfile.Name = name;
            store_.CurrentProfile.PetData.Name = petName;
            store_.CurrentProfile.ProfilePictureId = pictureId;
            dispatcher_?.Send(new ProfileUpdatedEvent());
            NotifyDataChanged();
        }

        public void SetPetName(string name)
        {
            if (store_.CurrentProfile?.PetData == null)
            {
                return;
            }

            if (!CanUsePetName(name) || store_.CurrentProfile.PetData.Name == name)
            {
                return;
            }

            store_.CurrentProfile.PetData.Name = name;
            dispatcher_?.Send(new ProfileUpdatedEvent());
            NotifyDataChanged();
        }

        private void NotifyDataChanged()
        {
            dispatcher_?.Send(new LocalDataChangedEvent());
        }
    }
}
