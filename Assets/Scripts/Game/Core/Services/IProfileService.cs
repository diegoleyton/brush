using Game.Core.Data;

namespace Game.Core.Services
{
    /// <summary>
    /// Application-facing profile use cases for the client.
    /// </summary>
    public interface IProfileService
    {
        bool CanUseName(string name);

        bool CanUsePetName(string name);

        Profile CreateProfile(string name, string petName, int pictureId);

        void SwitchProfile(int index);

        bool DeleteProfile(int index);

        void ModifyCurrentProfile(string name, string petName, int pictureId);

        void SetPetName(string name);
    }
}
