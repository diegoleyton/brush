using System.Collections.Generic;
using System.IO;

using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace Flowbit.Utilities.Unity.Editor
{
    /// <summary>
    /// Creates or reuses an Addressables group and moves every asset inside a selected folder into it.
    /// </summary>
    public sealed class FolderAddressablesGroupWindow : EditorWindow
    {
        private const string MenuPath = "Assets/Addressables/Make Folder Contents Addressable";
        private const float WindowWidth = 420f;
        private const float WindowHeight = 120f;

        private static string selectedFolderPath_;

        private string groupName_;
        private bool focusGroupNameField_;

        [MenuItem(MenuPath, false, 2000)]
        private static void OpenWindow()
        {
            if (!TryGetSelectedFolderPath(out string folderPath))
            {
                return;
            }

            selectedFolderPath_ = folderPath;

            FolderAddressablesGroupWindow window = CreateInstance<FolderAddressablesGroupWindow>();
            window.titleContent = new GUIContent("Folder Addressables");
            window.groupName_ = Path.GetFileName(folderPath);
            window.focusGroupNameField_ = true;
            window.minSize = new Vector2(WindowWidth, WindowHeight);
            window.maxSize = new Vector2(WindowWidth, WindowHeight);
            window.ShowUtility();
        }

        [MenuItem(MenuPath, true)]
        private static bool ValidateOpenWindow()
        {
            return TryGetSelectedFolderPath(out _);
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Selected Folder", selectedFolderPath_, EditorStyles.wordWrappedLabel);
            EditorGUILayout.Space();

            GUI.SetNextControlName("GroupNameField");
            groupName_ = EditorGUILayout.TextField("Group Name", groupName_);

            if (focusGroupNameField_)
            {
                EditorGUI.FocusTextInControl("GroupNameField");
                focusGroupNameField_ = false;
            }

            EditorGUILayout.HelpBox(
                "Every asset inside the selected folder will be moved into this Addressables group. Each address will use the file name without extension.",
                MessageType.Info);

            HandleKeyboardShortcuts();

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Cancel"))
            {
                Close();
            }

            if (GUILayout.Button("Create Addressables"))
            {
                CreateAddressablesForSelectedFolder();
            }

            EditorGUILayout.EndHorizontal();
        }

        private void HandleKeyboardShortcuts()
        {
            Event currentEvent = Event.current;
            if (currentEvent == null || currentEvent.type != EventType.KeyDown)
            {
                return;
            }

            if (currentEvent.keyCode == KeyCode.Return || currentEvent.keyCode == KeyCode.KeypadEnter)
            {
                currentEvent.Use();
                CreateAddressablesForSelectedFolder();
            }
            else if (currentEvent.keyCode == KeyCode.Escape)
            {
                currentEvent.Use();
                Close();
            }
        }

        private void CreateAddressablesForSelectedFolder()
        {
            string trimmedGroupName = groupName_?.Trim();
            if (string.IsNullOrEmpty(trimmedGroupName))
            {
                EditorUtility.DisplayDialog(
                    "Group Name Required",
                    "Enter a group name before creating Addressables entries.",
                    "OK");
                return;
            }

            if (!TryGetSelectedFolderPath(out string folderPath))
            {
                EditorUtility.DisplayDialog(
                    "Folder Not Found",
                    "Select a single folder in the Project window and try again.",
                    "OK");
                return;
            }

            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                EditorUtility.DisplayDialog(
                    "Addressables Not Configured",
                    "This project does not have Addressables settings available.",
                    "OK");
                return;
            }

            AddressableAssetGroup group = GetOrCreateGroup(settings, trimmedGroupName);
            if (group == null)
            {
                EditorUtility.DisplayDialog(
                    "Group Creation Failed",
                    $"Could not create or find the Addressables group '{trimmedGroupName}'.",
                    "OK");
                return;
            }

            string[] assetGuids = AssetDatabase.FindAssets(string.Empty, new[] { folderPath });
            List<string> assetPaths = new List<string>(assetGuids.Length);

            foreach (string assetGuid in assetGuids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(assetGuid);
                if (string.IsNullOrEmpty(assetPath) || AssetDatabase.IsValidFolder(assetPath))
                {
                    continue;
                }

                assetPaths.Add(assetPath);
            }

            if (assetPaths.Count == 0)
            {
                EditorUtility.DisplayDialog(
                    "No Assets Found",
                    "The selected folder does not contain any assets to add.",
                    "OK");
                return;
            }

            try
            {
                for (int index = 0; index < assetPaths.Count; index++)
                {
                    string assetPath = assetPaths[index];
                    EditorUtility.DisplayProgressBar(
                        "Creating Addressables Entries",
                        assetPath,
                        (float)(index + 1) / assetPaths.Count);

                    string assetGuid = AssetDatabase.AssetPathToGUID(assetPath);
                    AddressableAssetEntry entry = settings.CreateOrMoveEntry(assetGuid, group, false, false);
                    entry.address = Path.GetFileNameWithoutExtension(assetPath);
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            settings.SetDirty(AddressableAssetSettings.ModificationEvent.BatchModification, null, true, true);
            AssetDatabase.SaveAssets();

            Debug.Log(
                $"[FolderAddressablesGroupWindow] Added {assetPaths.Count} assets from '{folderPath}' to Addressables group '{trimmedGroupName}'.");

            Close();
        }

        private static AddressableAssetGroup GetOrCreateGroup(AddressableAssetSettings settings, string groupName)
        {
            AddressableAssetGroup existingGroup = settings.FindGroup(groupName);
            if (existingGroup != null)
            {
                return existingGroup;
            }

            List<AddressableAssetGroupSchema> schemasToCopy = settings.DefaultGroup != null
                ? settings.DefaultGroup.Schemas
                : null;

            return settings.CreateGroup(groupName, false, false, false, schemasToCopy);
        }

        private static bool TryGetSelectedFolderPath(out string folderPath)
        {
            folderPath = null;

            if (Selection.assetGUIDs == null || Selection.assetGUIDs.Length != 1)
            {
                return false;
            }

            folderPath = AssetDatabase.GUIDToAssetPath(Selection.assetGUIDs[0]);
            return !string.IsNullOrEmpty(folderPath) && AssetDatabase.IsValidFolder(folderPath);
        }
    }
}
