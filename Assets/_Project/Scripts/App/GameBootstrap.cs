using System.Collections;
using CBJ.Data;
using CBJ.Gameplay;
using CBJ.UI;
using UnityEngine;

namespace CBJ.App
{
    /// <summary>
    /// Composition root (ADR-006) and level flow: hands the assets to the scene systems, loads the save, opens home
    /// (or the level the editor asked for), starts the current level on request and, after a win, pays the coin
    /// reward, saves the next level and moves on to it.
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

        private SaveService _save;
        private GameSettings _settings;
        private int _levelIndex;
        private bool _isEditorTest;
        private WaitForSeconds _levelEndWait;
        private Coroutine _levelEnd;

        private void Awake()
        {
            _boardView.Initialize(_skin, _palette, _config);
            _boardCamera.Initialize(_config);
            _dragInput.Initialize(new UIBlockedPointerSource(new LegacyPointerSource(), _uiHitTest),
                _boardCamera.Camera, _config);
            _levelPlayer.Initialize(_boardView, _boardCamera, _dragInput);
            _save = new SaveService(new PlayerPrefsStore());
            _settings = new GameSettings();
            _save.LoadSettings(_settings);
            _screens.Initialize(_settings);
            _levelIndex = LevelOrder.Resume(_save.LevelIndex, _catalog.Count);
            _levelEndWait = new WaitForSeconds(_config.LevelEndDelay);
        }

        private void OnEnable()
        {
            _levelPlayer.AttemptStarted += CancelLevelEnd;
            _levelPlayer.Won += OnLevelWon;
            _screens.PlayRequested += OnPlayRequested;
            _screens.HomeRequested += OnHomeRequested;
            _settings.Changed += _save.SaveSetting;
        }

        private void OnDisable()
        {
            _levelPlayer.AttemptStarted -= CancelLevelEnd;
            _levelPlayer.Won -= OnLevelWon;
            _screens.PlayRequested -= OnPlayRequested;
            _screens.HomeRequested -= OnHomeRequested;
            _settings.Changed -= _save.SaveSetting;
        }

        private void Start()
        {
#if UNITY_EDITOR
            // Consumed on every boot so a request left by a failed Play entry cannot leak into a later session.
            if (EditorPlayRequest.TryConsume(out int requested))
            {
                if (requested >= 0 && requested < _catalog.Count)
                {
                    _isEditorTest = true;
                    PlayLevel(requested);
                    return;
                }

                Debug.LogWarning($"Level editor asked for catalog level {requested}, which does not exist.", this);
            }
#endif
            _screens.ShowHome(_levelIndex, _save.Coins);
        }

        private void PlayLevel(int index)
        {
            if (_catalog.Count == 0)
            {
                Debug.LogError("The level catalog is empty. Create a level in Tools > Color Block Jam > Level Editor.", _catalog);
                return;
            }

            _levelIndex = index;
            LevelDefinition level = _catalog.Get(_levelIndex);
            if (level == null)
            {
                Debug.LogError($"Catalog entry {_levelIndex} has no level asset.", _catalog);
                _levelPlayer.Stop();
                _screens.ShowHome(_levelIndex, _save.Coins);
                return;
            }

            _screens.ShowGameplay(_levelIndex);
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
            _screens.ShowHome(_levelIndex, _save.Coins);
        }

        // Reward and progress are saved at the win itself, so quitting during the level-end pause loses neither. A
        // designer testing a level from the editor must not move the player's saved progress or coins.
        private void OnLevelWon()
        {
            CancelLevelEnd();
            _levelIndex = LevelOrder.Next(_levelIndex, _catalog.Count);
            if (!_isEditorTest)
            {
                _save.CompleteLevel(_levelIndex, _config.CoinsPerWin);
            }

            _levelEnd = StartCoroutine(PlayLevelAfterDelay(_levelIndex));
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
