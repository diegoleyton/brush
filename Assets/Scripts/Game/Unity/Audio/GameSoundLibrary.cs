using Flowbit.Utilities.Audio;
using UnityEngine;

namespace Game.Unity.Audio
{
    [CreateAssetMenu(fileName = "GameSoundLibrary", menuName = "Game/Audio/Game Sound Library")]
    public sealed class GameSoundLibrary : SoundLibrary<SoundId>
    {
        [field: SerializeField]
        public GameObject AudioPool { get; private set; }

        [field: SerializeField]
        public float MusicTransitionTime { get; private set; } = 0.25f;
    }
}
