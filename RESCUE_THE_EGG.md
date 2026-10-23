# Rescue the Egg — first playable test

Open the village and approach the new **Rescue the egg** gate, then enter. You can also use **Little Paws > Open Rescue the egg** in Unity and press Play.

## Levels and heroes

Level 1 is playable. Levels 2 and 3 are locked and marked **Coming Soon**. These level slots are separate from the three combat stages inside Level 1.

Choose either humanoid bird guardian before starting:

- **Rooster:** golden feathers, prominent crimson comb, silver helmet, chest plate, forest tunic, armored greaves/boots, iron sword.
- **Hen:** ivory feathers, smaller rose comb, silver helmet, chest plate, teal tunic, armored greaves/boots, iron sword.

Both have the same combat stats. The hero is approximately one fifth of the available landscape screen height, and limbs, torso, sword and boots animate independently. Original character preview PNGs and actual Unity scene screenshots are in `Exports/RescueDesigns/`.

## Controls and combat

Use the touch direction pad or WASD/arrow keys to walk. Up/down preserve the last horizontal facing. Press FIGHT or Space for **one swing per press**; holding it does not auto-repeat. The first swing travels left, the next right, alternating thereafter. A swing only hits a vulnerable enemy on the facing side, within one space horizontally and vertically, including exact overlap.

Each tap removes **1 HP** from a **30 HP** roach. A hit refreshes its **0.5-second damage state** and pushes it away from the hero's facing direction. The enemy cannot attack or patrol during that state. The hero can advance horizontally during a combo; vertical movement is blocked until damage mode ends. Afterwards the enemy lies down, invulnerable, for **2 seconds**. Its HP appears only after the first hit.

Roaches carry swords, chase within five spaces, and otherwise patrol a five-unit horizontal route inside the stage. Their attacks deal **20 HP** after a **0.2-second sword windup**, with a one-second attack interval to prevent frame-by-frame damage. Hitting a roach cancels its pending attack and sword animation, so it cannot damage the hero during damage mode or while lying down. Its one-second attack cooldown restarts on a hit and stays paused until it recovers. Knockback can push them beyond the hero's stage bounds; after recovering, they return inside, then chase or return to patrol.

The hero starts with **50 HP**. Zero HP or expiration of the **2-hour active-play timer** ends the round. Pause or losing app focus pauses the timer. Restart begins a new round; this test does not add authentication or cloud saving.

## Stage progression and scale

Each of the three stretches is **15 world units**. This is the consistent gameplay equivalent of the requested 15 cm; physical centimetres vary by screen size and cannot be guaranteed across phones/tablets. Hero x is clamped to the current stretch until every enemy is defeated. Walking against the left boundary still animates the hero. A bouncing **GO RIGHT** sign appears when the stretch is cleared; physically cross its right edge to enter the next stage. Cross the third cleared stage to rescue the egg.

For this first test, **one roach appears per stage**, including Stage 1. The planned three-roach Stage 1 is intentionally reduced to one to match the request to test a single enemy first.

## Validation

- **Little Paws > Run Rescue the egg checks**: deterministic combat, stage, bounds, patrol, overlap, stun, knockback, invulnerability, health, attack timing, and long-running timer checks.
- **Little Paws > Run Rescue scene checks**: actual Unity scene/UI checks, level locks, hero selection, initial health, single-enemy spawning, hit/down/GO visuals, stage transition, and rendered screenshots.
- **Little Paws > Export Rescue character designs**: export the actual runtime art as PNGs.

Implementation: `Assets/LittlePaws/Scripts/Rescue/`. No external assets or backend services are required.

## Approved hero artwork

The selected armored rooster and helmeted hen now live in `Assets/LittlePaws/Resources/LittlePaws/Rescue/Heroes/` as `RoosterKnight.png` and `HenKnight.png`. Both have transparent backgrounds and Unity Sprite imports with point filtering, no mipmaps, and no compression.

The selector and gameplay use these exact character assets. `RescueHeroGraphic` adds a small 2D mesh rig for foot/body movement and alternating sword swings; horizontal facing mirrors the entire hero, including the egg shield. This is procedural mesh animation of the approved single-pose images, rather than a hand-drawn frame-by-frame animation sheet. The old procedural art remains a fallback if an asset is missing.
