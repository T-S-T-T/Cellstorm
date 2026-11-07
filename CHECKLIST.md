# Cellstorm grand checklist (V0.1.1 + V0.2)

Tick each box as you go. If something fails, note the exact steps and the Console text and send it over.
Tools: the **debug panel** (press `` ` ``), the old debug keys (README) and **Cellstorm > Tests > Run self tests**.
Everything was written without compiling, so Part 0 comes first.

---

## Part 0 - One-time setup

### Git
- [ ] Tag the finished V0.1 commit: `git tag v0.1`
- [ ] Branch for the work: `git checkout -b v0.1.1`
- [ ] (Later) after Part A-B pass: commit, `git tag v0.1.1`, then `git checkout -b v0.2` and commit V0.2 there.

### Unity
- [ ] Open the project in 6000.6.0f1. **Console has 0 compile errors.** If not: send me the first error (fix, repeat).
- [ ] Delete `Assets/Scripts/Weapons/WeaponCell.cs` and `Assets/Scripts/Combat/DamageCalculator.cs` (stubs).
- [ ] Accept **Regenerate now** (or Cellstorm > Data > Regenerate data (V0.2)). Console says "data regenerated (0.2.0)". A `Backups/Data_<date>` folder exists.
- [ ] `Assets/Data/Weapons` has 18 assets (Ram, Jaw, Gun, Hardening, Whip, SpreadShot, SporeCloud, Mine, Minion, Convert, Shockwave, plus 7 Enemy* weapons).
- [ ] `Assets/Data/Enemies` has SingleCell, MultiCell, RangedCell, ExplodingCell, SupportCell; SpawnTable lists all 5.
- [ ] `Assets/Data/Loadouts` has Default, Speedy, Tank, Giant, Swarmer, Sporer.
- [ ] Cellstorm > Tests > Run self tests: **0 failed**.
- [ ] Open Game scene, press Play: no red errors in the Console. A yellow "Upgrade Pool ... before V0.2" warning must NOT appear after regenerating.
- [ ] Press `` ` ``: the debug panel opens. Misc tab > **Run V0.2 self-test**: shows "ALL PASSED".

---

## Part A - Quick smoke test (10 minutes)

- [ ] Main menu opens, loadout picker shows 6 loadouts (locked ones greyed with their unlock text).
- [ ] Start a run with Default: swarm moves with WASD, aims with the mouse, Ram on LMB dashes.
- [ ] Kill a few enemies, pick up EXP, level up: **6 options** appear.
- [ ] Die (or press L then N for a boss): the run summary shows score, time, milestone, weapons, evolutions.
- [ ] Back to the menu, start another run: no errors, HUD correct.

---

## Part B - V0.1.1 (HP model)

### B1 Health and stats core
- [ ] Player starts with 100 max HP, regen 1 HP/s (Swarmer 2). Health bar in the HUD fills and drains.
- [ ] Debug `U` (20 damage test hit) at DEF 2 loses about 20 x (1 - DEF reduction); `,` and `.` change DEF and the damage taken.
- [ ] DEF curve: DEF 1 ~ 5%, DEF 2 ~ 20%, DEF 5 ~ 40%, DEF 50 ~ 90% (cap). Self-test covers this.
- [ ] Speed: 1 unit/s per Speed point (Default 2, Speedy 5, Tank 1).
- [ ] Sizes are fixed: player radius 0.335 (Giant 0.67); changing HP with `1`-`5` or `=`/`-` never changes size.
- [ ] Giant loadout: double size, 200 max HP. Enemies' size is set once from their max HP and never changes.
- [ ] Enemy stats: Single 10 HP / 5 contact; Multi 100 HP / 10; Ranged 100 HP (gun); Exploding 10 HP (explodes). Bosses 500 HP x milestone.

### B2 Ram, movement, speed math
- [ ] Ram: dashes toward the cursor 0.3 s at 5x speed, 30 damage to anything touched once per dash, 1 s cooldown.
- [ ] Movement feels responsive; no sticking on enemies.

### B3 Soft-ball visuals
- [ ] The swarm is ONE soft ball that squashes when it moves or gets hit. Aim marker orbits it.
- [ ] Weapon cells merge visually into the ball. No flicker at the edge.

### B4 Weapon cells (Jaw, Gun, Hardening)
- [ ] Jaw: hold = a jaw forms (3 s shape), release bites a 90 degree cone, range 2, 150 damage. 3 s cooldown.
- [ ] Gun: hold = barrel forms, release fires 100 damage bullet. Bullet speed 2x.
- [ ] Hardening: DEF x2 for 3 s, 10 s cooldown.
- [ ] Taking damage while forming reduces held HP; invariant 0 <= held <= HP <= max holds (the invariant monitor logs violations; there should be none).
- [ ] Weapon cells have no colliders and never block movement.

### B5 HUD, level-up, loadouts
- [ ] HUD: health bar with held-HP highlight, four slot icons (LMB, RMB, Q, E) with cooldown, EXP bar, boss bar.
- [ ] Swap mode (F) swaps weapon/skill slots; pause (Esc) freezes timers.
- [ ] Level-up screen: grid of options, reroll button spends rerolls.

### B6 Balance bot and polish
- [ ] Debug overlay (F12) shows FPS and counters.
- [ ] Balance bot runs without errors (it only needs to not crash; numbers are not tuned).

---

## Part C - V0.2

### C1 Card system and level-up
- [ ] Each level-up offers **6** options (panel Misc: slider changes it to 1-12; "Show every eligible upgrade" lists them all).
- [ ] Every card shows a stack count (e.g. "2/5") and a tooltip with what the next stack does.
- [ ] Panel Cards tab lists every card; **+1** applies a stack. Check a few: Heavy Hits (+damage), Quick Hands (cooldown), Wide Reach, Tough Cells.
- [ ] Specific cards only appear when you own that weapon (e.g. Wide Jaw needs Jaw; Choke needs Spread Shot).
- [ ] Shared cards only affect the right tags (Piercing Rounds on Gun/Spread Shot, not on Jaw).
- [ ] Max stacks stop at the limit; a maxed card no longer shows up.
- [ ] Taking a weapon when slots are full asks which slot to replace; skills go to Q/E, weapons to LMB/RMB.
- [ ] Reroll never shows the identical set when enough alternatives exist.

### C2 Status effects (panel Status tab, apply to the player)
- [ ] Toxic 5 HP/s: green tint, HUD "T" icon with a pie ring, HP drops about 5/s minus DEF. Ends on time.
- [ ] Applying Toxic 15 while Toxic 5 is running **replaces** it; applying a weaker one only **refreshes** the time.
- [ ] Slow 40%: blue tint, "S" icon, speed drops 40%. Slow 95% never goes below 20% of normal speed.
- [ ] Stun 1.5 s: yellow flicker, "Z" icon, cannot move or use weapons, a forming Jaw/Gun is released.
- [ ] A second stun during or in the 1 s after a stun does nothing. After 1 s it works again.
- [ ] Shove moves the swarm about 3 units in 0.2 s and ignores DEF; Pull moves it toward the point.
- [ ] Clear all effects removes everything.
- [ ] On a boss (panel Spawn tab): slow and stun last half as long; shove distance halves.

### C3 New weapons (panel Items tab)
- [ ] **Whip**: hold 3 s, release lashes a 120 degree arc, range 3, 80 damage, pulls enemies 1.5 units toward you without pulling them through you.
- [ ] **Spread Shot**: 5 pellets in a 40 degree fan, 40 damage each, range 6; Extra Pellets / Choke cards change count and spread.
- [ ] **Spore Cloud** (press): cloud radius 2.5 for 4 s, Toxic 5 HP/s to enemies, no direct damage. Stays where dropped.
- [ ] **Mine** (press): arms after 1 s, explodes at 60 damage in 2 units when an enemy gets close; Extra Mines, Quick Fuse, Proximity work.
- [ ] Each new weapon icon/glyph appears in the HUD slot with its cooldown.
- [ ] Jaw cards: Wide Jaw (angle), Long Fangs (range), Second Bite (a second bite 0.3 s later).
- [ ] Gun cards: Rifled Barrel, Explosive Rounds.

### C4 Skills and Minions
- [ ] **Minion** (Q/E): each press spawns minions costing 2.5% of max HP **each**; HP is spent (visible in the health bar). Not usable if HP minus held HP would not cover it.
- [ ] Minions are small red-tinted swarms that chase the nearest enemy, deal contact damage (10 DPS), have the DEF you had, and can be killed. Dead minions do not return HP.
- [ ] Minions do not collide with you or each other and do not block enemies' contact with you.
- [ ] Cards: More Minions, Cheaper Minions (cost down, min 1%), Sharp Minions (damage), Sturdy Minions (HP +25% per stack).
- [ ] **Convert** (5 s, 15 s cooldown): enemies you damage drop heal orbs worth 10% of the damage (max one per enemy per 0.5 s); orbs vanish after 10 s; Rich Orbs / Longer Convert cards work.
- [ ] **Shockwave** (12 s cooldown): ring radius 5, enemies pushed 4 units and stunned 1 s, no damage; Wider Wave / Hard Shove / Long Stun work. Bosses are shoved half as far.
- [ ] **Hardening** cards: Longer Hardening, Stronger Hardening.
- [ ] Enemies target minions as well as you (the nearest valid target); retargeting looks smooth (about 4 times per second).

### C5 Evolutions (panel Cards tab / real runs)
Each evolution is once per run, shows an "EVOLUTION" tag and its requirement text, and cannot be taken while locked out.
- [ ] **Tank**: 5 Dense Cells. DEF x5, Max HP and regen upgrades half as effective afterwards, locks out Swarm.
- [ ] **Swarm**: Minion plus 3 Minion cards. Each activation spawns 2 more minions, minion contact x2, max HP x0.8, locks out Tank.
- [ ] **Toxic**: 4 Toxic Tips. All Toxic you apply is 2x stronger and 50% longer; a poisoned enemy that dies spreads Toxic within 2 units.
- [ ] **Hunter**: Gun or Spread Shot plus 4 Projectile cards. Projectiles home in, +50% damage, pierce +1, cell weapon deploy time x1.3, locks out Artillery.
- [ ] **Artillery**: a defeated boss plus Gun, Spread Shot or Mine. Explosions +50% size and damage, Gun bullets explode, cooldowns x1.2, locks out Hunter.
- [ ] The run summary lists the evolutions taken.

### C6 Enemies and bosses
- [ ] **Support Cell** appears from its start time: stands about 8 units away, heals other (non-support) enemies within 4 units by 5%/s (bosses 1%), green ring visible. Killing it is worth score/EXP like others.
- [ ] Panel Spawn tab: spawn each enemy type; they behave (chase, ranged keeps distance, exploding explodes, support heals and stands off).
- [ ] Bosses: spawn Boss N with each weapon (Ram, Jaw, Gun, Whip, Spread Shot, Spore Cloud, Mine). Each uses its weapon correctly: whip pulls, shotgun fans, cloud applies Toxic 3 HP/s to you and your minions, mine hurts only you and your minions, never other enemies.
- [ ] Bosses never exceed one at a time per milestone; boss HP bar shows; defeating one gives its reward and counts toward achievements.
- [ ] Enemy cap: at most 50 non-boss enemies alive (guards do not count toward it).

### C7 World: resource points
- [ ] First point appears at 90 s, then every 90 s, up to 2 active, only if fewer than 40 enemies are alive (retry every 10 s).
- [ ] Off-screen arrow points to each active point (HUD), with remaining time.
- [ ] Approaching (about 8 units) wakes the guards; guards leash to about 3 units from the point, never despawn by distance, and are not counted in the 50-enemy cap.
- [ ] Kinds: Group (many basic guards), Elite (few strong), Mixed. Panel World tab spawns each.
- [ ] Defeating all guards captures it: reward drops (EXP burst, Nutrient heals 30% max HP, Adrenaline Speed +2 for 10 s with refresh, Reroll). Panel "Capture nearest point" does the same.
- [ ] A point expires after 180 s without a capture, with no reward.

### C8 World: hazards
- [ ] Hazard zones fade in over 2 s (harmless while fading), stay ~20 s, then fade out 2 s. Interval shrinks with milestone (45 s down to 20 s), max 3 active.
- [ ] **Toxic zone**: Toxic 5 HP/s while inside (reduced by DEF). At DEF 50+ you are immune.
- [ ] **Current**: pushes everything inside (3 units/s) in its direction; works on enemies, minions, bosses; does not affect weapon cells' visuals badly.
- [ ] Enemies killed **only** by a hazard give no score, EXP or drops (bosses excepted).
- [ ] Panel "Toxic zone ON the player" / "Current ON the player" spawn hazards at your feet.

### C9 Unlocks, achievements, save
- [ ] Main menu > Achievements lists all 15 unlocks with locked/unlocked state and progress text.
- [ ] Score unlocks: Jaw 100, Hardening 150, Gun 200, Spread Shot 300, Minion 400, Spore Cloud 600, Shockwave 800, Swarmer loadout 2000, Speedy 3000, Giant 5000, Tank 10000 (lifetime score).
- [ ] Achievements: Whip (defeat Boss 2), Mine (200 enemies in one run), Convert (defeat Boss 3), Sporer loadout (reach milestone 4 / 20:00). They unlock when the run ends, **including on defeat**, and show on the run summary.
- [ ] Locked weapons never appear in level-up; locked loadouts cannot be picked. F11 / panel "Treat everything as unlocked" overrides this for testing.
- [ ] Old save file (version 2) loads without errors and upgrades to version 3. Delete the save to test a fresh start.
- [ ] Swarmer loadout starts with the Minion skill and regen 2; Sporer starts with Spore Cloud and Speed 3.

### C10 Regression (do after C1-C9)
- [ ] Ram, Jaw, Gun, Hardening still behave exactly as in Part B.
- [ ] Pause, swap mode, level-up screen and boss warnings do not break timers (status effects freeze while paused).
- [ ] Defeat screen, victory screen, endless mode (after the last boss), "New run" and "Main menu" all work, twice in a row without restarting Unity.
- [ ] Leaderboard shows new runs with their weapons.
- [ ] Release-style check: Cellstorm > Build > 2 builds without errors, and the build has no debug panel.

---

## Part D - Performance gates

- [ ] 50 enemies + 2 bosses + 3 hazards + 2 points: stable FPS (target 60 on your machine).
- [ ] 30+ minions alive (Swarm evolution, spam Minion): FPS drop is acceptable; note the number where it gets bad.
- [ ] 20 minutes of play: no steady frame-rate loss, no memory climb in the Profiler, no growing object counts (Minion.Count, Enemy count, mines, orbs return to normal).
- [ ] No red errors or per-frame warnings in the Console during a full run.

---

## Part E - Definition of done

- [ ] Parts 0, A, B, C, D all ticked, or each failure noted and fixed.
- [ ] A full run to Boss 5 or endless with a build of your choice (e.g. Swarm evolution + Minion cards, then Toxic/Spore Cloud) finishes without errors.
- [ ] Everything committed: `git tag v0.1.1`, then V0.2 on its own branch.

---

## Deliberate decisions and deviations (so you do not report them as bugs)

- **Minion cost** needs `HP - held HP > total cost`; it spends current HP (never below 1) and does not return on minion death.
- **Hazard-only kills** give no reward (score, EXP, drops), except bosses.
- **Bosses** take half the stun/slow duration and half the shove distance.
- **Convert**: max one HP orb per enemy per 0.5 s, orbs last 10 s.
- **Runtime fallbacks** keep the game running with old assets (built-in upgrade list, support cell appended to the spawn table, boss weapon pool merged, missing loadouts added). They log a warning; regenerate data to remove it.
- **Endless mode**: DEF scaling for enemies starts at milestone 7.
- **Numbers are placeholders**: every threshold, cooldown, cost and rate lives in the `*Defaults.cs` files and `CardCatalog.cs` for tuning after the test pass.
- The 'F' keys F1-F12 are all used by the old debug tools, so the V0.2 panel uses the backquote key.
