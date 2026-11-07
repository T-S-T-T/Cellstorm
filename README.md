# Cellstorm

Cellstorm is a physics-based 2D swarm survival roguelike where you evolve a colony of cells, build powerful mutations, and survive a hostile petri dish.

- **Engine:** Unity 6 (6000.6.0f1), 2D, Universal Render Pipeline, Input System 1.20.1
- **Platforms:** Windows, macOS and Linux desktop (keyboard and mouse)
- **Design documents:** [`Docs/`](Docs) (GDDs and solo dev plans for V0.1.1 and V0.2)
- **Test plan:** [`CHECKLIST.md`](CHECKLIST.md) (the grand checklist for V0.1.1 and V0.2)

## Status

| Version | State |
| --- | --- |
| V0.1 | Finished (cell-count model). Tag it as `v0.1`. |
| V0.1.1 | Code complete (HP model, Ram/Jaw/Gun/Hardening, soft-ball visuals, weapon cells, HUD, balance bot). Needs your test pass. |
| V0.2 | Code complete (all phases): card system, status effects, new weapons and skills, Minions, Support Cell, resource points, hazards, 6 level-up options, unlocks and achievements. Needs your test pass and compile check. |

Everything was written without compiling in Unity. Expect a few compile errors on first import: paste the first error from the Console and it gets fixed.

## Setup

### 1. Install

