namespace Flowbit.Utilities.Storage
{
    /// <summary>
    /// Represents the result of loading a value from storage.
    /// </summary>
    public readonly struct DataLoadResult<T>
    {
        public DataLoadResult(bool found, T data)
        {
            Found = found;
            Data = data;
        }

        /// <summary>
        /// Gets whether a value was found in storage.
        /// </summary>
        public bool Found { get; }

        /// <summary>
        /// Gets the loaded value, or the default constructed value when not found.
        /// </summary>
        public T Data { get; }
    }
}
