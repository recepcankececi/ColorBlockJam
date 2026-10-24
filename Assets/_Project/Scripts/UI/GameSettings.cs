using System;

namespace CBJ.UI
{
    /// <summary>
    /// The player's on/off settings. Every setting starts on; views follow <see cref="Changed"/>.
    /// </summary>
    public sealed class GameSettings
    {
        private static readonly int Count = Enum.GetValues(typeof(Setting)).Length;

        private readonly bool[] _values;

        /// <summary>Creates settings with everything switched on.</summary>
        public GameSettings()
        {
            _values = new bool[Count];
            for (int i = 0; i < Count; i++)
            {
                _values[i] = true;
            }
        }

        /// <summary>Raised with the setting and its new value whenever a value actually changes.</summary>
        public event Action<Setting, bool> Changed;

        /// <summary>True when <paramref name="setting"/> is switched on.</summary>
        public bool IsOn(Setting setting)
        {
            return _values[Index(setting)];
        }

        /// <summary>Switches <paramref name="setting"/> on or off.</summary>
        public void Set(Setting setting, bool on)
        {
            int index = Index(setting);
            if (_values[index] == on)
            {
                return;
            }

            _values[index] = on;
            Changed?.Invoke(setting, on);
        }

        /// <summary>Flips <paramref name="setting"/>.</summary>
        public void Toggle(Setting setting)
        {
            Set(setting, !IsOn(setting));
        }

        private static int Index(Setting setting)
        {
            int index = (int)setting;
            if (index < 0 || index >= Count)
            {
                throw new ArgumentOutOfRangeException(nameof(setting), setting, "Unknown setting.");
            }

            return index;
        }
    }
}
