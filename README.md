# Halo: Harvest — The Line prototype

An early, unofficial fan prototype about ordinary marines during the fighting on Harvest. This repository contains a code-generated graybox scene. No Halo art, audio, or other game assets are included.

## Open and play

1. Install Unity **6.3 LTS** in Unity Hub with desktop build support. Open this repository as a Unity project. A newer 6.3 patch is fine; Unity may update `ProjectVersion.txt` locally.
2. Allow scripts to compile. On first import, the editor creates `Assets/Harvest/Scenes/TheLine.unity` and opens it. **Accept the rebuild prompt after combat updates, or choose Harvest > Build The Line Prototype.** Save or back up any manual scene edits first; the command rebuilds the graybox scene.
3. Press Play. Click the Game view to capture the mouse. Escape releases it.

| Control | Action |
| --- | --- |
| WASD / mouse | Move / aim |
| Left mouse | Fire; tap plasma pistol for a normal shot, hold then release for a charged shot |
| 1 / 2 or scroll | Select weapon slot 1 / 2 |
| R | Reload current magazine weapon |
| E near a drop | Pick up weapon and replace the equipped slot |
| Shift / Space | Sprint / jump |
| Hold Left Ctrl | Crouch; release to stand when there is headroom |

Hold the road alongside three allied riflemen through five escalating Covenant waves, use the green armor supplies, collect Covenant weapons from fallen enemies, then reach the marked evacuation pad. Death transfers control to another marine at the defense line. The three available lives are an opening-sequence device, not the eventual campaign rules.

## Combat and extension points

- `MarineController` handles movement and camera look. `WeaponDefinition` assets describe hitscan or projectile fire and magazine or finite-energy ammo. `WeaponInstance` holds remaining ammo and cooldowns and travels with a drop; `WeaponRuntime` resolves firing and damage for both player and enemy AI. `MarineLoadout` handles only player input and two slots, while `ActorWeapon` uses the same firing path. `WeaponView` chooses the matching player model.
- `Vitality` owns health and Covenant shield recharge. `MarineArmor` absorbs marine damage first, never recharges on its own, and accepts armor supplies; exposed health regenerates at 5 per second after 7 seconds without damage. The initial 175 armor / 100 health values are tuning defaults. Rifle and shotgun share the damage path; the shotgun is stronger against shields at short range.
- Enemy prefabs combine `CovenantEnemy` movement/perception, `Vitality`, `ActorWeapon`, and one behavior (`GruntBehavior`, `JackalBehavior`, or `BruteBehavior`). Grunts panic when a nearby Brute dies; the Jackal shield protects its front arc but can be flanked; a Brute telegraphs and commits to a charge. `EnemyHitFeedback` shows shield and health hits. Add a behavior component for another role.
- `EncounterDefinition` contains timed waves and spawn positions. `HarvestEncounter` runs that data and owns objective progression. `HarvestHud` and `EnemyHealthBar` subscribe to state changes for presentation.
- `BuildPrototype` generates starter assets and the graybox scene. It **preserves edited weapon, encounter, material, and enemy prefab values** on rebuild; it adds missing prefab visuals and feedback components. The scene itself is rebuilt.

The generated scene, prefabs, materials, and data assets should be committed after generation in the Editor. The code generator establishes defaults; those Unity assets become the editable source of truth for tuning. Runtime enemy spawning uses prefabs, not primitives.

Grunts and Jackals carry plasma pistols; Brutes carry plasma rifles. NPC death drops receive a random 40–70% of battery capacity (or magazine and reserve capacities for conventional weapons); the current marine also drops the equipped weapon on death. Press E near a drop to replace the currently selected weapon; the replaced weapon drops with its remaining ammo. Plasma weapons use finite charge and cannot reload. The plasma pistol charges in one second and consumes 20 battery units per charged shot. Release early for a normal shot; below the charged-shot cost, release fires a normal shot if enough energy remains. The HUD shows charge progress. Charged impacts strip an active shield without health overflow, or deal 40 damage to unshielded targets. Jackal flanking still bypasses the front shield. Weapon switching, pickup, death, cursor release, and encounter completion cancel charging. Charge duration, cost, and damage live in `WeaponDefinition`; AI can request a charged shot through the shared runtime later.

