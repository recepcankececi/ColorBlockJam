using System;
using CBJ.App;
using CBJ.UI;
using NUnit.Framework;

namespace CBJ.Tests.EditMode.App
{
    public class SaveServiceTests
    {
        [Test]
        public void Constructor_EmptyStore_StartsAtFirstLevelWithNoCoins()
        {
            var save = new SaveService(new MemoryStore());

            Assert.That((save.LevelIndex, save.Coins), Is.EqualTo((0, 0)));
        }

        [Test]
        public void Constructor_NullStore_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new SaveService(null));
        }

        [Test]
        public void CompleteLevel_AfterRelaunch_NextLevelAndRewardAreKept()
        {
            var store = new MemoryStore();
            new SaveService(store).CompleteLevel(3, 10);

            var relaunched = new SaveService(store.Relaunch());

            Assert.That((relaunched.LevelIndex, relaunched.Coins), Is.EqualTo((3, 10)));
        }

        [Test]
        public void CompleteLevel_TwoWins_RewardsAddUp()
        {
            var save = new SaveService(new MemoryStore());
            save.CompleteLevel(1, 10);

            save.CompleteLevel(2, 15);

            Assert.That(save.Coins, Is.EqualTo(25));
        }

        [Test]
        public void CompleteLevel_CoinsPastIntMax_StopAtIntMax()
        {
            var save = new SaveService(new MemoryStore());
            save.CompleteLevel(1, int.MaxValue);

            save.CompleteLevel(2, 1);

            Assert.That(save.Coins, Is.EqualTo(int.MaxValue));
        }

        [Test]
        public void CompleteLevel_NegativeReward_Throws()
        {
            var save = new SaveService(new MemoryStore());

            Assert.Throws<ArgumentOutOfRangeException>(() => save.CompleteLevel(1, -1));
        }

        [Test]
        public void CompleteLevel_NegativeIndex_Throws()
        {
            var save = new SaveService(new MemoryStore());

            Assert.Throws<ArgumentOutOfRangeException>(() => save.CompleteLevel(-1, 10));
        }

        [Test]
        public void Constructor_CorruptNegativeValues_ReadAsZero()
        {
            var store = new MemoryStore();
            store.Saved["cbj.levelIndex"] = -4;
            store.Saved["cbj.coins"] = -50;

            var save = new SaveService(store);

            Assert.That((save.LevelIndex, save.Coins), Is.EqualTo((0, 0)));
        }

        [Test]
        public void LoadSettings_NothingSaved_KeepsDefaults()
        {
            var settings = new GameSettings();

            new SaveService(new MemoryStore()).LoadSettings(settings);

            Assert.That(settings.IsOn(Setting.Vibration) && settings.IsOn(Setting.Sound) && settings.IsOn(Setting.Music));
        }

        [Test]
        public void SaveSetting_AfterRelaunch_LoadSettingsRestoresIt()
        {
            var store = new MemoryStore();
            new SaveService(store).SaveSetting(Setting.Music, false);
            var settings = new GameSettings();

            new SaveService(store.Relaunch()).LoadSettings(settings);

            Assert.That((settings.IsOn(Setting.Music), settings.IsOn(Setting.Sound)), Is.EqualTo((false, true)));
        }

        [Test]
        public void SaveSetting_ListeningToSettingsChanged_SavesToggles()
        {
            var store = new MemoryStore();
            var settings = new GameSettings();
            settings.Changed += new SaveService(store).SaveSetting;
            settings.Toggle(Setting.Vibration);
            var restored = new GameSettings();

            new SaveService(store.Relaunch()).LoadSettings(restored);

            Assert.That(restored.IsOn(Setting.Vibration), Is.False);
        }

        [Test]
        public void LoadSettings_Null_Throws()
        {
            var save = new SaveService(new MemoryStore());

            Assert.Throws<ArgumentNullException>(() => save.LoadSettings(null));
        }
    }
}
