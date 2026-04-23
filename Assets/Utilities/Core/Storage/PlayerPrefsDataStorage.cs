using System.Threading.Tasks;

using UnityEngine;

namespace Flowbit.Utilities.Storage
{
    /// <summary>
    /// Stores arbitrary serializable data locally using PlayerPrefs and JSON.
    /// </summary>
    public sealed class PlayerPrefsDataStorage : IDataStorage
    {
        public Task SaveAsync<T>(string key, T data)
        {
            string json = JsonUtility.ToJson(data);
            PlayerPrefs.SetString(key, json);
            PlayerPrefs.Save();
            return Task.CompletedTask;
        }

        public Task<DataLoadResult<T>> LoadAsync<T>(string key) where T : new()
        {
            if (!PlayerPrefs.HasKey(key))
            {
                return Task.FromResult(new DataLoadResult<T>(false, new T()));
            }

            string json = PlayerPrefs.GetString(key);

            if (string.IsNullOrWhiteSpace(json))
            {
                return Task.FromResult(new DataLoadResult<T>(false, new T()));
            }

            T data = JsonUtility.FromJson<T>(json);

            if (data == null)
            {
                return Task.FromResult(new DataLoadResult<T>(false, new T()));
            }

            return Task.FromResult(new DataLoadResult<T>(true, data));
        }
    }
}
