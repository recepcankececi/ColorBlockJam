using UnityEngine;

namespace CBJ.UI
{
    /// <summary>
    /// <see cref="IScreen"/> on a GameObject: showing activates it, hiding deactivates it, so a hidden screen runs
    /// nothing and listens to nothing.
    /// </summary>
    public abstract class ScreenView : MonoBehaviour, IScreen
    {
        /// <inheritdoc />
        public void Show()
        {
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
        }

        /// <inheritdoc />
        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
