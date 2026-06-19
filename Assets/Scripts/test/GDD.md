# SIGHT — Game Design Document (Jam Demo, 6-8h)

## 1. Pitch

A dark 2D top-down maze. You only see a small circle of light that follows
your gaze (tracked via webcam, using head position as a proxy). You move
the character with WASD to find the exit, but the character is separate
from where you're looking: if you leave it outside your field of view for
too long, its sanity collapses. Enemies in the maze move toward you and can
only be destroyed by holding the light on them for a few seconds.

The core tension: your gaze is a scarce resource you must split between
"looking ahead to navigate," "looking at enemies to kill them," and
"looking at your own character so it doesn't lose its mind."

## 2. Core Loop

1. The player walks through the maze (WASD).
2. The player moves their head in front of the webcam to move the vision
   circle on screen, independently of the character's movement.
3. If the character stays outside the circle too long → sanity drops.
4. If an enemy enters the circle and stays lit long enough → it dies.
5. If an enemy touches the character → game over.
6. If sanity hits 0 → game over.
7. If the character reaches the exit → victory.

## 3. Balancing Numbers (starting point, tune via playtest)

| Parameter | Starting value | Notes |
|---|---|---|
| Vision circle radius | 2.2 world units (~2.5 tiles) | too big = no tension |
| Max sanity | 100 | |
| Sanity drain (character out of sight) | -15 / sec | punishing but not instant |
| Sanity regen (character in sight) | +8 / sec | intentionally slower than drain |
| Exposure time to kill an enemy | 1.5 sec of continuous light | timer resets if light leaves |
| Enemy speed | 60% of player speed | otherwise you can never escape |
| Enemy-player contact | instant game over | simple to implement, very readable |
| Head-tracking sensitivity | tune in playtest | too high = nausea, too low = frustrating |

If you have time, replace "instant game over on contact" with a big sanity
hit (e.g. -40) + knockback: softer, but more complex to get right. For a
6-8h jam, stick with instant game over.

## 4. Technical Systems (Unity)

Architecture designed to be parallelizable across the team from the start.

- **GazeTracker** — exposes `Vector2 ScreenPosition` (where the player is
  "looking" on screen). Two interchangeable modes: `Mouse` (fallback, uses
  the pointer) and `Webcam` (real head-tracking). **Always start in Mouse
  mode**: build and test the entire game without touching the webcam, and
  only wire it in at the end once the core loop works. This is your safety
  net — if webcam tracking doesn't converge in time, the demo is still
  fully playable with the mouse.
- **VisionController** — reads `GazeTracker.ScreenPosition`, converts it to
  world space (`Camera.ScreenToWorldPoint`) and positions the light circle
  there. Exposes `WorldPosition` and `Radius` for other systems.
- **PlayerController** — standard WASD movement (Rigidbody2D).
- **SanityManager** — computes the distance between the character and
  `VisionController.WorldPosition`; drains sanity if it exceeds the radius.
- **EnemyAI** — chases the player, checks whether it's inside the vision
  circle to accumulate the "kill" timer, handles contact game-over.
- **GameManager** — state machine: Calibration → Playing → Win/Lose.

### 4.1 Rendering the darkness (the part you must not get wrong)

Create the Unity project with the **2D (URP)** template: it already
includes the Universal Render Pipeline with Light2D support, saving you
hours of hand-written shaders.

- Add a **Global Light 2D** with intensity near zero (e.g. 0.03-0.05): this
  makes everything dark by default.
- Create an empty GameObject with a **Light2D component of type Point**
  (Parametric also works): outer radius = vision radius, high falloff for a
  soft edge, intensity ~1.5-2. This is your "light/gaze circle." The
  `VisionController` moves this GameObject's position every frame.
- Make sure the maze/enemy/player sprites use the **Sprite-Lit-Default**
  material (not Unlit), otherwise the Global Light won't affect them and
  they'll always be visible regardless of darkness.

This gives you the "flashlight/gaze" effect for free, with no custom
fog-of-war code required.

## 5. Level Design — Demo Level

11x9 cell maze (ASCII reference layout — rebuild it with Unity's Tilemap or,
if you use the included scene-generation script, it's built automatically
with placeholder sprites; either way, feel free to tweak individual cells
in the editor — what matters is keeping: a single main path from entrance
to exit, and 2-4 dead ends to place enemies in for ambushes).

```
###########
#S....#X..#
#####.###.#
#...#..X#.#
#.#.###.#.#
#.#..X#...#
#.#######.#
#..X.....E#
###########
```

