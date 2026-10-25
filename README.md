# Color Block Jam — vertical slice

A vertical slice of Rollic's *Color Block Jam* for the Game Developer Case 2026: home screen, settings, five levels
with a countdown, coins that persist, pause / restart / fail flow, and an in-editor level editor that a designer
can use without touching code or files.

Unity **2022.3.62f2**, URP (mobile "Performant" asset), portrait 1080×1920 (also laid out for 20:9), Android.
No third-party packages; only the supplied art.

## Open and run

1. Clone the repository and open the folder with Unity Hub using Unity **2022.3.62f2** (Android Build Support
   module installed for the APK).
2. Open `Assets/_Project/Scenes/Boot.unity` — the only scene. Screens and popups are children of its canvas.
3. Set the Game view to **1080x1920** (or a 20:9 size such as 1080x2400) and press Play.
4. Drag a block with the mouse (or one finger on device). A block leaves through a door of its colour that is
   wide enough for it. Clear the board before the timer runs out. The *Hammer* booster breaks one block you tap;
   *Freeze* stops the timer for 10 seconds (one use each per attempt).

Build: **Tools > Build > Android** writes `Builds/Android/ColorBlockJam.apk` (IL2CPP, ARM64, portrait). Headless:
`Unity -batchmode -projectPath . -buildTarget Android -executeMethod CBJ.Editor.BuildScript.BuildAndroid`.

Tests: **Window > General > Test Runner > EditMode > Run All**.

## Make a level in the editor

![Level editor](Docs/editor.png)

1. Open **Tools > Color Block Jam > Level Editor**.
2. Click **+ New Level** (or pick an existing level in *Levels (play order)*; new levels are added to the end of the
   play order).
3. Set **Width**, **Height** and **Timer (seconds)** with the sliders.
4. **Blocks:** choose the *Block* tool and a colour, then drag over cells — one drag is one block. *Shift+drag*
   from a block adds cells to it. Choose a preset under *Shape* to stamp it with a click; **R** rotates it.
   *Kind* sets what new blocks are: Normal, Arrow H / Arrow V (slides only along that axis) or Ice 1–3 (frozen
   until that many other blocks have left). The grid draws arrows and hatched ice with its layer count.
5. **Doors:** choose the *Door* tool and a colour, then click or drag over the slots around the board. Neighbouring
   door cells of one colour form one door.
6. **Fix mistakes:** right-click (or the *Erase* tool) removes a block or a door cell; **Ctrl+Z** undoes.
7. Read the **Checks** panel: errors (for example a block outside the board) block saving; warnings (for example a
   block with no door of its colour wide enough) ask for confirmation.
8. Click **Save**, then **Save & Play** to play the level straight away. A level played from the editor does not
   change the player's saved progress or coins.

The editor writes `Assets/_Project/Levels/Level_NN.asset` and adds it to `LevelCatalog.asset`. The game reads the
same `LevelData` format; there is no second format. All five shipped levels were made in this editor.

## Architecture

```
Core      pure C#: BoardModel, Block, BlockShape, Door, MoveResolver, LevelData, LevelValidator
Data      ScriptableObjects: LevelDefinition, LevelCatalog, GameConfig, BlockPalette, BoardSkin
Gameplay  BoardView, BlockView, DoorView, DragInput, LevelSession, LevelPlayer, BoardCamera
UI        UIRouter, ScreenFlow, Home / Settings / HUD / Pause / Fail, widgets
App       GameBootstrap (composition root), SaveService
Editor    LevelEditorWindow and helpers, BuildScript
```

Each folder is an assembly; references only point left (Core ← Data ← Gameplay ← UI ← App, Editor sees
Core + Data + Gameplay), and an EditMode test fails if an asmdef breaks that direction.

Decisions and why:

