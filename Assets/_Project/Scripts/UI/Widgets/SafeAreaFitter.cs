using UnityEngine;

namespace CBJ.UI
{
    /// <summary>
    /// Keeps its RectTransform inside <see cref="Screen.safeArea"/>, so buttons and labels stay clear of notches and
    /// the gesture bar. Backgrounds sit outside it and still fill the screen.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeAreaFitter : MonoBehaviour
    {
        private RectTransform _rect;
        private Rect _applied;
        private Vector2Int _appliedScreen;

        /// <summary>Anchors that cover <paramref name="safeArea"/> on a screen of <paramref name="screenSize"/> pixels.</summary>
        public static void Anchors(Rect safeArea, Vector2 screenSize, out Vector2 min, out Vector2 max)
        {
            min = new Vector2(safeArea.xMin / screenSize.x, safeArea.yMin / screenSize.y);
            max = new Vector2(safeArea.xMax / screenSize.x, safeArea.yMax / screenSize.y);
        }

        private void Awake()
        {
            _rect = (RectTransform)transform;
        }

        private void OnEnable()
        {
            Apply();
        }

        private void LateUpdate()
        {
            if (Screen.safeArea != _applied || Screen.width != _appliedScreen.x || Screen.height != _appliedScreen.y)
            {
                Apply();
            }
        }

        private void Apply()
        {
            _applied = Screen.safeArea;
            _appliedScreen = new Vector2Int(Screen.width, Screen.height);
            Anchors(_applied, _appliedScreen, out Vector2 min, out Vector2 max);
            _rect.anchorMin = min;
            _rect.anchorMax = max;
            _rect.offsetMin = Vector2.zero;
            _rect.offsetMax = Vector2.zero;
        }
    }
}
