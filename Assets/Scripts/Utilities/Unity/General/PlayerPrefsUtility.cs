using UnityEngine;

namespace Flowbit.Utilities.Unity.General
{
    /// <summary>
    /// Utility helpers for clearing local Unity player preferences.
    /// </summary>
    public static class PlayerPrefsUtility
    {
        /// <summary>
        /// Deletes all PlayerPrefs data and forces it to be saved immediately.
        /// </summary>
        public static void DeleteAll()
        {
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
        }
    }
}
