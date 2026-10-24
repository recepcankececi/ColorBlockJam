using CBJ.UI;
using NUnit.Framework;

namespace CBJ.Tests.EditMode.UI
{
    public class UIRouterTests
    {
        private UIRouter _router;
        private FakeScreen _home;
        private FakeScreen _hud;
        private FakeScreen _settings;
        private FakeScreen _pause;

        [SetUp]
        public void CreateRouter()
        {
            _router = new UIRouter();
            _home = new FakeScreen();
            _hud = new FakeScreen();
            _settings = new FakeScreen();
            _pause = new FakeScreen();
        }

        [Test]
        public void ShowScreen_First_ShowsIt()
        {
            _router.ShowScreen(_home);

            Assert.That(_home.IsVisible, Is.True);
        }

        [Test]
        public void ShowScreen_Another_HidesThePrevious()
        {
            _router.ShowScreen(_home);

            _router.ShowScreen(_hud);

            Assert.That((_home.IsVisible, _hud.IsVisible), Is.EqualTo((false, true)));
        }

        [Test]
        public void ShowScreen_SameAgain_StaysVisible()
        {
            _router.ShowScreen(_home);

            _router.ShowScreen(_home);

            Assert.That(_home.IsVisible, Is.True);
        }

        [Test]
        public void ShowScreen_WithOpenPopups_ClosesThem()
        {
            _router.ShowScreen(_hud);
            _router.OpenPopup(_pause);

            _router.ShowScreen(_home);

            Assert.That(_pause.IsVisible, Is.False);
        }

        [Test]
        public void OpenPopup_KeepsTheScreenBelowVisible()
        {
            _router.ShowScreen(_home);

            _router.OpenPopup(_settings);

            Assert.That((_home.IsVisible, _settings.IsVisible), Is.EqualTo((true, true)));
        }

        [Test]
        public void ClosePopup_TwoOpen_ClosesOnlyTheTopOne()
        {
            _router.OpenPopup(_settings);
            _router.OpenPopup(_pause);

            _router.ClosePopup();

            Assert.That((_pause.IsVisible, _settings.IsVisible), Is.EqualTo((false, true)));
        }

        [Test]
        public void ClosePopup_NoneOpen_DoesNothing()
        {
            _router.ShowScreen(_home);

            _router.ClosePopup();

            Assert.That(_home.IsVisible, Is.True);
        }

        [Test]
        public void OpenPopup_AlreadyOpen_MovesItToTheTop()
        {
            _router.OpenPopup(_settings);
            _router.OpenPopup(_pause);

            _router.OpenPopup(_settings);
            _router.ClosePopup();

            Assert.That((_settings.IsVisible, _pause.IsVisible), Is.EqualTo((false, true)));
        }

        [Test]
        public void CloseAllPopups_HidesEveryPopup()
        {
            _router.OpenPopup(_settings);
            _router.OpenPopup(_pause);

            _router.CloseAllPopups();

            Assert.That((_settings.IsVisible, _pause.IsVisible), Is.EqualTo((false, false)));
        }

        private sealed class FakeScreen : IScreen
        {
            public bool IsVisible { get; private set; }

            public void Show()
            {
                IsVisible = true;
            }

            public void Hide()
            {
                IsVisible = false;
            }
        }
    }
}
