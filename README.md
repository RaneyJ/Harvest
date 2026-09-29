# Halo: Harvest — The Line prototype

An early, unofficial fan prototype about ordinary marines during the fighting on Harvest. This repository contains a code-generated graybox scene. No Halo art, audio, or other game assets are included.

## Open and play

1. Install Unity **6.3 LTS** in Unity Hub with desktop build support. Open this repository as a Unity project. A newer 6.3 patch is fine; Unity may update `ProjectVersion.txt` locally.
2. Allow scripts to compile. On first import, the editor creates `Assets/Harvest/Scenes/TheLine.unity` and opens it. **If your scene predates the armor update, accept the rebuild prompt or choose Harvest > Build The Line Prototype.** Save or back up any manual scene edits first; the command rebuilds the graybox scene.
3. Press Play. Click the Game view to capture the mouse. Escape releases it.

| Control | Action |
| --- | --- |
| WASD / mouse | Move / aim |
| Left mouse | Fire |
| 1 / 2 or scroll | Service rifle / combat shotgun |
| R | Reload current weapon |
| Shift / Space | Sprint / jump |

Hold the road through two Covenant waves, use the green armor supplies, then reach the marked evacuation pad. Death transfers control to another marine at the defense line. The three available lives are an opening-sequence device, not the eventual campaign rules.

## Combat and extension points

- `MarineController` handles movement and camera look. `MarineLoadout` handles weapon input and per-weapon magazine state. `WeaponDefinition` assets hold weapon values; add a definition to the loadout array to add a weapon. `WeaponView` chooses the matching visual model.
- `Vitality` owns health and Covenant shield recharge. `MarineArmor` absorbs marine damage first, never recharges on its own, and accepts armor supplies; exposed health regenerates at 5 per second after 7 seconds without damage. The initial 175 armor / 100 health values are tuning defaults. Rifle and shotgun share the damage path; the shotgun is stronger against shields at short range.
- Enemy prefabs combine `CovenantEnemy` movement/perception, `Vitality`, and one behavior (`GruntBehavior`, `JackalBehavior`, or `BruteBehavior`). Grunts panic when a nearby Brute dies; the Jackal shield protects its front arc but can be flanked; a Brute telegraphs and commits to a charge. `EnemyHitFeedback` shows shield and health hits. Add a behavior component for another role.
- `EncounterDefinition` contains timed waves and spawn positions. `HarvestEncounter` runs that data and owns objective progression. `HarvestHud` and `EnemyHealthBar` subscribe to state changes for presentation.
- `BuildPrototype` generates starter assets and the graybox scene. It **preserves edited weapon, encounter, material, and enemy prefab values** on rebuild; it adds missing prefab visuals and feedback components. The scene itself is rebuilt.

The generated scene, prefabs, materials, and data assets should be committed after generation in the Editor. The code generator establishes defaults; those Unity assets become the editable source of truth for tuning. Runtime enemy spawning uses prefabs, not primitives.

This is still **single-player only**. Damage and encounter progression now have clear mutation points for a future host/server; responsive look and immediate local feedback can remain client-side. We will tune the combat before wiring networking.

No Unity Editor is available in the authoring environment, so the changes have had source inspection but not an Editor import or play test. Report any import error with the Unity Console output.
