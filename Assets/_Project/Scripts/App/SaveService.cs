using System;
using CBJ.UI;

namespace CBJ.App
{
    /// <summary>
    /// The player's saved progress: current level, coins and settings. Every change is written through to the
    /// store at once, so quitting at any point keeps it.
    /// </summary>
    public sealed class SaveService
    {
        private const string LevelIndexKey = "cbj.levelIndex";
        private const string CoinsKey = "cbj.coins";
        private const string SettingKeyPrefix = "cbj.setting.";
        private const int On = 1;
        private const int Off = 0;

        private readonly IKeyValueStore _store;

        /// <summary>Reads the saved progress from <paramref name="store"/>; values that cannot be valid read as zero.</summary>
        public SaveService(IKeyValueStore store)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            LevelIndex = Math.Max(0, store.GetInt(LevelIndexKey, 0));
            Coins = Math.Max(0, store.GetInt(CoinsKey, 0));
        }

        /// <summary>Catalog position of the level the player plays next.</summary>
        public int LevelIndex { get; private set; }

        /// <summary>Coins the player owns; the balance stops at <see cref="int.MaxValue"/>.</summary>
        public int Coins { get; private set; }

        /// <summary>
        /// Records a won level in one write: adds <paramref name="reward"/> coins and saves
        /// <paramref name="nextIndex"/>, so a quit can never keep the reward without the progress.
        /// </summary>
        public void CompleteLevel(int nextIndex, int reward)
        {
            if (nextIndex < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(nextIndex), nextIndex, "A level index cannot be negative.");
            }

            if (reward < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(reward), reward, "Coins can only be added.");
            }

            LevelIndex = nextIndex;
            Coins = (int)Math.Min(int.MaxValue, (long)Coins + reward);
            _store.SetInt(LevelIndexKey, LevelIndex);
            _store.SetInt(CoinsKey, Coins);
            _store.Save();
        }

        /// <summary>Applies the saved settings to <paramref name="settings"/>; a setting never saved keeps its value.</summary>
        public void LoadSettings(GameSettings settings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            foreach (Setting setting in Enum.GetValues(typeof(Setting)))
            {
                int fallback = settings.IsOn(setting) ? On : Off;
                settings.Set(setting, _store.GetInt(SettingKey(setting), fallback) != Off);
            }
        }

        /// <summary>Saves one setting; matches <see cref="GameSettings.Changed"/> so it can listen to it directly.</summary>
        public void SaveSetting(Setting setting, bool on)
        {
            Write(SettingKey(setting), on ? On : Off);
        }

        private static string SettingKey(Setting setting)
        {
            return SettingKeyPrefix + setting;
        }

        private void Write(string key, int value)
        {
            _store.SetInt(key, value);
            _store.Save();
        }
    }
}
