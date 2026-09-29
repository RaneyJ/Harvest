# Halo: Harvest — The Line prototype

An early, unofficial fan prototype about ordinary marines during the fighting on Harvest. This repository contains a code-generated graybox scene. No Halo art, audio, or other game assets are included.

## Open and play

1. Install Unity **6.3 LTS** in Unity Hub with desktop build support. Open this repository as a Unity project. A newer 6.3 patch is fine; Unity may update `ProjectVersion.txt` locally.
2. Allow scripts to compile. The editor creates `Assets/Harvest/Scenes/TheLine.unity` on first import and opens it. If it does not, choose **Harvest > Build The Line Prototype**. This command also rebuilds the scene after generator changes.
3. Press Play. Click the Game view to capture the mouse. Escape releases it.

| Control | Action |
| --- | --- |
| WASD | Move |
| Mouse | Aim |
| Left mouse | Fire |
| R | Reload |
| Shift | Sprint |
| Space | Jump |

Hold the road through two Covenant waves, then reach the marked evacuation pad. Death transfers control to another marine at the defense line. The three available lives are an opening-sequence device, not the eventual campaign rules.

## Current slice

- First-person CharacterController, hitscan rifle, magazine and reserve ammo, enemy shield/health split, and an on-screen HUD.
- Grunts and a shielded Jackal advance and fire visible plasma bolts. The Brute closes to melee range. Cover blocks both rifle fire and plasma.
- Two escalating waves, evacuation state, three named marines, failure and victory states.
- A generated dusk farm road with cover, crop rows, freight structures, smoke columns, lighting and fog. Everything is made from Unity primitives for now.

## Development notes

The generated scene is saved under `Assets/Harvest/Scenes/` and should be committed once generated in the Editor. The generator remains the source of truth for this initial graybox. Runtime components are deliberately separate from scene creation, so we can replace geometry and visuals without rewriting combat.

This is currently **single-player only**. The multiplayer pass should make encounter progression, damage, enemy decisions and spawn authority live on the host/server, while the local marine retains responsive look and firing feedback. We will test the combat feel before selecting the full networking implementation.

No Unity Editor is available in the authoring environment, so this first commit has had source inspection but not an Editor import or play test. Please report any import error with the Unity Console output.
