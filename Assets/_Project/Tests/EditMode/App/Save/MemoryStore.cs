using System.Collections.Generic;
using CBJ.App;

namespace CBJ.Tests.EditMode.App
{
    public sealed class MemoryStore : IKeyValueStore
    {
        private readonly Dictionary<string, int> _pending = new Dictionary<string, int>();

        public Dictionary<string, int> Saved { get; } = new Dictionary<string, int>();

        public int GetInt(string key, int fallback)
        {
            if (_pending.TryGetValue(key, out int pending))
            {
                return pending;
            }

            return Saved.TryGetValue(key, out int saved) ? saved : fallback;
        }

        public void SetInt(string key, int value)
        {
            _pending[key] = value;
        }

        public void Save()
        {
            foreach (KeyValuePair<string, int> entry in _pending)
            {
                Saved[entry.Key] = entry.Value;
            }

            _pending.Clear();
        }

        // What a fresh app launch sees: only values that reached Save().
        public MemoryStore Relaunch()
        {
            var relaunched = new MemoryStore();
            foreach (KeyValuePair<string, int> entry in Saved)
            {
                relaunched.Saved[entry.Key] = entry.Value;
            }

            return relaunched;
        }
    }
}