- **Pure C# model, thin views (ADR-001).** The board, blocks, doors and move rules live in `CBJ.Core` with no scene
  or component dependency, so every rule is tested in EditMode without a scene. Views subscribe to model events
  (`BlockMoved`, `BlockExited`, `Cleared`) and never poll; the model does not know the views exist. A new block
  type is a model rule plus a view, and the editor and the game share the same data.
- **Continuous drag over a cell model (ADR-002).** The model stays on whole cells and moves one cell per step with a
  collision check, so the rules are deterministic and fully testable. On top of it the held block moves freely in
  two dimensions: each frame the finger's lead is applied along X, then Y, each clamped by the nearest obstacle
  (the unit cells of the other blocks and the wall cells around the board; door cells are open only to blocks of
  their colour). The model origin is the rounded position, and the model steps whenever it changes. A box that
  overlaps no obstacle is at least half a cell clear of it on some axis, so the rounded footprint is always free
  (`BlockDrag`, pure and tested, including a randomised walk). A block exits when it is moved into a door of its
  colour whose width fits the block's side and every cell has a clear path to the edge.
- **One level format: `LevelData` inside a `LevelDefinition` ScriptableObject, ordered by `LevelCatalog`
  (ADR-003).** Unity-native, referenced by drag and drop, inspectable, no file management. `LevelData` is a plain
  serializable class, so moving to JSON later would be one serializer. The game only reads it; edits go through
  `internal` methods visible to the editor and tests.
- **Level editor as an IMGUI `EditorWindow` (ADR-004).** It is the studio-standard home for designer tools, it is
  fast to build, and it keeps paint, settings, checks, Save and Play in one window. A runtime editor would have
  cost far more UI work for no gain in this case.
- **uGUI with `UIRouter` + `IScreen` (ADR-005).** The supplied UI art is sprites with 9-slice borders, which uGUI
  handles directly. Canvas Scaler 1080×1920 with match 0.5 and a safe-area fitter keeps the layout correct from
  16:9 to 20:9. Screens are shown and popups stacked by a small router; button feedback is a curve-driven
  `ScaleOnPress` instead of a tween library.
- **Zero third-party packages, manual composition root (ADR-006).** `GameBootstrap` creates and wires every
  system in one readable place; collaborators get their dependencies through `Initialize`, and test doubles go
  through interfaces (`IPointerSource`, `IKeyValueStore`). Nothing in the project needs a library to be understood.
- **Freehand blocks stored as cells (ADR-007).** A block is saved as its normalised cell list plus origin and
  colour, so a designer can draw any shape without a code change. Presets remain as stamps. A colour-per-cell grid
  was rejected because two touching blocks of the same colour would merge into one.

Other choices worth knowing:

- Data-driven tuning: sizes, colours, timers, speeds, the camera pitch and coin reward live in `GameConfig`,
  `BlockPalette` and `BoardSkin`.
- Rendering: one shared material per piece type, colour through `MaterialPropertyBlock`. Each block's modular
  pieces are merged into one pooled mesh per block (rebuilt when an attempt starts), so a block is one draw call.
- No per-frame allocations in gameplay: pooled views and pieces, no LINQ or closures in update or drag code; the
  move resolver has zero-allocation tests.

### Performance pass

Measured in the editor on a temporary 7×9 stress level (15 blocks covering 49 cells, 8 doors over 22 door cells),
built in memory, while dragging a block back and forth for 320 frames (40 cell moves). GC is measured from the
first to the last script update of each frame.

| | Before | After merging block pieces |
|---|---|---|
| Active renderers on the board | 313 | 146 |
| Board draw calls (Frame Debugger) | 206 | 39 |
| Script GC allocation per frame while dragging | 0 B | 0 B |

Script allocations were already zero, so the change only targeted renderers: 182 block-piece renderers became 15
block renderers. The remaining non-batched draws are the 22 tinted door frames; floor, walls and arrows go through
the SRP Batcher. Not measured on a device yet. Since then each merged door is drawn as one stretched frame with one
arrow, so a door is one tinted renderer whatever its width.

