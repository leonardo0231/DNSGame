# DNS Game — Defense of Majnoon Island

> A third-person tactical defense game set during the defense of Majnoon Island.

`DNS Game` is an in-development Unity project focused on a responsive infantry combat loop: moving through a contested environment, taking cover, engaging hostile patrols, and reacting to group alerts.

## Project vision

The game aims to present a compact, replayable combat experience in which positioning matters as much as aiming. The player controls a soldier who must navigate the battlefield, use cover intelligently, and survive escalating enemy encounters while defending key areas of the island.

The initial version prioritizes a strong playable vertical slice over a large map or a long campaign.

## Core gameplay

- Third-person movement and camera control
- Soldier locomotion: walk, run, crouch, aim, and shoot
- Cover-aware combat
- Enemy patrol routes and target detection
- Group-alert behaviour: enemies can notify nearby allies after spotting the player
- Combat encounters built around defensive positioning
- Health, damage, death, and restart loop

## Planned gameplay loop

1. Enter a defensive combat area.
2. Scout patrol routes and choose a safe position.
3. Use movement, cover, and weapons to engage hostile soldiers.
4. React when enemies detect the player and alert their squad.
5. Secure the area or survive the encounter to advance.

## Technical direction

| Area | Initial approach |
| --- | --- |
| Engine | Unity |
| Primary language | C# |
| Camera | Third-person follow / aim camera |
| Player | Modular soldier controller and combat components |
| AI | Patrol, detection, combat states, and local group alerts |
| Level design | Small combat arenas designed for cover and flanking |

## Proposed architecture

```text
Assets/
├── Art/                 # Models, materials, animations, audio
├── Prefabs/             # Player, enemies, weapons, cover pieces
├── Scenes/              # Menu, prototype, and gameplay scenes
├── Scripts/
│   ├── Player/          # Input, movement, aiming, health
│   ├── Combat/          # Weapons, damage, projectiles, hit detection
│   ├── AI/              # Patrol, detection, combat, alert system
│   ├── World/           # Objectives, checkpoints, spawn points
│   └── UI/              # HUD, pause menu, objective feedback
└── Settings/            # Unity project settings
```

## Milestones

- [ ] **Prototype:** third-person controller, camera, basic shooting, and one enemy
- [ ] **Combat slice:** patrol AI, detection, alerts, health, and cover-based arena
- [ ] **Vertical slice:** complete playable defense scenario with objectives and HUD
- [ ] **Polish:** animation, sound, visual feedback, balancing, and performance work

## Getting started

The Unity project has not been committed yet. Once it exists:

1. Clone the repository.
2. Open the project folder through Unity Hub using the Unity version specified in `ProjectSettings/ProjectVersion.txt`.
3. Open the primary gameplay scene in `Assets/Scenes/`.
4. Press Play to run the prototype.

## Current status

The repository is being initialized. This README defines the first playable scope and will be updated alongside the implementation.

## Contributing

For now, keep each feature isolated and testable. Use focused branches, descriptive commits, and avoid committing Unity-generated folders such as `Library/`, `Temp/`, `Logs/`, and `Obj/`.

## License

License selection is pending.
