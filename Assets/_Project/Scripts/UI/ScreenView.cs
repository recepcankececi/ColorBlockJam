using UnityEngine;

namespace CBJ.UI
{
    /// <summary>
    /// <see cref="IScreen"/> on a GameObject: showing activates it (and plays its entrance, when it has one), hiding
    /// deactivates it, so a hidden screen runs nothing and listens to nothing.
    /// </summary>
    public abstract class ScreenView : MonoBehaviour, IScreen
    {
        [SerializeField] private ScreenTransition _transition;

        /// <inheritdoc />
        public void Show()
        {
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            if (_transition != null)
            {
                _transition.Play();
            }
        }

        /// <inheritdoc />
        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
