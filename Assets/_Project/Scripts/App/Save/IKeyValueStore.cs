namespace CBJ.App
{
    /// <summary>
    /// Durable integer storage the save data is written to.
    /// </summary>
    public interface IKeyValueStore
    {
        /// <summary>The value stored under <paramref name="key"/>, or <paramref name="fallback"/> when there is none.</summary>
        int GetInt(string key, int fallback);

        /// <summary>Stores <paramref name="value"/> under <paramref name="key"/>.</summary>
        void SetInt(string key, int value);

        /// <summary>Writes pending values to disk.</summary>
        void Save();
    }
}
