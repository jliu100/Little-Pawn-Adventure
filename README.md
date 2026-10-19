# Little Paws Adventure

A landscape-only single-player pet adventure for iOS and Android, with a 3D home and planned 2D minigames.

## Explore the 3D village map

1. Stop Play mode and let Unity import the new files.
2. Select **Little Paws → Open Village Map**, or open `Assets/Scenes/TownMap.unity`.
3. Press **Play**. The map is generated at runtime.
4. Tap clear ground to walk. Tap a building, its floating name label, or a destination button to walk to its entrance.
5. When you arrive, a location card appears. At **Home**, choose **Enter home** to open the existing pet-care room. Inside the room, use **Village map** to return.

| Destination | 3D design |
| --- | --- |
| Home | Humble timber cottage, repaired terracotta roof, chimney, and stepping stone |
| Weapon | Stone forge with slate roof, furnace, anvil, and crossed-sword sign |
| Hospital | Ivory clinic with mint trim, a green cross, and a sheltered doorway |
| Food store | Colorful shop with a striped awning and produce crates |
| Adventure | Stone gate with stars and a trail leading toward future minigames |

The five places surround an open paw-print plaza, with connected sandstone paths, trees, shrubs, and flowers. Both scenes stay landscape-only and share the selected pet and saved cleanliness.

**Scope:** map geometry, navigation, location cards, and home entry are implemented. Weapon purchases/combat, medical treatment, food shopping, and playable adventures are not yet implemented. This is a map design and navigation prototype.

The village is now the first enabled build scene; the home is also included. To test only the room, use **Open Pet Home**.

Select **Little Paws → Run village map checks** for Play-mode checks of five destinations, floor/building taps, and arrival at each entrance. Results are written to `Library/LittlePawsChecks/VillageResults.txt`. These live-editor checks have been compiled but have not been run in this session.

## Play the home prototype

1. Open this project in Unity **6000.6.6f1**, and allow import/compilation to finish.
2. Select **Little Paws → Open Pet Home**, or open `Assets/Scenes/PetHome.unity`.
3. Press **Play**. The room and characters are generated at runtime; the full home appears in Play mode.
4. Choose **Kitten** (Miso) or **Puppy** (Mochi). Tap the pet to show Health, Clean, and Hunger bars immediately, without washing.
5. Tap/click an empty spot on the floor to walk there. A gold ring marks the destination. Tap another clear floor spot to change the route. Taps on furniture, walls, the pet, UI, and outside the floor are ignored. Walking paths keep clearance from furniture.
6. Press and hold the pink soap in the lower-left care tray, then rub it back and forth over the pet. Bubbles appear during scrubbing. After **2.5 seconds of accumulated rubbing**, the Health, Clean, and Hunger bars appear. Release to return the soap to its tray. Pauses in rubbing do not count toward the timer.

### Implemented in this prototype

- Landscape left/right rotation only; portrait is disabled in Player Settings and at runtime.
- Original procedural kitten and small puppy models with expressive faces, ears, tails, collars, idle motion, and four-leg walking animation. Pets face the camera on arrival and while resting or washing, with a gentle upward head tilt; they face the travel direction while walking.
- A simple, modest home with worn plank flooring, patched plaster, boxes, a floor mat, and bowls. The bed and table have been removed for now.
- Floor-only tap-to-walk with paths around furniture, pet-sized obstacle clearance, and a destination marker. No arrow controls.
- A generated 2D soap icon with mouse/touch dragging and animated 3D bubbles.
- Status bars revealed by tapping the pet or after washing. Cleanliness increases with rubbing. **Health (82%) and Hunger (58%) are demonstration values**, not a complete needs simulation. Higher Hunger means more hungry.
- Pet choice and each species' cleanliness save locally using PlayerPrefs. Position and full care history are not saved yet. Changing species resets the current bath timer and hides the bars until another pet tap or bath.
- A landscape UI that respects screen safe areas. Desktop default resolution: 1440 × 810.

### Movement initialization