This is still **single-player only**. Damage and encounter progression now have clear mutation points for a future host/server; responsive look and immediate local feedback can remain client-side.

## Crouching and suppression

Hold Left Ctrl to crouch. Movement slows, the capsule shrinks while keeping its feet fixed, and the camera eases down. Release to stand; overhead geometry keeps the player crouched until clear. Sprint and jump are disabled while crouched. A marine handoff restores standing stance and clears pressure.

`Suppression` is a per-combatant component. Hostile shots within 2.5 meters of the body add pressure; closer shots add more. Solid cover reduces nearby pressure to 25%. Pressure accumulates silently up to a **50% activation threshold**. Three close shots (48% pressure) cause no accuracy loss, blur, or suppression HUD; further sustained shots ease effects in above the threshold and reach maximum at 100% pressure. Wider near misses and cover impacts need more shots. Pressure waits 1.5 seconds after the last threat, then fades at 25% per second, so isolated fire can recover before effects activate. At maximum pressure, player, allied, and Covenant weapons gain 5 degrees of additional spread. Nearby friendly shots do not add suppression. Heavily suppressed allied marines retreat to their cover slot.

Hitscan exposure stops at the first collision and combines shotgun pellets into one strongest contribution per target per trigger pull. Moving plasma bolts track each target's strongest exposure across their lifetime, adding only the difference as they pass closer; a bolt cannot stack pressure every frame. No threat is generated by an empty weapon or a failed cooldown check.

The player camera uses `SuppressionScreenBlur` and a serialized two-pass blur shader with a separate composite pass in the project's built-in render pipeline. Blur radius and blend follow the shared strength above the threshold, then recover; below the threshold blur switches off completely. Maximum blur radius is 2.5 (previously 5) and maximum blend is 45% (previously 85%). HUD text remains sharp. Tune receiver pressure, radius, recovery, maximum spread, and cover attenuation on `Suppression`, and blur strength separately on the camera effect.

## Allied squad and cover

Three named riflemen defend the checkpoint alongside the player. They have the same finite armor and slow health regeneration, can be killed by plasma or Brute melee, and drop their rifle with 40–70% ammo. Allies persist between waves and do not respawn when the player changes marines. Friendly fire is disabled for both factions; a friendly body still blocks a firing lane.

`CombatTarget` registers living combatants for both factions, so Covenant choose between the player and squad rather than always pursuing the player. `AlliedMarine` owns target selection and a cover/peek/burst/retreat loop; `ActorWeapon` owns the shared ammo and firing path. Allies crouch behind cover between bursts, retreat when hit or reloading, and have a small configurable aim error. They do not consume the player's armor pickups.

`CoverPoint` defines a hidden position and lateral peek offset, with an exclusive claim per marine. It checks whether geometry protects the hidden position and whether both positions have complete navigation paths. Add or move cover slots in the scene and tune the squad prefab's detection range, cover radius, burst size, rest time, and accuracy. `BattlefieldNavigation` builds navigation once at scene startup from static box colliders in the generated graybox; replace it with a baked surface when moving to a finished map.

New scenes contain five waves with 3, 6, 7, 8, and 10 enemies. Later waves add shield lines and paired Brutes, with 7–9 second pauses. Existing encounter assets keep their configured waves and receive the three later waves once, tracked by `PrototypeWaveRevision`. Subsequent rebuilds preserve edited waves and do not append duplicates. We will tune the combat before wiring networking.

During Play mode, **Harvest > Run Combat Regression Checks (Play Mode)** exercises faction/range filtering, projectile and shotgun deduplication, impacts on cover, cooldown/ammo rejection, silent buildup, activation threshold, effect ramp, pressure limits and reset using temporary actors outside the map. This does not replace visual blur, headroom, recovery timing, or AI play testing.

No Unity Editor is available in the authoring environment, so the changes have had source inspection but not an Editor import or play test. Report any import error with the Unity Console output.
