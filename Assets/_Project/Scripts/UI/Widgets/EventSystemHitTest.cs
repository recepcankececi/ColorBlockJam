using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace CBJ.UI
{
    /// <summary>
    /// <see cref="IUIHitTest"/> that raycasts the scene's UI through its <see cref="EventSystem"/>. Raycasting
    /// directly (rather than <c>IsPointerOverGameObject</c>) gives the right answer on the frame a touch begins.
    /// Every raycast target counts, so decoration over the board must keep <c>raycastTarget</c> off.
    /// </summary>
    [RequireComponent(typeof(EventSystem))]
    public sealed class EventSystemHitTest : MonoBehaviour, IUIHitTest
    {
        private readonly List<RaycastResult> _results = new List<RaycastResult>();
        private EventSystem _eventSystem;
        private PointerEventData _pointer;

        /// <inheritdoc />
        public bool IsOverUI(Vector2 screenPosition)
        {
            _pointer.position = screenPosition;
            _results.Clear();
            _eventSystem.RaycastAll(_pointer, _results);
            return _results.Count > 0;
        }

        private void Awake()
        {
            _eventSystem = GetComponent<EventSystem>();
            _pointer = new PointerEventData(_eventSystem);
        }
    }
}
