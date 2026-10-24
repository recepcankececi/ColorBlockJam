using CBJ.Gameplay;
using CBJ.UI;
using NUnit.Framework;
using UnityEngine;

namespace CBJ.Tests.EditMode.UI
{
    public class UIBlockedPointerSourceTests
    {
        private FakePointer _pointer;
        private FakeHitTest _hitTest;
        private UIBlockedPointerSource _source;

        [SetUp]
        public void CreateSource()
        {
            _pointer = new FakePointer();
            _hitTest = new FakeHitTest();
            _source = new UIBlockedPointerSource(_pointer, _hitTest);
        }

        [Test]
        public void IsPressed_PressStartsOffUI_IsPressed()
        {
            _pointer.IsPressed = true;

            Assert.That(_source.IsPressed, Is.True);
        }

        [Test]
        public void IsPressed_PressStartsOnUI_IsNotPressed()
        {
            _hitTest.OverUI = true;
            _pointer.IsPressed = true;

            Assert.That(_source.IsPressed, Is.False);
        }

        [Test]
        public void IsPressed_PressStartedOnUIThenLeavesIt_StaysBlocked()
        {
            _hitTest.OverUI = true;
            _pointer.IsPressed = true;
            _ = _source.IsPressed;

            _hitTest.OverUI = false;

            Assert.That(_source.IsPressed, Is.False);
        }

        [Test]
        public void IsPressed_DragMovesOntoUI_StaysPressed()
        {
            _pointer.IsPressed = true;
            _ = _source.IsPressed;

            _hitTest.OverUI = true;

            Assert.That(_source.IsPressed, Is.True);
        }

        [Test]
        public void IsPressed_NewPressAfterBlockedOne_IsCheckedAgain()
        {
            _hitTest.OverUI = true;
            _pointer.IsPressed = true;
            _ = _source.IsPressed;
            _pointer.IsPressed = false;
            _ = _source.IsPressed;

            _hitTest.OverUI = false;
            _pointer.IsPressed = true;

            Assert.That(_source.IsPressed, Is.True);
        }

        private sealed class FakePointer : IPointerSource
        {
            public bool IsPressed { get; set; }

            public Vector2 Position { get; set; }
        }

        private sealed class FakeHitTest : IUIHitTest
        {
            public bool OverUI { get; set; }

            public bool IsOverUI(Vector2 screenPosition)
            {
                return OverUI;
            }
        }
    }
}
