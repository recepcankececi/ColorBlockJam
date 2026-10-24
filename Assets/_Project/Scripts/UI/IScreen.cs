namespace CBJ.UI
{
    /// <summary>
    /// Anything <see cref="UIRouter"/> can show: a full screen or a popup.
    /// </summary>
    public interface IScreen
    {
        /// <summary>Makes the screen visible and interactive.</summary>
        void Show();

        /// <summary>Hides the screen.</summary>
        void Hide();
    }
}