## Optional scope

Implemented after the required scope was complete (ADR-008), with every rule in Core and covered by EditMode tests:

- **O1 level validation** — static checks in the editor (see Decisions), including the two block kinds below.
- **O3 ice blocks** — an ice block cannot move while it has layers (1–3). Every time another block leaves the
  board, through a door or the hammer, each ice block loses one layer. Drawn as a translucent shell made from
  `icetexture.jpg` that shrinks away when the last layer goes.
- **O4 arrow blocks** — an arrow block slides and exits only along its axis. The arrow from `Arrows.fbx` sits on
  the block's longest straight run and is merged into the block's mesh, so the block is still one renderer.
- **O2 boosters, partly** — *Hammer* (tap it, then tap a block to destroy it; tap an empty cell or the hammer again
  to cancel) and *Freeze* (stops the countdown for 10 seconds). One use of each per attempt, refilled on restart
  and on a new level, not saved. *Vacuum* and *Rocket* stay locked placeholders: no rules were specified for them,
  and a coin shop to refill boosters was left out to keep the scope small.

## Decisions on unclear requirements

- **Blocks** are painted freely: one drag is one block, cells must be 4-connected (disconnected cells are dropped
  with a notice). Presets are stamps; a click places the preset's top-left filled cell on the clicked cell.
- **Doors** are painted per cell; touching cells of one colour on one edge form one door, and erasing removes one
  door cell.
- **Exit rule:** a block leaves through a door of its colour only if its side fits the door's width and every
  cell has a clear path to that edge (so concave shapes cannot clip through walls). The exit happens in one step.
