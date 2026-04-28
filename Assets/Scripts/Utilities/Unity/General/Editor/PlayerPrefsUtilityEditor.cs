using UnityEditor;

using Flowbit.Utilities.Unity.General;

namespace Flowbit.Utilities.Unity.Editor
{
    /// <summary>
    /// Editor menu tools for clearing local PlayerPrefs data.
    /// </summary>
    public static class PlayerPrefsUtilityEditor
    {
        private const string ClearMenuPath = "Tools/Local Data/Clear PlayerPrefs";

        [MenuItem(ClearMenuPath)]
        private static void ClearPlayerPrefs()
        {
            bool confirmed = EditorUtility.DisplayDialog(
                "Clear Local Data",
                "Delete all local PlayerPrefs data for this project?",
                "Delete All",
                "Cancel");

            if (!confirmed)
            {
                return;
            }

            PlayerPrefsUtility.DeleteAll();
            UnityEngine.Debug.Log("[Editor] Cleared local PlayerPrefs data.");
        }
    }
}