1. Install [Unity Hub](https://unity.com/download).
2. In the Hub, install editor **6000.6.0f1** (the exact version is in `ProjectSettings/ProjectVersion.txt`).
3. Install an IDE with Unity support: Visual Studio 2022 with the **Game development with Unity** workload (see `.vsconfig`), or JetBrains Rider.

### 2. Get the project

```
git clone <your repository url> Cellstorm
```

Or use the folder you already have (`F:\Proj\Unity\Cellstorm`).

### 3. Open it

1. Unity Hub > **Add** > **Add project from disk** > pick the `Cellstorm` folder, open with 6000.6.0f1. Packages restore from `Packages/manifest.json`.
2. If Unity enters Safe Mode there is a compile error: fix it first (see Troubleshooting).
3. **Delete the two empty stub files** `Assets/Scripts/Weapons/WeaponCell.cs` and `Assets/Scripts/Combat/DamageCalculator.cs` (with their `.meta` files) in the Project window. They are harmless but obsolete.
4. **Regenerate the data (once for V0.2):** a dialog says "Cellstorm data is out of date". Click **Regenerate now**, or run **Cellstorm > Data > Regenerate data (V0.2)**. It copies `Assets/Data` to `Backups/` first, then rebuilds all weapons (18), enemy types (5, including the Support Cell), loadouts (6, including Swarmer and Sporer), upgrades (cards and evolutions) and the upgrade pool in place (file names and GUIDs stay), and re-links the loadouts into the Game scene. Until you do this the game still runs from built-in defaults (the Console warns), but the assets in `Assets/Data` stay old.
5. Run **Cellstorm > Tests > Run self tests**. The Console should say `0 failed`.

### 4. Play

1. Open `Assets/Scenes/Boot.unity` (it loads the main menu) and press **Play**.
2. Or open `Assets/Scenes/Game.unity` directly, press Play, pick a loadout.

The V0.2 world systems (resource points, hazards, off-screen arrows) are added at runtime by `WorldBootstrap`, so no scene edits are needed.

## Controls

| Action | Input |
| --- | --- |
| Move | WASD |
| Aim | Mouse |
| Weapons | Left mouse button, right mouse button (hold to form a cell weapon, release to use) |
| Skills | Q, E |
| Swap weapon and skill slots | F |
| Pause | Esc |

The bindings live in `Assets/Resources/CellstormInput`.

## What is in V0.2

- **Weapons (LMB/RMB):** Ram, Jaw, Gun (V0.1.1) and Whip, Spread Shot, Spore Cloud, Mine.
- **Skills (Q/E):** Hardening, Minion, Convert, Shockwave.
- **Level-up:** 6 options per level, stack-based cards that target one weapon, a group of tags (Cell, Projectile, Melee, Area, Damage, Skill) or everything. Each card shows its stack count and a tooltip.
- **Evolutions (once per run):** Tank, Swarm, Toxic, Hunter, Artillery, each with requirements and lock-outs (see the in-game tooltip).
- **Status effects:** Toxic (damage over time, DEF applies), Slow (never below 20% speed), Stun (releases cell weapons, 1 s immunity afterwards), knockback and pull. Bosses halve stun/slow durations and shove distances.
- **Enemies:** Support Cell (heals other enemies, stands back), enemies of every type can carry the new boss weapons.
- **World:** Resource points (guarded spots, capture for EXP, Nutrient, Adrenaline or rerolls) and hazard zones (Toxic zone, Current). Hazards ignore you at DEF 50 or more.
- **Meta:** 15 unlocks. Score thresholds plus achievements (Whip, Mine, Convert, Sporer loadout). An Achievements view is in the main menu. Save version 3.

## Debug tools

| Key | Effect |
| --- | --- |
| **`** (backquote) | **V0.2 debug panel**: give any weapon or skill, add card stacks, apply Toxic/Slow/Stun, spawn any enemy or any boss with any weapon, resource points, hazards, pickups, set options per level-up, run the V0.2 self-test |
| F12 | Show or hide the debug overlay |
| F11 | Treat everything as unlocked |
| G | God mode |
| Y | DEF +5 |
| L | Defeat all enemies |
| N / J / E | Next boss / +60 s / endless mode |
| 1-5 | Max HP 20 / 100 / 500 / 2000 / 10000 |
| = / - | Double / halve max HP |
| [ / ] | Body size x1 / x2 (Giant) |
| Z / X / C / V | Spawn test enemies |
| U | Take a 20 damage test hit |
| H | Heal to full |
| , / . | Base DEF -1 / +1 |
| 6 / 7 / 8 / 9 / 0 | Equip Ram / Jaw / Gun / Hardening / clear slots (Shift for the second slot) |
| I / O / P | Spawn a ranged / jaw / ram enemy |
| F6-F10 | Level up, reroll, shells, EXP orbs, Dense Cells x5 |

Debug code only exists in the editor and development builds (`DebugGate`). Release builds remove it.

## Project layout

```
Assets/
  Scenes/       Boot, MainMenu, Game
  Scripts/
    Combat/       SwarmStats (HP model), StatusController, ITargetable/TargetingService, IKnockbackable, DefCurve, DamageInfo
    Swarm/        SwarmBody (player), SwarmVisual, CircleSwarmVisual, MetaballLayer, FxSprites, CameraFollow
    Enemies/      Enemy, EnemySpec, EnemySpawner (guards, boss pool)
    Weapons/      AbilityController, Ram/Jaw/Gun/Whip/SpreadShot/SporeCloud/Mine, Hardening/Minion/Convert/Shockwave, UpgradeMods, Minion, Bullet
    World/        WorldBootstrap, ResourcePoint(+Director,+Hud), HazardPatch, HazardDirector
    Progression/  PlayerBuild (cards, evolutions), PlayerProgress, RunSetup, Pickup
    Data/         ScriptableObject types and code defaults (CardTypes, CardCatalog, WeaponDefaults, EnemyDefaults, LoadoutDefaults)
    Meta/         Save data/service, UnlockCatalog, run recorder
    Core/         GameManager, RunDirector, input, scene loading
    UI/           IMGUI screens (HUD, level-up, menus, summary, leaderboard, achievements)
    Debug/        Debug tools, V02DebugPanel, V02SelfTest, RuntimeSelfTest, BalanceBot
  Data/         Generated ScriptableObject assets (Weapons, Enemies, Loadouts, Upgrades)
  Editor/       Setup menus, BuildTools, RegenerateData, SelfTests
  Resources/    CellstormInput (input actions)
Docs/           GDDs and dev plans
Backups/        Created by Regenerate data (not part of the build)
```

Balance numbers live in the `*Defaults.cs` / `CardCatalog.cs` files. To change one for good, edit it there and run **Regenerate data**, or edit the asset in the Inspector (Regenerate overwrites assets, so do one or the other).

## Editor menus

| Menu | What it does |
| --- | --- |
| Cellstorm > Data > Regenerate data (V0.2) | Rebuilds the data assets in place (with a backup) |
| Cellstorm > Data > Wire loadouts into open scene | Re-links the loadout list of the open Game scene |
| Cellstorm > Tests > Run self tests | Checks the DEF curve, Speed math, sizes and HP rules |
| Cellstorm > Build > 1 - Prepare project | Scenes in build settings, shaders, window settings |
| Cellstorm > Build > 2 - Build for the current platform | Makes a build in `Builds/` |

## Git workflow

```
git tag v0.1                 # on the commit that is the finished V0.1
git checkout -b v0.1.1
git add -A && git commit -m "V0.1.1 complete"
git tag v0.1.1
git checkout -b v0.2
git add -A && git commit -m "V0.2: cards, status effects, weapons, skills, world"
```

`.gitignore` already excludes `Library/`, `Temp/`, `Logs/`, `Builds/` and user settings. Commit the `.meta` files. Add `Backups/` to `.gitignore` if you do not want them in the repository.

## Save file

`cellstorm_save.json` in `Application.persistentDataPath` (Windows: `%USERPROFILE%\AppData\LocalLow\<company>\Cellstorm`). Save version 3 adds boss and evolution records for achievements; older saves load and upgrade automatically. Delete the file to reset progress.

## Troubleshooting

- **Compile errors:** read the first one in the Console and send it over. A missing type usually means a file did not copy.
- **Console warns "Upgrade Pool asset is missing or from before V0.2":** run **Cellstorm > Data > Regenerate data (V0.2)**.
- **"Data is out of date" keeps appearing:** `Assets/Data/data_version.txt` is written at the end of Regenerate. If the run failed the Console shows why.
- **Swarmer/Sporer loadout missing in the picker:** open `Game.unity` and run **Cellstorm > Data > Wire loadouts into open scene**.
- **No resource points or hazards:** they start after 90 s (points) and at the first hazard interval. Use the debug panel World tab to spawn them now.
- **Pink or missing sprites:** run **Cellstorm > Build > 1 - Prepare project**; check the URP asset in Project Settings > Graphics.
- **Keys do nothing:** the Game view needs focus; Project Settings > Player > Active Input Handling must be **Input System Package (New)** or **Both**.
- **Odd errors after switching branches:** close Unity, delete `Library/`, reopen.

## Docs

| File | Content |
| --- | --- |
| `Docs/Cellstorm_-_Game_Design_Document__V0_1_1_.docx` | V0.1.1 game design |
| `Docs/Cellstorm_V0_1_1___Solo_Dev_Plan.docx` | V0.1.1 phases and estimates |
| `Docs/Cellstorm_-_Game_Design_Document__V0_2_.docx` | V0.2 game design |
| `Docs/Cellstorm_V0_2___Solo_Dev_Plan.docx` | V0.2 phases and estimates |
| `CHECKLIST.md` | Grand test checklist |