- **Auto-exit:** whenever a held block's rounded position touches an edge whose door lets it out, it leaves at
  once (the last step's direction is tried first), so the player never has to push past the edge or line the block
  up exactly with the door. A block resting next to its door leaves on the first move after it is grabbed.
- **Drag feel:** a grabbed block lifts slightly and casts a soft shadow (a rounded quad from the supplied
  `SquareRoundedMask`, sized to the block's bounding box, so L shapes get a rectangular shadow). It follows the
  finger exactly wherever the board lets it go and slides along walls and blocks it is pushed against; the block
  chases the finger's position (not each frame's movement), so it catches up after being stopped. Released, it
  eases back onto its model cell (120 ms) and drops.
- **Exit animation:** the door swallows the block: it slides along the exit axis and shrinks only along that axis,
  pinned at the door plane (280 ms, ease-in), while its shadow follows and fades. The door sinks to the board
  surface meanwhile and rises again after. A block destroyed by the hammer shrinks away in place. Only model events
  drive it. Pause freezes the timer and input only;
  an exit or settle already under way finishes behind the Pause popup.
- **Validation** (optional O1) is static only: board size, timer, overlapping or out-of-range blocks and doors,
  and whether every block has a door of its colour wide enough. There is no solver. A level with errors cannot be
  saved or played from the editor; an empty level cannot be played.
- **Block kinds** are stored on each block (`Kind`, `ArrowAxis`, `IceLayers`); levels saved before kinds existed
  load as normal blocks. An arrow block must find its door on the two edges of its axis, or the editor warns.
- **Optional scope:** the requirements list O2–O4 as out of scope; they were brought in once the required scope
  was complete (see *Optional scope*).
- **Ice "can never thaw"** assumes every non-ice block leaves and the ice blocks then leave in order of their
  layers; an ice block warns when fewer blocks than its layers can leave before it. Equal is fine: with as many
  layers as blocks before it, the ice thaws after the last of them and can still exit. Two ice blocks with nothing
  else to thaw them both warn. A frozen ice block cannot be picked up.
- **Hammer** counts a destroyed block as cleared (it can win the level) and thaws ice like an exit. A frozen ice
  block can be hammered.
- **Freeze** used during a freeze would extend it; with one use per attempt this does not happen in play. While
  frozen the timer holder and its text turn blue and the low-time pulse stops.
- **Input:** one pointer only — the first finger drags, a second finger is ignored and never takes over. A press
  that starts on UI never drags a block. A diagonal drag moves along X first, then Y.
- **Flow:** a win pays the coin reward, saves the next level and advances automatically after a short delay (no
  win popup); after the last level the game wraps to level 1. A fail opens a popup with Retry and Home.
- **Leaving the app** mid-level (home button, call, lock screen) pauses the attempt and shows the Pause popup on
  return, so the timer does not run out in the background.
- **Coins** are shown on Home and in the HUD; the HUD count updates when a win pays the reward (no coin
  animation).
- **HUD restart** works only while the attempt runs (not while paused, nor after a win, when the reward and the
  next level are already saved). The Pause and Fail popups keep their own restart.
- **Boosters** respond only while the attempt runs (not while paused or after a win or fail); a spent booster greys
  out with a 0 badge. Pausing, restarting or ending the attempt puts an armed hammer away.
- **Low time:** under 10 seconds the timer turns red and pulses; the pulse stops on pause and fail.
- **Saved progress** is a catalog index: if the catalog shrinks below it, progress restarts at level 1.
- **Settings toggles** (vibration, sound, music) show a distinct on/off state and are saved; only an "On" sprite
  was supplied, so "Off" is the same art tinted grey with an "Off" label. The game has no audio or haptics.
- **Placeholders** (tabs, the Vacuum and Rocket boosters, profile, lives, other buttons) give press feedback only.
- **Camera:** perspective, 45° field of view, pitched 55° (both in `GameConfig`). `CameraFit` solves for the
  closest distance at which the board, its walls and a margin fit the frustum between the HUD bands and inside the
  safe area; on every current level at 9:16 and 20:9 the fit is width-bound. Supplied block pieces are 1-unit
  quadrants, so a cell is 2 world units; each door has one white arrow in its middle.
- **Board pieces:** the supplied block pieces reach below their pivot, so each merged block is raised until its
  lowest point rests on the floor tiles, and flat fillers close its open underside. The supplied wall pieces are
  much taller than a cell below their cap; they are scaled down in height around the cap (`BoardSkin`), so the rim
  reads short from the tilted camera.
- **Palette:** colour ids are palette indices, so new colours are only appended (Dark Green is id 8).
- **Text** uses uGUI legacy `Text` with the built-in font (TextMesh Pro resources are not imported).
- **Restart** goes from the UI straight to the level player, while Play and Home go through the game flow; restart
  does not change which level is played.

## Known problems

- No Android back-button handling.
- Reordering levels in the catalog moves a player's saved index to a different level.
- Reserved UI bands are screen fractions: correct from 16:9 to 20:9, not tuned for 3:4 tablets.
- Door frames are one tinted renderer per door, so they are not SRP-batched.
- A level edited by hand into an invalid state and added to the catalog is not guarded at runtime; the editor is
  the only checked path.
- `BlockData.Cells` exposes its backing list (read-only interface, but castable).
- Performance has been measured in the editor only; the frame rate on a mid-range device is still to be checked.
- In the game an ice block does not show how many layers it has left (the editor does); the shell only disappears
  when the last layer thaws.
- A placed block's kind cannot be changed in the editor; erase it and paint it again.

## LLM tools

Claude Code (Anthropic) was used as an implementation assistant: writing C# from my architecture spec and
decision records, EditMode tests, editor-tool boilerplate (IMGUI), and the first draft of this README.
Architecture and data-model decisions, the editor's UX design, level design, code review, device testing and
the final verification of every acceptance criterion are my own work. No LLM-generated art or assets were used.

## Approximate work time

About **2 calendar days** (24–25 October 2026), roughly **8–9 active hours**: planning and decision records, then
eight milestones from project setup to this build.
