using UnityEngine;

namespace CBJ.App
{
    /// <summary>
    /// <see cref="IKeyValueStore"/> backed by <see cref="PlayerPrefs"/>.
    /// </summary>
    public sealed class PlayerPrefsStore : IKeyValueStore
    {
        /// <inheritdoc />
        public int GetInt(string key, int fallback)
        {
            return PlayerPrefs.GetInt(key, fallback);
        }

        /// <inheritdoc />
        public void SetInt(string key, int value)
        {
            PlayerPrefs.SetInt(key, value);
        }

        /// <inheritdoc />
        public void Save()
        {
            PlayerPrefs.Save();
        }
    }
}
