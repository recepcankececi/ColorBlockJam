using CBJ.UI;
using NUnit.Framework;

namespace CBJ.Tests.EditMode.UI
{
    public class GameSettingsTests
    {
        [Test]
        public void IsOn_NewSettings_EverythingIsOn()
        {
            var settings = new GameSettings();

            Assert.That(settings.IsOn(Setting.Vibration) && settings.IsOn(Setting.Sound) && settings.IsOn(Setting.Music));
        }

        [Test]
        public void Toggle_OnSetting_TurnsItOffAndLeavesTheOthers()
        {
            var settings = new GameSettings();

            settings.Toggle(Setting.Sound);

            Assert.That((settings.IsOn(Setting.Sound), settings.IsOn(Setting.Music)), Is.EqualTo((false, true)));
        }

        [Test]
        public void Toggle_RaisesChangedWithTheNewValue()
        {
            var settings = new GameSettings();
            Setting changed = Setting.Vibration;
            bool value = true;
            settings.Changed += (setting, on) =>
            {
                changed = setting;
                value = on;
            };

            settings.Toggle(Setting.Music);

            Assert.That((changed, value), Is.EqualTo((Setting.Music, false)));
        }

        [Test]
        public void Set_SameValue_DoesNotRaiseChanged()
        {
            var settings = new GameSettings();
            int raised = 0;
            settings.Changed += (setting, on) => raised++;

            settings.Set(Setting.Sound, true);

            Assert.That(raised, Is.Zero);
        }
    }
}
