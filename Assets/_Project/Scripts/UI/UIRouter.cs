using System;
using System.Collections.Generic;

namespace CBJ.UI
{
    /// <summary>
    /// Keeps one full screen visible with a stack of popups above it. Showing another screen closes every popup.
    /// </summary>
    public sealed class UIRouter
    {
        private readonly List<IScreen> _popups = new List<IScreen>();
        private IScreen _current;

        /// <summary>Closes all popups and replaces the current screen with <paramref name="screen"/>.</summary>
        public void ShowScreen(IScreen screen)
        {
            if (screen == null)
            {
                throw new ArgumentNullException(nameof(screen));
            }

            CloseAllPopups();
            if (_current == screen)
            {
                return;
            }

            _current?.Hide();
            _current = screen;
            screen.Show();
        }

        /// <summary>Opens <paramref name="popup"/> above everything shown; an open popup is moved to the top.</summary>
        public void OpenPopup(IScreen popup)
        {
            if (popup == null)
            {
                throw new ArgumentNullException(nameof(popup));
            }

            _popups.Remove(popup);
            _popups.Add(popup);
            popup.Show();
        }

        /// <summary>Closes the top popup, if any.</summary>
        public void ClosePopup()
        {
            if (_popups.Count == 0)
            {
                return;
            }

            IScreen top = _popups[_popups.Count - 1];
            _popups.RemoveAt(_popups.Count - 1);
            top.Hide();
        }

        /// <summary>Closes every open popup, top first.</summary>
        public void CloseAllPopups()
        {
            while (_popups.Count > 0)
            {
                ClosePopup();
            }
        }
    }
}