Legend: `#` wall, `.` floor, `S` player spawn, `E` exit (trigger collider →
victory), `X` enemy spawn (4 total: two in dead ends, two along the main
path, so the player can't avoid every encounter).

Recommended scale: 1 cell = 1.5-2 Unity units, so the whole maze is visible
with a **static camera** framing the entire level (no camera following the
player: this massively simplifies the screen→world conversion for the
vision circle, and is more than enough for a single-level demo).

## 6. Assets (so you don't waste time drawing)

With 6-8h, don't draw assets from scratch. Recommended pack, CC0
(free to use, no attribution required):

- **Kenney – Tiny Dungeon** (https://kenney.nl/assets/tiny-dungeon): a
  complete top-down dungeon tileset with walls, floors, characters and
  items, ready for Tilemap.
- **Kenney – Tiny Creatures** (compatible expansion, on itch.io/OpenGameArt):
  monsters and creatures in the same style, useful for enemies.
- Audio: Kenney's audio packs (kenney.nl/assets, audio category) for
  footsteps, ambient drone, and a "heartbeat" that speeds up as sanity
  drops — a cheap effect with a big perceived impact.

The included scene-generation script (see section 7) uses simple colored
square placeholders instead of these sprites, so you get a playable scene
immediately; swap in the real tileset/sprites once imported.

## 7. Ready-to-Import Unity Project Files

This package includes:

- `Scripts/` — all gameplay scripts (`GazeTracker`, `VisionController`,
  `PlayerController`, `SanityManager`, `EnemyAI`, `GameManager`,
  `ExitTrigger`).
- `Editor/SceneBuilder.cs` — an editor tool that programmatically builds a
  full, working scene (camera, lights, maze, player, enemies, exit, UI)
  with everything wired up, instead of a hand-written `.unity` file.

**Why a build script instead of a raw scene file?** Unity scene files
reference components and scripts by internal GUIDs that depend on your
exact installed packages (URP, UI, etc.) and Unity version. A hand-authored
`.unity` file risks broken/missing references that are hard to debug under
jam time pressure. A script that calls Unity's real API (`AddComponent`,
etc.) lets the Editor resolve all of that correctly for your project, and
is far more reliable.

### How to use it

1. Create a new Unity project with the **2D (URP)** template (or add the
   Universal RP package to an existing 2D project and assign a 2D Renderer
   Data asset in your URP Asset).
2. Copy `Scripts/` into `Assets/Scripts/` and `Editor/SceneBuilder.cs` into
   `Assets/Editor/SceneBuilder.cs`.
3. Let Unity compile. In the menu bar, go to **Tools > Sight Game > Build
   Demo Scene**.
4. This creates and saves `Assets/Scenes/DemoLevel.unity` with the full
   maze, player, 4 enemies, exit trigger, sanity UI, and calibration/win/
   lose panels already wired to the scripts. Open it and press Play.
5. Check the Console: if URP/Light2D wasn't detected, you'll get a warning
   telling you to add the Light2D components manually (2 minutes of work,
   see section 4.1) — everything else will still be wired up correctly.
6. Press SPACE on the calibration screen to start. `GazeTracker` defaults
   to Mouse mode — switch it to Webcam mode on the `Managers` GameObject in
   the Inspector once you're ready to test real head-tracking.

## 8. Hour-by-Hour Plan (for a team working in parallel)

| Time | Task |
|---|---|
| 0:00–0:30 | Project setup (2D URP), run the scene builder, agree on balancing numbers |
| 0:30–2:00 | **Track A**: swap placeholder sprites for real assets, tweak maze layout in-editor. **Track B**: verify Light2D/darkness setup, tune vision radius. **Track C** (3+ people): develop the webcam GazeTracker mode in isolation, in a blank test scene |
| 2:00–3:30 | Tune EnemyAI behavior, sanity drain/regen feel, UI polish |
| 3:30–4:30 | Wire in webcam mode, calibration flow, tune sensitivity |
| 4:30–5:30 | Win/lose screens, audio, low-sanity vignette effect |
| 5:30–6:30 | Playtest, rebalance numbers, bug fixing |
| 6:30–7:00+ | Build, buffer, pitch rehearsal |

**Golden rule:** don't integrate the webcam before the core loop (movement,
enemies, sanity, win/lose) fully works with the mouse. Webcam tracking has
the most unknowns (varies by camera, room lighting, skin tone) and is also
the most likely thing to misbehave on the first try — keep it isolated and
test it last, never as a blocking dependency for the rest of the game.

## 9. Risks and Mitigations

- **Webcam tracking is inaccurate / doesn't detect the face**: `GazeTracker`
  has a confidence threshold — if it doesn't find enough "skin" pixels, it
  keeps the last known position instead of jumping randomly. Add a keyboard
  shortcut to force recalibration (useful during the demo if room lighting
  changes).
- **Enemies getting stuck in maze corners**: the included `EnemyAI` does
  simple steering (tries alternate directions if the direct one is
  blocked); it's not real pathfinding, but it's enough for a maze this
  size. If you have extra time, consider NavMeshPlus for proper 2D
  pathfinding.
- **Presenting in a dimly lit room**: skin-color tracking degrades in low
  light. If possible, present near a front-facing light source, or force
  Mouse mode for the live demo if the webcam isn't reliable in the moment —
  a solid fallback beats a demo that risks breaking on stage.
