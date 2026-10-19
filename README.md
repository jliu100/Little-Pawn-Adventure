# Little Paws Adventure

A new Unity 3D project for a single-player pet adventure planned for iOS and Android.

## Game vision

**Little Paws Adventure** is a single-player pet-care and exploration game for iOS and Android. Players choose a dog or cat, develop a bond through everyday care, and accompany it through three evolution stages toward a human-like character that keeps its animal traits.

The home uses **3D** characters and surroundings. The minigames use **2D** gameplay in separate scenes within the same Unity project. Pet progression and rewards should carry between the home and minigames. The visual treatment of the wider map is still to be decided.

All features below are planned ideas, not implemented gameplay in the current template.

### Pet choice and evolution

- Start with a dog or cat companion.
- Include three evolution stages in total.
- Gradually evolve the pet toward a human-like character while retaining recognizable dog/cat features such as ears and a tail.
- Keep the companion recognizable throughout its evolution.

Proposed stage design (not final):

| Stage | Appearance |
| --- | --- |
| 1 | Animal companion: a small dog or cat |
| 2 | Transitional companion: partly human with strong animal features |
| 3 | Human-like companion retaining dog/cat traits |

Exact evolution requirements, timing, character designs, and whether evolution is reversible remain undecided. Combining care milestones with minigame achievements is a proposed progression model.

### Daily care and health

Players should be able to:

- Give their pet food daily.
- Bathe their pet.
- Exercise with their pet.
- Visit a hospital and attend a yearly doctor checkup.

The meaning of a year (real-world time or in-game time), offline care behavior, care meters, and missed-day consequences still need design decisions. Gentle care without permanent punishment for missing a day is a recommendation, not an agreed rule.

### Exploration and movement

- Single-player first; multiplayer is outside the current scope.
- Tap an empty, reachable location to make the pet walk there.
- Route the pet around furniture, walls, and other obstacles.
- Explore a large map with locations and minigame entrances.

The 3D home is the starting area. A fixed camera looking down at an angle is a suggested mobile-friendly design; camera controls and orientation are not finalized.

### World locations

| Location | Intended purpose |
| --- | --- |
| Home (3D) | Spend time with the pet and perform care activities |
| Hospital | Health visits and yearly checkups |
| Weapon/equipment location | Original weapon idea; shop, tools, and any combat role need clarification |
| Escape Maze entrance | Enter the first 2D minigame |
| Puzzle entrance | Enter the second 2D minigame once unlocked |
| Find Stars entrance | Enter the third 2D minigame once unlocked |

An exercise park and home decoration are optional expansion ideas. The large world can be developed as connected areas instead of building every location at once.

### 2D minigames and progression

| Game | Planned experience | Content and unlock plan |
| --- | --- | --- |
| Game 1: Escape Maze | Navigate a maze and reach its exit | Start with 30 stages; expand to 50 |
| Game 2: Puzzle | A puzzle game; exact mechanics undecided | Unlock after completing Maze stage 30; provisionally expand to 50 stages |
| Game 3: Find Stars | Find stars in explorable scenes | Provisionally unlock after completing 50 Maze stages and 50 Puzzle stages |

**Stage counts and unlock conditions remain provisional.** The interpretation above is that Maze stage 30 unlocks Puzzle, and completing 50 stages in each unlocks Find Stars. Confirm this before creating full level sets. Find Stars does not yet have a stage count.

Block-pushing puzzles and hidden-object star scenes are possible mechanics, not finalized requirements.

### Core gameplay loop (proposed)

Care for the pet → explore → play minigames → earn rewards → develop the companion → unlock more activities.

Reward types, the currency economy, and how care contributes to evolution still need balancing. Saving should preserve pet identity, evolution, care, unlocked activities, and minigame completion between play sessions.

## Monetization — later phase

The selected revenue model is **in-app purchases plus ads**, introduced after the core game is enjoyable and reliable.

Possible offerings, subject to design and testing:

- Purchases: outfits, accessories, home decorations, and cosmetic themes.
- Optional rewarded ads: hints, an extra attempt, or bonus rewards after a minigame.
- An ad-removal purchase if the final design includes automatic ads.

No monetization SDKs, live purchases, ads, or prices are configured. Basic care and normal progression remaining available through gameplay is a proposed design principle. Decide the target age group before choosing ads, analytics, and purchase flows.

## Suggested development milestones

1. **3D home prototype:** ground, a small home area, one placeholder pet, camera, and obstacle-aware tap-to-walk movement.
2. **Care and saving:** feeding, bathing, exercise, and persistent local progress.
3. **Pet evolution:** dog/cat selection, three evolution appearances, animations, and agreed progression rules.
4. **First 2D minigame:** one Maze stage with home-to-minigame transitions and shared rewards.
5. **First release content:** polish the home, add a tutorial, and build the first 30 Maze stages.
6. **Mobile validation:** test iOS and Android touch controls, performance, screen layouts, and save reliability.
7. **Monetization:** implement purchases and optional ads, then verify them in store test environments.
8. **Expansion:** grow the map, add Maze stages 31–50, develop Puzzle, and later add Find Stars.

This is an implementation sequence, not a delivery schedule. Scope and estimates should be revisited after the first playable prototype.

## Open design decisions

- Target age group and intended audience.
- Art direction, camera behavior, and phone orientation.
- How the wider map connects to the 3D home and 2D minigames.
- Whether the weapon idea involves combat or equipment for challenges.
- Exact evolution requirements and offline care rules.
- Real-world versus in-game yearly doctor visits.
- Confirmation of Maze/Puzzle stage counts and unlock conditions.
- Puzzle mechanics and Find Stars stage count.
- Purchase products, ad placements, and reward balancing.

## Mobile

Install Android Build Support (including SDK/NDK and OpenJDK) and iOS Build Support via Unity Hub. Local iOS builds require Xcode. Configure your own company name, unique application identifiers, signing, orientation, and store settings before publishing.
