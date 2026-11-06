# Cellstorm

Cellstorm is a physics-based 2D swarm survival roguelike where you evolve a colony of cells, build powerful mutations, and survive a hostile petri dish.

- **Engine:** Unity 6 (6000.6.0f1), 2D, Universal Render Pipeline, Input System 1.20.1
- **Platforms:** Windows, macOS and Linux desktop (keyboard and mouse)
- **Design documents:** [`Docs/`](Docs) (GDDs and solo dev plans for V0.1.1 and V0.2)

## Status

| Version | State |
| --- | --- |
| V0.1 | Finished (cell-count model). Tag it as `v0.1`. |
| V0.1.1 | In progress, built phase by phase from `Docs/Cellstorm_V0_1_1___Solo_Dev_Plan.docx`. **Phase 1 (health and stats core) is in.** |
| V0.2 | Not started. Follows V0.1.1 (`Docs/Cellstorm_V0_2___Solo_Dev_Plan.docx`). |

What Phase 1 changed (so you know what is temporary):

- Cells became **HP**: max HP 100, regen 1 HP/s applied every physics tick, DEF reduces every hit on a log curve, Speed is 1 unit/s per point.
- Every swarm has a **fixed size**: the player's radius is 0.335 (Giant 0.67), an enemy's is set once from its max HP. The size never follows HP.
- New **Giant** loadout (double size). It unlocks at 5,000 lifetime score.
- Enemies got the V0.1.1 numbers (HP 10 / 100 / 500 / 10, contact damage 5 / 10 / 10 / 5, flat score). Bosses use 500 HP x milestone.
- **Temporary until later phases:** the player deals 30 HP/s by touching (Phase 2 replaces it with the Ram dash), the swarm still draws as dots (Phase 3), weapon cells use a simple 12-cell stand-in (Phase 4), the HUD still shows a heart (Phase 5), and the balance bot is not meaningful yet (Phase 6).

## Setup

### 1. Install

