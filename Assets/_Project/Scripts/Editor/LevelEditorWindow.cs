using CBJ.Core;
using CBJ.Data;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CBJ.Editor
{
    /// <summary>
    /// Level editor for designers (ADR-004): pick or create a level, set size and timer, paint blocks and doors on a
    /// visual board, then Save or Play. Edits go to a draft copy and reach the level asset only on Save.
    /// </summary>
    public sealed class LevelEditorWindow : EditorWindow
    {
        private const string MenuPath = "Tools/Color Block Jam/Level Editor";
        private const string WindowTitle = "Level Editor";
        private const string PaintUndoName = "Paint Level";
        private const string Hint = "Left click: place    Right click / drag: erase    Ctrl+Z: undo";
        private const float SidePanelWidth = 230f;
        private const float Padding = 8f;
        private const float HintHeight = 20f;
        private const float LevelListHeight = 160f;
        private const float ActionButtonHeight = 32f;
        private const float MinBoardAreaWidth = 320f;
        private const float MinWindowHeight = 520f;
        private const int MinBoardSize = 3;
        private const int MaxBoardSize = 10;
        private const int MinTimerSeconds = 10;
        private const int MaxTimerSeconds = 300;
        private const int SecondsPerMinute = 60;

        [SerializeField] private LevelDefinition _asset;
        [SerializeField] private LevelDefinition _draft;
        [SerializeField] private LevelEditorBrush _brush = new LevelEditorBrush();
        [SerializeField] private Vector2 _levelListScroll;
        [SerializeField] private Vector2 _toolScroll;

        private readonly LevelEditorGrid _grid = new LevelEditorGrid();
        private LevelCatalog _catalog;
        private BlockPalette _palette;
        private GridHit _hover = GridHit.None;
        private GridHit _lastPainted = GridHit.None;
        private bool _dragging;
        private bool _dragErases;
        private int _dragUndoGroup;

        private string DisplayName => _asset != null ? CatalogName(_catalog.IndexOf(_asset), _asset) : "New level";

        /// <summary>Opens the level editor.</summary>
        [MenuItem(MenuPath)]
        public static void Open()
        {
            GetWindow<LevelEditorWindow>(WindowTitle);
        }

        /// <summary>Writes the draft to its level asset; called by Unity when closing with unsaved changes.</summary>
        public override void SaveChanges()
        {
            Save();
            base.SaveChanges();
        }

        private void OnEnable()
        {
            titleContent = new GUIContent(WindowTitle);
            minSize = new Vector2(SidePanelWidth * 2f + MinBoardAreaWidth, MinWindowHeight);
            wantsMouseMove = true;
            saveChangesMessage = "This level has unsaved changes. Save them?";
            Undo.undoRedoPerformed += OnUndoRedo;
            LoadProjectAssets();

            // The draft is a native object referenced by a serialized field, so it outlives domain reloads and keeps
            // unsaved edits across script compiles; DontSave keeps it out of scenes and asset unloading.
            if (_draft == null)
            {
                _draft = CreateInstance<LevelDefinition>();
                _draft.name = "LevelEditorDraft";
                _draft.hideFlags = HideFlags.DontSave;
                if (_catalog != null && _catalog.Count > 0 && _catalog.Get(0) != null)
                {
                    Load(_catalog.Get(0));
                }
            }

            RefreshUnsavedState();
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= OnUndoRedo;
        }

        private void OnDestroy()
        {
            if (_draft != null)
            {
                Undo.ClearUndo(_draft);
                DestroyImmediate(_draft);
            }
        }

        private void OnProjectChange()
        {
            LoadProjectAssets();
            RefreshUnsavedState();
            Repaint();
        }

        private void OnUndoRedo()
        {
            RefreshUnsavedState();
            Repaint();
        }

        private void OnGUI()
        {
            if (_catalog == null || _palette == null || _palette.Count == 0)
            {
                DrawSetup();
                return;
            }

            using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
            {
                float panelHeight = position.height - Padding * 2f;
                var left = new Rect(Padding, Padding, SidePanelWidth, panelHeight);
                var right = new Rect(position.width - SidePanelWidth - Padding, Padding, SidePanelWidth, panelHeight);
                Rect board = Rect.MinMaxRect(left.xMax + Padding, Padding, right.xMin - Padding, position.height - Padding - HintHeight);

                GUILayout.BeginArea(left);
                DrawLevelPanel();
                GUILayout.EndArea();

                GUILayout.BeginArea(right);
                DrawToolPanel();
                GUILayout.EndArea();

                DrawBoard(board);
                GUI.Label(new Rect(board.x, board.yMax, board.width, HintHeight), Hint, EditorStyles.centeredGreyMiniLabel);
            }
        }

        private void DrawSetup()
        {
            EditorGUILayout.HelpBox("The level editor needs a Level Catalog and a Block Palette with at least one colour.", MessageType.Info);
            if (_catalog == null && GUILayout.Button("Create Level Catalog"))
            {
                _catalog = LevelEditorAssets.CreateCatalog();
            }

            if (_palette == null && GUILayout.Button("Create Block Palette"))
            {
                _palette = LevelEditorAssets.CreatePalette();
            }

            if (_palette != null && _palette.Count == 0 && GUILayout.Button("Select Block Palette to add colours"))
            {
                Selection.activeObject = _palette;
            }
        }

        // The game shows the catalog position, so the editor leads with it; the asset name only tells files apart.
        private static string CatalogName(int catalogIndex, LevelDefinition level)
        {
            return catalogIndex < 0 ? $"{level.name} (not in catalog)" : $"{LevelLabel.For(catalogIndex)} · {level.name}";
        }

        private void DrawLevelPanel()
        {
            EditorGUILayout.LabelField("Levels (play order)", EditorStyles.boldLabel);
            _levelListScroll = EditorGUILayout.BeginScrollView(_levelListScroll, GUILayout.Height(LevelListHeight));
            for (int i = 0; i < _catalog.Count; i++)
            {
                LevelDefinition level = _catalog.Get(i);
                if (level == null)
                {
                    EditorGUILayout.LabelField($"{LevelLabel.For(i)} (missing level)");
                    continue;
                }

                bool selected = level == _asset;
                if (GUILayout.Toggle(selected, CatalogName(i, level), EditorStyles.miniButton) && !selected)
                {
                    SwitchTo(level);
                }
            }

            EditorGUILayout.EndScrollView();
            if (GUILayout.Button("+ New Level"))
            {
                SwitchTo(null);
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField(hasUnsavedChanges ? DisplayName + " *" : DisplayName, EditorStyles.boldLabel);
            DrawLevelSettings(_draft.Level);

            GUILayout.FlexibleSpace();
            using (new EditorGUI.DisabledScope(!hasUnsavedChanges))
            {
                if (GUILayout.Button("Save", GUILayout.Height(ActionButtonHeight)))
                {
                    Save();
                }
            }

            // An empty board starts already cleared, so there is nothing to play.
            using (new EditorGUI.DisabledScope(_draft.Level.Blocks.Count == 0))
            {
                if (GUILayout.Button("Save & Play", GUILayout.Height(ActionButtonHeight)))
                {
                    SaveAndPlay();
                }
            }
        }

        private void DrawLevelSettings(LevelData level)
        {
            EditorGUI.BeginChangeCheck();
            int width = EditorGUILayout.IntSlider("Width", level.Width, MinBoardSize, MaxBoardSize);
            int height = EditorGUILayout.IntSlider("Height", level.Height, MinBoardSize, MaxBoardSize);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(_draft, "Resize Board");
                int removed = LevelEditOperations.Resize(level, width, height);
                if (removed > 0)
                {
                    ShowNotification(new GUIContent($"Removed {removed} that no longer fit (Ctrl+Z to undo)"));
                }

                OnEdited();
            }

            EditorGUI.BeginChangeCheck();
            int timer = EditorGUILayout.IntSlider("Timer (seconds)", level.TimerSeconds, MinTimerSeconds, MaxTimerSeconds);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(_draft, "Change Timer");
                LevelEditOperations.SetTimer(level, timer);
                OnEdited();
            }

            EditorGUILayout.LabelField(" ", $"{level.TimerSeconds / SecondsPerMinute}:{level.TimerSeconds % SecondsPerMinute:00}");

            EditorGUILayout.Space();
            bool empty = level.Blocks.Count == 0 && level.Doors.Count == 0;
            using (new EditorGUI.DisabledScope(empty))
            {
                if (GUILayout.Button("Clear Board") &&
                    EditorUtility.DisplayDialog("Clear board", "Remove every block and door from this level?", "Clear", "Cancel"))
                {
                    Undo.RecordObject(_draft, "Clear Board");
                    LevelEditOperations.Clear(level);
                    OnEdited();
                }
            }
        }

        private void DrawToolPanel()
        {
            LevelData level = _draft.Level;
            _toolScroll = EditorGUILayout.BeginScrollView(_toolScroll);
            if (_brush.ColorId >= _palette.Count)
            {
                _brush.ColorId = 0;
            }

            LevelEditorPalette.Draw(_brush, _palette, SidePanelWidth - Padding * 2f, Mathf.Max(level.Width, level.Height));
            EditorGUILayout.EndScrollView();
        }

        private void DrawBoard(Rect area)
        {
            LevelData level = _draft.Level;
            _grid.Layout(area, level.Width, level.Height);
            HandleBoardInput(area);
            if (Event.current.type == EventType.Repaint)
            {
                _grid.Draw(level, _palette, _brush, _hover, _brush.Tool == LevelEditorTool.Erase || (_dragging && _dragErases));
            }
        }

        private void HandleBoardInput(Rect area)
        {
            Event current = Event.current;
            int control = GUIUtility.GetControlID(FocusType.Passive);
            switch (current.GetTypeForControl(control))
            {
                case EventType.MouseMove:
                    UpdateHover(area, current.mousePosition);
                    break;

                case EventType.MouseLeaveWindow:
                    _hover = GridHit.None;
                    Repaint();
                    break;

                case EventType.MouseDown:
                    if (!GUI.enabled || current.button > 1 || !area.Contains(current.mousePosition))
                    {
                        break;
                    }

                    GUIUtility.hotControl = control;
                    _dragging = true;
                    _dragErases = current.button == 1 || _brush.Tool == LevelEditorTool.Erase;
                    _lastPainted = GridHit.None;
                    Undo.IncrementCurrentGroup();
                    _dragUndoGroup = Undo.GetCurrentGroup();
                    Paint(_grid.HitTest(current.mousePosition));
                    current.Use();
                    break;

                case EventType.MouseDrag:
                    if (GUIUtility.hotControl != control)
                    {
                        break;
                    }

                    // Placing repeats only on a click; a drag that kept placing would scatter unwanted blocks.
                    UpdateHover(area, current.mousePosition);
                    if (_dragErases)
                    {
                        Paint(_hover);
                    }

                    current.Use();
                    break;

                case EventType.MouseUp:
                    if (GUIUtility.hotControl != control)
                    {
                        break;
                    }

                    GUIUtility.hotControl = 0;
                    _dragging = false;
                    Repaint();
                    Undo.CollapseUndoOperations(_dragUndoGroup);
                    current.Use();
                    break;
            }
        }

        private void UpdateHover(Rect area, Vector2 mouse)
        {
            GridHit hit = area.Contains(mouse) ? _grid.HitTest(mouse) : GridHit.None;
            if (!hit.SameAs(_hover))
            {
                _hover = hit;
                Repaint();
            }
        }

        private void Paint(GridHit hit)
        {
            if (hit.Kind == GridHitKind.None || hit.SameAs(_lastPainted))
            {
                return;
            }

            _lastPainted = hit;
            LevelData level = _draft.Level;
            Undo.RecordObject(_draft, PaintUndoName);

            bool changed = false;
            if (_dragErases)
            {
                changed = hit.Kind == GridHitKind.Cell
                    ? LevelEditOperations.RemoveBlockAt(level, hit.Cell)
                    : LevelEditOperations.RemoveDoorAt(level, hit.Edge, hit.Slot);
            }
            else if (_brush.Tool == LevelEditorTool.Block && hit.Kind == GridHitKind.Cell)
            {
                Vector2Int origin = LevelEditOperations.OriginForClick(_brush.ShapeId, hit.Cell);
                changed = LevelEditOperations.TryPlaceBlock(level, _brush.ShapeId, origin, _brush.ColorId);
            }
            else if (_brush.Tool == LevelEditorTool.Door && hit.Kind == GridHitKind.DoorSlot)
            {
                LevelEditOperations.PlaceDoor(level, hit.Edge, hit.Slot, _brush.DoorLength, _brush.ColorId);
                changed = true;
            }

            if (changed)
            {
                OnEdited();
            }
        }

        private void OnEdited()
        {
            RefreshUnsavedState();
            Repaint();
        }

        private void SwitchTo(LevelDefinition level)
        {
            if (!ConfirmLeaveDraft())
            {
                return;
            }

            if (level != null)
            {
                Load(level);
            }
            else
            {
                _asset = null;
                Overwrite(_draft.Level, NewLevelData());
                Undo.ClearUndo(_draft);
            }

            RefreshUnsavedState();
            GUI.FocusControl(null);
        }

        private void Load(LevelDefinition level)
        {
            _asset = level;
            LevelEditorAssets.CopyLevel(level, _draft);
            Undo.ClearUndo(_draft);
        }

        private bool ConfirmLeaveDraft()
        {
            if (!hasUnsavedChanges)
            {
                return true;
            }

            int choice = EditorUtility.DisplayDialogComplex(
                "Unsaved changes", $"Save the changes to {DisplayName}?", "Save", "Cancel", "Discard");
            if (choice == 0)
            {
                Save();
            }

            return choice != 1;
        }

        private void Save()
        {
            if (_asset == null)
            {
                _asset = LevelEditorAssets.CreateLevel(_catalog, _draft);
            }
            else
            {
                LevelEditorAssets.SaveLevel(_draft, _asset);
            }

            RefreshUnsavedState();
            ShowNotification(new GUIContent("Saved " + _asset.name));
        }

        private void SaveAndPlay()
        {
            Save();
            if (_catalog.IndexOf(_asset) < 0)
            {
                LevelEditorAssets.AddToCatalog(_catalog, _asset);
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            if (SceneManager.GetActiveScene().path != LevelEditorAssets.BootScenePath)
            {
                EditorSceneManager.OpenScene(LevelEditorAssets.BootScenePath);
            }

            EditorPlayRequest.Set(_catalog.IndexOf(_asset));
            EditorApplication.EnterPlaymode();
        }

        private void LoadProjectAssets()
        {
            _catalog = LevelEditorAssets.FindFirst<LevelCatalog>();
            _palette = LevelEditorAssets.FindFirst<BlockPalette>();
        }

        // A new level counts as changed once it differs from the defaults; a loaded one once it differs from its asset.
        private void RefreshUnsavedState()
        {
            LevelData saved = _asset != null ? _asset.Level : NewLevelData();
            hasUnsavedChanges = JsonUtility.ToJson(_draft.Level) != JsonUtility.ToJson(saved);
        }

        private static LevelData NewLevelData()
        {
            return new LevelData(LevelDefinition.DefaultWidth, LevelDefinition.DefaultHeight, LevelDefinition.DefaultTimerSeconds);
        }

        private static void Overwrite(LevelData target, LevelData source)
        {
            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(source), target);
        }
    }
}
