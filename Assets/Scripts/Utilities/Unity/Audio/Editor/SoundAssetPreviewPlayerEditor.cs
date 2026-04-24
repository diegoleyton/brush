#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

using Flowbit.Utilities.Audio;
using Flowbit.Utilities.Unity.Audio;

namespace Flowbit.Utilities.Unity.Audio.Editor
{
    /// <summary>
    /// Custom inspector for SoundAssetPreviewPlayer with Play Mode preview buttons
    /// and inline editing of the assigned SoundAsset.
    /// </summary>
    [CustomEditor(typeof(SoundAssetPreviewPlayer))]
    public sealed class SoundAssetPreviewPlayerEditor : UnityEditor.Editor
    {
        private bool showSoundAssetSettings_ = true;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            SoundAssetPreviewPlayer previewPlayer = (SoundAssetPreviewPlayer)target;
            SoundAsset soundAsset = previewPlayer.SoundAsset;

            DrawSoundAssetEditor(soundAsset);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Preview Controls", EditorStyles.boldLabel);

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox(
                    "Enter Play Mode to preview the sound with real pitch and volume.",
                    MessageType.Info);
                return;
            }

            using (new EditorGUI.DisabledScope(
                       previewPlayer.SoundAsset == null ||
                       previewPlayer.AudioSource == null))
            {
                if (GUILayout.Button("Play Random Preview"))
                {
                    previewPlayer.PlayRandomPreview();
                }

                if (GUILayout.Button("Stop Preview"))
                {
                    previewPlayer.StopPreview();
                }
            }

            if (previewPlayer.SoundAsset == null)
            {
                EditorGUILayout.HelpBox(
                    "Assign a SoundAsset to enable preview and inline editing.",
                    MessageType.Warning);
            }
            else if (previewPlayer.AudioSource == null)
            {
                EditorGUILayout.HelpBox(
                    "Assign an AudioSource to enable preview.",
                    MessageType.Warning);
            }
        }

        private void DrawSoundAssetEditor(SoundAsset soundAsset)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Sound Asset Settings", EditorStyles.boldLabel);

            if (soundAsset == null)
            {
                EditorGUILayout.HelpBox(
                    "No SoundAsset assigned.",
                    MessageType.Info);
                return;
            }

            showSoundAssetSettings_ = EditorGUILayout.Foldout(
                showSoundAssetSettings_,
                $"Edit '{soundAsset.name}'",
                true);

            if (!showSoundAssetSettings_)
            {
                return;
            }

            using SerializedObject soundAssetSerializedObject = new SerializedObject(soundAsset);

            SerializedProperty clipsProperty =
                soundAssetSerializedObject.FindProperty("_clips");
            SerializedProperty volumeRangeProperty =
                soundAssetSerializedObject.FindProperty("_volumeRange");
            SerializedProperty pitchRangeProperty =
                soundAssetSerializedObject.FindProperty("_pitchRange");

            soundAssetSerializedObject.Update();

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.PropertyField(clipsProperty, includeChildren: true);
            EditorGUILayout.PropertyField(volumeRangeProperty);
            EditorGUILayout.PropertyField(pitchRangeProperty);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(soundAsset, "Edit Sound Asset");

                soundAssetSerializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(soundAsset);

                AssetDatabase.SaveAssets();
            }

            EditorGUILayout.Space();

            using (new EditorGUI.DisabledScope(soundAsset == null))
            {
                if (GUILayout.Button("Select Sound Asset"))
                {
                    Selection.activeObject = soundAsset;
                    EditorGUIUtility.PingObject(soundAsset);
                }
            }
        }
    }
}
#endif