using Game.Unity.Settings;

using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Unity.Editor
{
    /// <summary>
    /// Captures the current dress rect offsets from the active scene into RoomSettings.
    /// </summary>
    public static class DressLayoutCaptureEditor
    {
        private const string CaptureMenuPath = "GameObject/Dress/Capture Layout To Room Settings";
        private const string ApplyMenuPath = "GameObject/Dress/Apply Layout From Room Settings";
        private const string RoomSettingsResourcePath = "RoomSettings";
        private const string DressObjectName = "Dress";
        private const string DressSpritePrefix = "dress_";
        private const string DressLayoutOverridesPropertyName = "dressLayoutOverrides_";

        [MenuItem(CaptureMenuPath, false, 30)]
        private static void CaptureDressLayout()
        {
            RoomSettings roomSettings = Resources.Load<RoomSettings>(RoomSettingsResourcePath);
            if (roomSettings == null)
            {
                EditorUtility.DisplayDialog(
                    "Room Settings Missing",
                    $"Could not load RoomSettings from Resources/{RoomSettingsResourcePath}.",
                    "OK");
                return;
            }

            if (!TryGetSelectedDressObject(out GameObject dressObject))
            {
                EditorUtility.DisplayDialog(
                    "Dress Object Missing",
                    $"Select a GameObject named '{DressObjectName}' in the hierarchy.",
                    "OK");
                return;
            }

            if (!TryGetDressId(dressObject, out int dressId, out string spriteName))
            {
                EditorUtility.DisplayDialog(
                    "Dress Sprite Invalid",
                    "The Dress object needs an Image or SpriteRenderer with a sprite named like dress_2.",
                    "OK");
                return;
            }

            RectTransform rectTransform = dressObject.GetComponent<RectTransform>();
            if (rectTransform == null)
            {
                EditorUtility.DisplayDialog(
                    "RectTransform Missing",
                    "The Dress object must have a RectTransform.",
                    "OK");
                return;
            }

            DressLayoutDefinition layout = new DressLayoutDefinition
            {
                Left = rectTransform.offsetMin.x,
                Bottom = rectTransform.offsetMin.y,
                Right = -rectTransform.offsetMax.x,
                Top = -rectTransform.offsetMax.y
            };

            Undo.RecordObject(roomSettings, "Capture Dress Layout");
            bool updatedExisting = AddOrUpdateDressLayoutOverride(roomSettings, dressId, layout);
            EditorUtility.SetDirty(roomSettings);
            AssetDatabase.SaveAssets();

            Selection.activeObject = roomSettings;
            EditorGUIUtility.PingObject(roomSettings);

            Debug.Log(
                $"[DressLayoutCaptureEditor] {(updatedExisting ? "Updated" : "Added")} layout for dress id {dressId} from sprite '{spriteName}'.");
        }

        [MenuItem(CaptureMenuPath, true)]
        private static bool ValidateCaptureDressLayout()
        {
            return TryGetSelectedDressObject(out _);
        }

        [MenuItem(ApplyMenuPath, false, 31)]
        private static void ApplyDressLayout()
        {
            RoomSettings roomSettings = Resources.Load<RoomSettings>(RoomSettingsResourcePath);
            if (roomSettings == null)
            {
                EditorUtility.DisplayDialog(
                    "Room Settings Missing",
                    $"Could not load RoomSettings from Resources/{RoomSettingsResourcePath}.",
                    "OK");
                return;
            }

            if (!TryGetSelectedDressObject(out GameObject dressObject))
            {
                EditorUtility.DisplayDialog(
                    "Dress Object Missing",
                    $"Select a GameObject named '{DressObjectName}' in the hierarchy.",
                    "OK");
                return;
            }

            if (!TryGetDressId(dressObject, out int dressId, out string spriteName))
            {
                EditorUtility.DisplayDialog(
                    "Dress Sprite Invalid",
                    "The Dress object needs an Image or SpriteRenderer with a sprite named like dress_2.",
                    "OK");
                return;
            }

            RectTransform rectTransform = dressObject.GetComponent<RectTransform>();
            if (rectTransform == null)
            {
                EditorUtility.DisplayDialog(
                    "RectTransform Missing",
                    "The Dress object must have a RectTransform.",
                    "OK");
                return;
            }

            DressLayoutDefinition layout = roomSettings.ResolveDressLayout(dressId);

            Undo.RecordObject(rectTransform, "Apply Dress Layout");
            rectTransform.offsetMin = new Vector2(layout.Left, layout.Bottom);
            rectTransform.offsetMax = new Vector2(-layout.Right, -layout.Top);
            EditorUtility.SetDirty(rectTransform);

            Debug.Log(
                $"[DressLayoutCaptureEditor] Applied layout to dress id {dressId} from sprite '{spriteName}'.");
        }

        [MenuItem(ApplyMenuPath, true)]
        private static bool ValidateApplyDressLayout()
        {
            return TryGetSelectedDressObject(out _);
        }

        private static bool AddOrUpdateDressLayoutOverride(
            RoomSettings roomSettings,
            int dressId,
            DressLayoutDefinition layout)
        {
            SerializedObject serializedObject = new SerializedObject(roomSettings);
            SerializedProperty overridesProperty =
                serializedObject.FindProperty(DressLayoutOverridesPropertyName);

            if (overridesProperty == null || !overridesProperty.isArray)
            {
                roomSettings.SetDressLayoutOverride(dressId, layout);
                return false;
            }

            for (int index = 0; index < overridesProperty.arraySize; index++)
            {
                SerializedProperty elementProperty = overridesProperty.GetArrayElementAtIndex(index);
                SerializedProperty itemIdProperty = elementProperty.FindPropertyRelative("ItemId");
                if (itemIdProperty != null && itemIdProperty.intValue == dressId)
                {
                    ApplyLayoutToProperty(elementProperty.FindPropertyRelative("Layout"), layout);
                    serializedObject.ApplyModifiedPropertiesWithoutUndo();
                    return true;
                }
            }

            int newIndex = overridesProperty.arraySize;
            overridesProperty.InsertArrayElementAtIndex(newIndex);
            SerializedProperty newElementProperty = overridesProperty.GetArrayElementAtIndex(newIndex);
            SerializedProperty newItemIdProperty = newElementProperty.FindPropertyRelative("ItemId");
            if (newItemIdProperty != null)
            {
                newItemIdProperty.intValue = dressId;
            }

            ApplyLayoutToProperty(newElementProperty.FindPropertyRelative("Layout"), layout);
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            return false;
        }

        private static void ApplyLayoutToProperty(SerializedProperty layoutProperty, DressLayoutDefinition layout)
        {
            if (layoutProperty == null)
            {
                return;
            }

            SerializedProperty leftProperty = layoutProperty.FindPropertyRelative("Left");
            SerializedProperty topProperty = layoutProperty.FindPropertyRelative("Top");
            SerializedProperty rightProperty = layoutProperty.FindPropertyRelative("Right");
            SerializedProperty bottomProperty = layoutProperty.FindPropertyRelative("Bottom");

            if (leftProperty != null)
            {
                leftProperty.floatValue = layout.Left;
            }

            if (topProperty != null)
            {
                topProperty.floatValue = layout.Top;
            }

            if (rightProperty != null)
            {
                rightProperty.floatValue = layout.Right;
            }

            if (bottomProperty != null)
            {
                bottomProperty.floatValue = layout.Bottom;
            }
        }

        private static bool TryGetSelectedDressObject(out GameObject dressObject)
        {
            dressObject = Selection.activeGameObject;
            if (dressObject == null)
            {
                return false;
            }

            return dressObject.name == DressObjectName;
        }

        private static bool TryGetDressId(GameObject dressObject, out int dressId, out string spriteName)
        {
            dressId = 0;
            spriteName = null;

            if (dressObject == null)
            {
                return false;
            }

            Image image = dressObject.GetComponent<Image>();
            if (image != null && image.sprite != null)
            {
                spriteName = image.sprite.name;
                return TryParseDressId(spriteName, out dressId);
            }

            SpriteRenderer spriteRenderer = dressObject.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null && spriteRenderer.sprite != null)
            {
                spriteName = spriteRenderer.sprite.name;
                return TryParseDressId(spriteName, out dressId);
            }

            return false;
        }

        private static bool TryParseDressId(string spriteName, out int dressId)
        {
            dressId = 0;

            if (string.IsNullOrEmpty(spriteName) || !spriteName.StartsWith(DressSpritePrefix))
            {
                return false;
            }

            string suffix = spriteName.Substring(DressSpritePrefix.Length);
            return int.TryParse(suffix, out dressId);
        }
    }
}
