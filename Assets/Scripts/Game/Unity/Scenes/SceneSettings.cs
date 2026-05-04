using System;
using System.Collections.Generic;
using Flowbit.Utilities.Navigation;
using Flowbit.Utilities.ScreenBlocker;
using Game.Unity.Definitions;
using UnityEngine;

namespace Game.Unity.Scenes
{
    /// <summary>
    /// ScriptableObject containing navigation configuration for the game.
    /// </summary>
    [CreateAssetMenu(fileName = "SceneSettings", menuName = "Flowbit/Scene Settings", order = 0)]
    public class SceneSettings : ScriptableObject
    {
        [Serializable]
        private struct SceneEntry
        {
            public SceneType SceneType;
            public string SceneName;
            public NavigationTargetType TargetType;
            public GameObject Prefab;
        }

        [SerializeField]
        private SceneEntry[] sceneEntries_ = Array.Empty<SceneEntry>();

        [field: SerializeField]
        public GameSceneTransitionBase TransitionNext { get; private set; }

        [field: SerializeField]
        public GameSceneTransitionBase TransitionPrev { get; private set; }

        [field: SerializeField]
        public GameSceneTransitionBase PopupTransitionOpen { get; private set; }

        [field: SerializeField]
        public GameSceneTransitionBase PopupTransitionClose { get; private set; }

        [field: SerializeField]
        public ScreenBlockerView ScreenBlockerView { get; private set; }

        public IReadOnlyDictionary<string, GameObject> GetPrefabs()
        {
            Dictionary<string, GameObject> prefabs = new Dictionary<string, GameObject>();

            foreach (SceneEntry entry in sceneEntries_)
            {
                if (entry.TargetType == NavigationTargetType.Prefab && entry.Prefab != null)
                {
                    prefabs[entry.SceneType.GeTInstruction()] = entry.Prefab;
                }
            }

            return prefabs;
        }

        public NavigationTarget GetTarget(SceneType sceneType)
        {
            foreach (SceneEntry entry in sceneEntries_)
            {
                if (entry.SceneType == sceneType)
                {
                    return new NavigationTarget(
                        entry.TargetType == NavigationTargetType.Scene ? entry.SceneName : entry.SceneType.GeTInstruction(),
                        entry.TargetType);
                }
            }

            return new NavigationTarget(sceneType.GeTInstruction(), NavigationTargetType.Scene);
        }

        public NavigationTarget GetTarget(string sceneName)
        {
            foreach (SceneEntry entry in sceneEntries_)
            {
                if (entry.SceneName == sceneName)
                {
                    return new NavigationTarget(sceneName, NavigationTargetType.Scene);
                }
            }

            return new NavigationTarget(sceneName, NavigationTargetType.Scene);
        }
    }
}