1. Install [Unity Hub](https://unity.com/download).
2. In the Hub, install editor **6000.6.0f1** (the exact version is in `ProjectSettings/ProjectVersion.txt`). Add the module for your platform's build target if you want to make builds.
3. Install an IDE with Unity support: Visual Studio 2022 with the **Game development with Unity** workload (see `.vsconfig`), or JetBrains Rider.

### 2. Get the project

```
git clone <your repository url> Cellstorm
```

Or use the folder you already have (`F:\Proj\Unity\Cellstorm`).

### 3. Open it

1. Unity Hub > **Add** > **Add project from disk** > pick the `Cellstorm` folder.
2. Open it with 6000.6.0f1. The first import takes a few minutes. Packages come from `Packages/manifest.json` and restore on their own.
3. If Unity asks about the Input System backend, choose the new Input System (the project already uses it). If it asks to enter Safe Mode, there is a compile error: fix it first (see Troubleshooting).
4. **Data migration (once, after Phase 1):** a dialog says "Cellstorm data is out of date". Click **Regenerate now**. It copies `Assets/Data` to `Backups/` first, rebuilds the weapons, enemy types, loadouts and upgrades from the code defaults (keeping file names and GUIDs), and adds the Giant loadout to the Game scene. You can run it later from **Cellstorm > Data > Regenerate data (V0.1.1)**.
5. Run **Cellstorm > Tests > Run self tests**. The Console should say `0 failed`.

### 4. Play

1. Open `Assets/Scenes/Boot.unity` (it loads the main menu).
2. Press **Play**.

To test the Game scene directly, open `Assets/Scenes/Game.unity` and press Play. Pick a loadout, then play.

## Controls

| Action | Input |
| --- | --- |
| Move | WASD |
| Aim | Mouse |
| Weapons | Left mouse button, right mouse button (hold to form Jaw or Gun, release to use) |
| Skills | Q, E |
| Swap weapon and skill slots | F |
| Pause | Esc |

The bindings live in `Assets/Resources/CellstormInput`.

### Debug keys (Game scene)

| Key | Effect |
| --- | --- |
| F12 | Show or hide the debug overlay |
| F11 | Treat everything as unlocked |
| G | God mode |
| Y | DEF +5 |
| L | Defeat all enemies |
| N / J / E | Next boss / +60 s / endless mode |
| 1-5 | Max HP 20 / 100 / 500 / 2000 / 10000 |
| = / - | Double / halve max HP |
| [ / ] | Body size x1 / x2 (Giant) |
| Z / X / C / V | Spawn test enemies (10 HP / 100 HP / 500 HP DEF 3 / same DEF as you) |
| U | Take a 20 damage test hit |
| H | Heal to full |
| , / . | Base DEF -1 / +1 |
| 6 / 7 / 8 / 9 / 0 | Equip Ram / Jaw / Gun / Hardening / clear slots (hold Shift for the second slot) |
| I / O / P | Spawn a ranged / jaw / ram enemy |
| F6-F10 | Level up, reroll, shells, EXP orbs, Dense Cells x5 |

## Project layout

```
Assets/
  Scenes/       Boot, MainMenu, Game
  Scripts/
    Combat/       SwarmStats (HP model), DefCurve, SpeedMath, SwarmSize, DamageInfo, SwarmCombat
    Swarm/        SwarmBody (player), SwarmVisual and CircleSwarmVisual, MetaballLayer, CameraFollow
    Enemies/      Enemy, EnemySpec, EnemySpawner
    Weapons/      AbilityController, Ram, Jaw, Gun, Hardening, Bullet, WeaponCell
    Progression/  PlayerBuild, PlayerProgress, RunSetup (loadouts), Pickup, EXP
    Data/         ScriptableObject types and the code defaults (WeaponDefaults, EnemyDefaults, UpgradeDefaults, LoadoutDefaults)
    Meta/         Save data and service, unlocks, run recorder
    Core/         GameManager, RunDirector, input, scene loading
    UI/           IMGUI screens (HUD, level-up, menus, summary, leaderboard)
    Debug/        Test harnesses and the balance bot
  Data/         Generated ScriptableObject assets (Weapons, Enemies, Loadouts, Upgrades)
  Editor/       Setup menus, BuildTools, RegenerateData, SelfTests
  Resources/    CellstormInput (input actions)
Docs/           GDDs and dev plans
Backups/        Created by Regenerate data (ignored by Unity, not part of the build)
```

The data assets in `Assets/Data` are generated from the code defaults. To change a balance number for good, change it in the `*Defaults.cs` file and run **Regenerate data**, or edit the asset in the Inspector.

## Editor menus

| Menu | What it does |
| --- | --- |
| Cellstorm > Data > Regenerate data (V0.1.1) | Rebuilds the data assets in place (with a backup) |
| Cellstorm > Data > Wire loadouts into open scene | Re-links the loadout list of the open Game scene |
| Cellstorm > Tests > Run self tests | Checks the DEF curve, Speed math, sizes and HP rules |
| Cellstorm > Build > 1 - Prepare project | Scenes in build settings, shaders, window settings |
| Cellstorm > Build > 2 - Build for the current platform | Makes a build in `Builds/` |
| Cellstorm > Phase N > Setup Game Scene | V0.1 scene setup helpers (only needed on a fresh scene) |

## Building

1. **Cellstorm > Build > 1 - Prepare project** (once).
2. **Cellstorm > Build > 2 - Build for the current platform**.

## Git workflow

Work on V0.1.1 in a branch so V0.1 stays playable:

```
git tag v0.1                 # on the commit that is the finished V0.1
git checkout -b v0.1.1
# ... after each phase:
git add -A
git commit -m "V0.1.1 Phase 1: health and stats core"
```

Later: merge `v0.1.1` to `main`, tag `v0.1.1`, then branch `v0.2`.

`.gitignore` already excludes `Library/`, `Temp/`, `Logs/`, `Builds/` and user settings. Commit the `.meta` files (Unity needs them). Add `Backups/` to `.gitignore` if you do not want the data backups in the repository.

## Save file

`cellstorm_save.json` in Unity's `Application.persistentDataPath` (on Windows: `%USERPROFILE%\AppData\LocalLow\<company>\Cellstorm`). Save version 2 (V0.1.1) keeps unlocks, high score and lifetime score from V0.1; old leaderboard rows show `-` for max HP. Delete the file to reset progress.

## Troubleshooting

- **Compile errors after pulling a phase:** read the first error in the Console. A missing type usually means a file did not copy. Check that every file of the phase is in `Assets/Scripts`.
- **"Data is out of date" keeps appearing:** the `Assets/Data/data_version.txt` file is written at the end of Regenerate data. If the run failed, the Console shows why. Fix that and run it again from **Cellstorm > Data**.
- **Enemies have 1 or 5 HP, or the Giant loadout is missing in the picker:** the data was not regenerated, or you are looking at a scene that was not opened during regeneration. Open `Game.unity` and run **Cellstorm > Data > Wire loadouts into open scene**.
- **Pink or missing sprites:** run **Cellstorm > Build > 1 - Prepare project**, then check that the Universal Render Pipeline asset is assigned in Project Settings > Graphics.
- **Keys do nothing:** make sure the Game view has focus, and that Project Settings > Player > Active Input Handling is **Input System Package (New)** or **Both**.
- **Slow import or odd errors after switching branches:** close Unity, delete `Library/`, and open the project again.

## Docs

| File | Content |
| --- | --- |
| `Docs/Cellstorm_-_Game_Design_Document__V0_1_1_.docx` | V0.1.1 game design |
| `Docs/Cellstorm_V0_1_1___Solo_Dev_Plan.docx` | V0.1.1 phases and estimates |
| `Docs/Cellstorm_-_Game_Design_Document__V0_2_.docx` | V0.2 game design |
| `Docs/Cellstorm_V0_2___Solo_Dev_Plan.docx` | V0.2 phases and estimates |
| `Docs/V0_1_1.txt` | V0.1.1 change notes |
