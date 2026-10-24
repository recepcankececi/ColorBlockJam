using System.Collections;
using CBJ.Data;
using CBJ.Gameplay;
using UnityEngine;

namespace CBJ.App
{
    /// <summary>
    /// Composition root (ADR-006): hands the assets to the scene systems, starts the first level and moves on to the
    /// next level after a win. A failed attempt restarts the same level.
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

        private int _levelIndex;
        private WaitForSeconds _levelEndWait;
        private Coroutine _levelEnd;

        private void Awake()
        {
            _boardView.Initialize(_skin, _palette, _config);
            _boardCamera.Initialize(_config);
            _dragInput.Initialize(new LegacyPointerSource(), _boardCamera.Camera, _config);
            _levelPlayer.Initialize(_boardView, _boardCamera, _dragInput);
            _levelEndWait = new WaitForSeconds(_config.LevelEndDelay);
        }

        private void OnEnable()
        {
            _levelPlayer.AttemptStarted += CancelLevelEnd;
            _levelPlayer.Won += OnLevelWon;
            _levelPlayer.Failed += OnLevelFailed;
        }

        private void OnDisable()
        {
            _levelPlayer.AttemptStarted -= CancelLevelEnd;
            _levelPlayer.Won -= OnLevelWon;
            _levelPlayer.Failed -= OnLevelFailed;
        }

        private void Start()
        {
            int startIndex = 0;
#if UNITY_EDITOR
            // Consumed on every boot so a request left by a failed Play entry cannot leak into a later session.
            if (EditorPlayRequest.TryConsume(out int requested))
            {
                if (requested >= 0 && requested < _catalog.Count)
                {
                    startIndex = requested;
                }
                else
                {
                    Debug.LogWarning($"Level editor asked for catalog level {requested}, which does not exist; starting level 1.", this);
                }
            }
#endif
            PlayLevel(startIndex);
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
                return;
            }

            _levelPlayer.Play(level.Level);
        }

        private void OnLevelWon()
        {
            ScheduleLevelEnd(PlayLevelAfterDelay(_levelIndex + 1));
        }

        private void OnLevelFailed()
        {
            ScheduleLevelEnd(RestartAfterDelay());
        }

        private void ScheduleLevelEnd(IEnumerator routine)
        {
            CancelLevelEnd();
            _levelEnd = StartCoroutine(routine);
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

        private IEnumerator RestartAfterDelay()
        {
            yield return _levelEndWait;
            _levelPlayer.Restart();
        }
    }
}
