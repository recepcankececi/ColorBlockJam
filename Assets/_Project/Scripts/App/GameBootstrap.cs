using System.Collections;
using CBJ.Data;
using CBJ.Gameplay;
using CBJ.UI;
using UnityEngine;

namespace CBJ.App
{
    /// <summary>
    /// Composition root (ADR-006) and level flow: hands the assets to the scene systems, opens home (or the level the
    /// editor asked for), starts the current level on request and moves on to the next level after a win.
    /// </summary>
    public sealed class GameBootstrap : MonoBehaviour
    {
        [Header("Assets")]
        [SerializeField] private LevelCatalog _catalog;
        [SerializeField] private BlockPalette _palette;
        [SerializeField] private GameConfig _config;
        [SerializeField] private BoardSkin _skin;

        [Header("Scene")]
        [SerializeField] private LevelPlayer _levelPlayer;
        [SerializeField] private BoardView _boardView;
        [SerializeField] private BoardCamera _boardCamera;
        [SerializeField] private DragInput _dragInput;
        [SerializeField] private ScreenFlow _screens;
        [SerializeField] private EventSystemHitTest _uiHitTest;

        private int _levelIndex;
        private WaitForSeconds _levelEndWait;
        private Coroutine _levelEnd;

        private void Awake()
        {
            _boardView.Initialize(_skin, _palette, _config);
            _boardCamera.Initialize(_config);
            _dragInput.Initialize(new UIBlockedPointerSource(new LegacyPointerSource(), _uiHitTest),
                _boardCamera.Camera, _config);
            _levelPlayer.Initialize(_boardView, _boardCamera, _dragInput);
            _screens.Initialize(new GameSettings());
            _levelEndWait = new WaitForSeconds(_config.LevelEndDelay);
        }

        private void OnEnable()
        {
            _levelPlayer.AttemptStarted += CancelLevelEnd;
            _levelPlayer.Won += OnLevelWon;
            _screens.PlayRequested += OnPlayRequested;
            _screens.HomeRequested += OnHomeRequested;
        }

        private void OnDisable()
        {
            _levelPlayer.AttemptStarted -= CancelLevelEnd;
            _levelPlayer.Won -= OnLevelWon;
            _screens.PlayRequested -= OnPlayRequested;
            _screens.HomeRequested -= OnHomeRequested;
        }

        private void Start()
        {
#if UNITY_EDITOR
            // Consumed on every boot so a request left by a failed Play entry cannot leak into a later session.
            if (EditorPlayRequest.TryConsume(out int requested))
            {
                if (requested >= 0 && requested < _catalog.Count)
                {
                    PlayLevel(requested);
                    return;
                }

                Debug.LogWarning($"Level editor asked for catalog level {requested}, which does not exist.", this);
            }
#endif
            _screens.ShowHome(_levelIndex + 1);
        }

        private void PlayLevel(int index)
        {
            if (_catalog.Count == 0)
            {
                Debug.LogError("The level catalog is empty. Create a level in Tools > Color Block Jam > Level Editor.", _catalog);
                return;
            }

            _levelIndex = index % _catalog.Count;
            LevelDefinition level = _catalog.Get(_levelIndex);
            if (level == null)
            {
                Debug.LogError($"Catalog entry {_levelIndex} has no level asset.", _catalog);
                _levelPlayer.Stop();
                _screens.ShowHome(_levelIndex + 1);
                return;
            }

            _screens.ShowGameplay(_levelIndex + 1);
            _levelPlayer.Play(level.Level);
        }

        private void OnPlayRequested()
        {
            PlayLevel(_levelIndex);
        }

        private void OnHomeRequested()
        {
            CancelLevelEnd();
            _levelPlayer.Stop();
            _screens.ShowHome(_levelIndex + 1);
        }

        private void OnLevelWon()
        {
            CancelLevelEnd();
            _levelEnd = StartCoroutine(PlayLevelAfterDelay(_levelIndex + 1));
        }

        // Any new attempt, however it was started, makes a pending level-end step stale.
        private void CancelLevelEnd()
        {
            if (_levelEnd != null)
            {
                StopCoroutine(_levelEnd);
                _levelEnd = null;
            }
        }

        private IEnumerator PlayLevelAfterDelay(int index)
        {
            yield return _levelEndWait;
            PlayLevel(index);
        }
    }
}