The room is generated at runtime. Unity physics transform auto-sync is disabled in this project, so the controller explicitly synchronizes collider transforms **before** taking the navigation bounds snapshot and before floor raycasts. This prevents default collider positions at startup from incorrectly blocking the pet's spawn. Restart Play mode after importing movement changes so the navigation map is rebuilt.

### Verification and limitations

The runtime and editor scripts compile against the installed Unity compiler and the project's actual library references. Eighteen executable navigation checks passed, including five town entrance routes and rejection of all five building footprints. The original checks cover: direct routes, floor-edge clearance, off-floor rejection, furniture rejection, furniture detours, segment clearance, mid-route retargeting, and unreachable destinations. Visual and interactive Play-mode validation remains pending: the project was already open in another Unity instance, and the UI automation connection timed out.

Select **Little Paws → Run play-mode smoke checks** to check accepted floor taps, rejected off-floor/furniture/UI taps, arrival, soap timing, status reveal, companion switching, and tapping either species to reveal status without starting movement. Results appear in `Library/LittlePawsChecks/Results.txt`. The check restores the saved species and cleanliness afterward. This check has been compiled but has not yet been executed in the live editor.

Test on an iPhone and Android phone before treating orientation, touch controls, layout, and performance as device-verified. Models use primitive meshes; optimization, production art, and audio remain future work. No ads or purchases are integrated.

### Project setup and files

The project uses the official Universal 3D template with URP 17.6.0, Universal Renderer assets, and the Input System. `TownMap.unity` and `PetHome.unity` are the enabled build scenes. The unused template scene has been removed. An existing Git repository was detected. This implementation does not create commits.

- `Assets/LittlePaws/Scripts/PetHomeGame.cs`: movement, UI, soap, bubbles, status, and local saving.
- `Assets/LittlePaws/Scripts/PetVisual.cs`: kitten/puppy geometry and animation.
- `Assets/LittlePaws/Scripts/TownVisual.cs`: village geometry and five landmarks.
- `Assets/LittlePaws/Scripts/TownLocation.cs`: destination metadata and entrances.
- `Assets/LittlePaws/Scripts/RuntimeVisualResources.cs`: generated asset cleanup when changing scenes.
- `Assets/LittlePaws/Scripts/HomeVisual.cs`: home geometry and floor/furniture hit detection.
- `Assets/LittlePaws/Scripts/HomeFloorSurface.cs`: marks the only collider accepted as a walking surface.
- `Assets/LittlePaws/Scripts/FloorPathfinder.cs`: floor bounds, obstacle clearance, and path planning.
- `Assets/LittlePaws/Editor/PetHomeTools.cs`: scene menu and play-mode smoke checks.
- `Assets/LittlePaws/Resources/LittlePaws/`: material references retaining procedural shaders for builds.

Evolution, feeding, exercise, hospital interactions, minigames, expansion beyond the starter village, and monetization remain planned features described below.

## Game vision

**Little Paws Adventure** is a single-player pet-care and exploration game for iOS and Android. Players choose a dog or cat, develop a bond through everyday care, and accompany it through three evolution stages toward a human-like character that keeps its animal traits.

The home uses **3D** characters and surroundings. The minigames use **2D** gameplay in separate scenes within the same Unity project. Pet progression and rewards should carry between the home and minigames. The starter village map now uses stylized 3D landmarks and an elevated camera.

This section records the broader design. Only features listed under “Implemented in this prototype” currently exist.

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

The 3D home is the starting area. The prototype uses a fixed camera looking down at an angle and landscape-only orientation. Further camera controls remain undecided.

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
- Final art direction and any additional camera behavior (landscape-only orientation is selected).
- How the wider map connects to the 3D home and 2D minigames.
- Whether the weapon idea involves combat or equipment for challenges.
- Exact evolution requirements and offline care rules.
- Real-world versus in-game yearly doctor visits.
- Confirmation of Maze/Puzzle stage counts and unlock conditions.
- Puzzle mechanics and Find Stars stage count.
- Purchase products, ad placements, and reward balancing.

## Mobile

Install Android Build Support (including SDK/NDK and OpenJDK) and iOS Build Support via Unity Hub. Local iOS builds require Xcode. Configure your own company name, unique application identifiers, signing, orientation, and store settings before publishing.
