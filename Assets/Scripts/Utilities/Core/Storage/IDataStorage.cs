using System.Threading.Tasks;

namespace Flowbit.Utilities.Storage
{
    /// <summary>
    /// Persists arbitrary data by key without exposing the backing store.
    /// </summary>
    public interface IDataStorage
    {
        /// <summary>
        /// Saves the given value under the provided key.
        /// </summary>
        Task SaveAsync<T>(string key, T data);

        /// <summary>
        /// Loads the value stored under the provided key.
        /// Returns false when no value exists.
        /// </summary>
        Task<DataLoadResult<T>> LoadAsync<T>(string key) where T : new();
    }
}
