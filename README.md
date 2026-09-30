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
| F | Melee |
| G / Q | Throw selected grenade / switch frag and plasma |

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

## Melee and grenades

F performs a 60-damage melee strike within a 2.3-meter, 55-degree half-angle cone, with a 0.65-second cooldown. The closest living opponent must be visible; walls and allies block the strike. The equipped model thrusts forward and confirmed hits show the normal hit marker. Allied riflemen can use the same `MeleeAttack` at close range. Melee retains ordinary armor and directional shield rules.

G throws the selected grenade; Q switches frag/plasma. Each marine starts with **two per type**. Walk over the green frag or purple plasma supplies to refill that type to two. Full inventories leave supplies on the ground. A player handoff resets both counts. Throwing and melee briefly interrupt firing and cancel a plasma-pistol charge.

Frag grenades use a bouncing rigidbody and a 2.5-second fuse. Plasma grenades bounce weakly on terrain, stick to living targets, follow their movement, and detonate two seconds after sticking; an unstuck plasma grenade expires after four seconds. A stuck victim's death releases the grenade without deleting it or restarting the fuse. Swept/overlap contact detection supplements physics callbacks for CharacterControllers.

`GrenadeDefinition` controls prefab, fuse, throw speed, damage, shield multiplier, blast and suppression radii, cover attenuation, and color. Frag defaults are 180 peak damage / 6-meter damage radius; plasma defaults are 320 / 5 meters with double shield damage. Both produce up to 1.4 pressure over a 12-meter radius, falling with distance: a close blast can immediately cross the sustained-fire threshold. Solid cover reduces blast damage to 25% and pressure to 50%. Blast pressure affects both factions; friendly blast damage is disabled, but the thrower can hurt themselves.

`CombatGeometry` shares capsule-distance calculations between bullets, melee, and blasts. `CombatDamage` resolves common faction/armor/shield rules. `GrenadeInventory` owns counts and throwing, `GrenadeProjectile` owns bounce/stick/fuse behavior, `GrenadeBlast` resolves radial damage and pressure once per actor, and `MarineCombatActions` handles player controls. No NPC grenade-throwing behavior is enabled yet.

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

**Harvest > Run Grenade and Melee Checks (Play Mode)** checks radial suppression/cover, self and friendly damage, one blast application per actor, melee visibility/cooldown, two-per-type capacity, refill/selection, and rejected throws. Play-test real bounce trajectories, plasma adhesion to moving/dead targets, fuses, walk-over supplies, and weapon/action feedback separately.

No Unity Editor is available in the authoring environment, so the changes have had source inspection but not an Editor import or play test. Report any import error with the Unity Console output.

### Armor pressure and damage feedback

Intact marine armor scales incoming suppression to 60%; broken armor scales it to 125%. This applies to bullets and grenade pressure for player and allied marines; the existing activation threshold and pressure cap remain. Supplies restore the protection as soon as armor is positive. Both multipliers are editable on Suppression.

Player damage shows fading red direction bars around the reticle, tracking world attack origins as the view turns. Up means forward; right means right; down means behind. Up to eight recent hits can display together. A red edge vignette appears while armor is broken, increasing from 12% at full health to 65% at zero health and fading with regeneration or armor restoration. The center remains clear; handoff resets feedback. Bullets, melee, Brute strikes, and grenades carry their source positions through shared damage resolution.

Frag bounce is reduced from 0.55 to 0.22 and uses Average bounce combination. Rebuilding also updates the existing generated frag physics material. Run **Harvest > Build The Line Prototype** to add the camera feedback and migrate bounce. **Harvest > Run Combat Regression Checks (Play Mode)** now also covers armor pressure multipliers, damage origin, directional bearing, and red tint scaling. These Editor checks and rendering/physics still require a Unity play test.

### Farm encounter geometry and tracers

Rebuild with **Harvest > Build The Line Prototype** for the refined farm scene: a marked freight road, gravel shoulders and farm track, mesh terrain with flat combat ground and low outer banks, grain fields, fences, and an enterable farmhouse on the road's left. The front door leads into the ground floor; stairs along the field-side wall lead to upstairs windows overlooking the road and Covenant approach. Windows and the stairwell are actual openings in collider geometry. Grain is combined into one render mesh without movement/bullet collision. Navigation includes the terrain mesh and building boxes. Existing waves, checkpoint cover, pickups, and evacuation remain.

Rifle and shotgun hitscan shots emit visible fading lines for player and NPC weapons, including misses and each shotgun pellet. Lines stop at the actual raycast endpoint. Plasma bolts retain their projectile visuals. WeaponDefinition exposes tracer visibility, color, width, and lifetime; the scene-owned HitscanTracerRenderer pools up to 128 lines and receives shot events independently of damage resolution. Tracers consume no additional ammo and apply no additional damage or suppression.

Source, upload integrity, and geometric checks are performed here; Unity compilation, upstairs movement/NavMesh connectivity, window firing, scene appearance, and tracer rendering still require an Editor play test.

### Civilian precision weapons

The farmhouse upstairs has a hunting rifle pickup; press E to exchange it for the selected weapon. The civilian rifle has a wood stock, iron sights, a visible bolt cycle, five rounds plus 20 reserve, 90 body damage, a 1.4-second firing interval, and a 3.2-second reload. It uses the shared weapon/ammo/drop/tracer systems and keeps the two-slot loadout.

Hold **right mouse** to aim a precision weapon. ADS centers the iron sights and narrows the FOV to 42 degrees. Any damaging hit, including an armor hit, immediately exits ADS; release right mouse and press again to resume. Switching, pickups, reloading, sprinting, melee/grenades, death, handoff, or losing cursor lock also exit aim. Suppression still builds and affects screen feedback, but adds spread only to hipfire. ADS uses the weapon's independent aimed spread (zero for this rifle).

Precision hits to the configured upper capsule head region kill unshielded Covenant targets, in ADS or hipfire. Active shields prevent the bonus, including the shot that breaks them; Brutes always take normal damage. Graybox head silhouettes mark the region. PrecisionHitRegion can later be replaced with authored model hit zones. WeaponDefinition holds precision/aim tuning, PrecisionAim handles input and camera feedback, and WeaponRuntime resolves accuracy and hits.

Shotgun damage rises from 14 to at least 20 per pellet on rebuild; frag friction rises to 0.85 dynamic / 0.95 static with Maximum friction combination, retaining the reduced bounce. Rebuild using **Harvest > Build The Line Prototype**. **Harvest > Run Precision Checks (Play Mode)** covers shared fire, ADS/hipfire suppression spread, body/head hits, shields, Brute exception, cooldown/ammo, hit interruption and input release. Source and numerical checks are performed here; Unity compilation, runtime regression checks, iron-sight alignment, pickup, and grenade friction still need an Editor play test.

### Vertical-slice polish: weapon presentation

The first polish pass adds bounded camera recoil and model kick tuned per weapon, subtle movement bob/turn sway, and a reload lowering/raising pose. Precision ADS reduces kick while keeping its suppression-independent spread. Mouse pitch and recoil are composed in MarineController so camera offsets cannot accumulate; handoff and weapon exchange clear the kick.

Shared shot/impact events drive scene-owned pooled muzzle flashes and surface sparks for player and NPC weapons. Cover, marine armor, flesh, and shields have distinct colors; both hitscan and plasma impacts report the actual collision. Effects have a 96-instance cap and use no colliders or damage logic. The hipfire reticle expands with current spread, including suppression; ADS retains iron sights. Reload and bolt-cycle progress explain when the weapon can fire again.

Rebuild with **Harvest > Build The Line Prototype**. Recoil defaults migrate once through WeaponDefinition.FeedbackRevision so subsequent tuning is retained. Feedback, tracer, aim, and damage systems remain separate. This is a visual/game-feel pass; final weapon art, authored sound, animation, environmental art and encounter pacing remain future polish work. Source/event ordering, recoil/recovery bounds, effect pool limits, metadata, and full uploaded blob integrity are checked here. Unity compilation, existing Play-mode regression commands, recoil feel, VFX rendering, and HUD presentation still require an Editor play test.

### Approved audio integration

Scene version 15 includes approved service-rifle fire, Mosin hunting-rifle fire, service-rifle reload, and a quiet wind loop. Rebuild via **Harvest > Build The Line Prototype**. The second audition adds Nova shotgun fire (02B), deep frag blast (05B), and isolated gravel/wood boot steps (06A/06B). Plasma pistol and impact audio remain pending owner-selected sources; the unapproved pump-handling placeholder is not installed.

WeaponDefinition exposes fire/reload clips and volumes; scene-owned CombatAudio subscribes to successful shot and reload events. Reload events belong to WeaponInstance, so player and NPC manual/empty-magazine reloads use the same path. Weapon swaps remove player reload listeners before transferring instances. Local weapon sounds use 2D playback, NPC fire uses positional audio with distance attenuation, and a bounded 32-voice pool prioritizes local feedback. Wind volume is editable on CombatAudio. Null clip slots stay silent and existing clip assignments are preserved on rebuild. Approved/ contains original source credits and licenses.

Source/event checks, audio decode/headroom, file metadata and exact uploaded blob checks are performed here. Unity import, actual mixing/attenuation, reload/shot event timing and wind looping still require an Editor play test.

Run **Harvest > Run Audio Event Checks (Play Mode)** to verify one firing event per trigger (including shotgun pellets), silent cooldown/reload failures, and one event per successful reload start. This command is not executed in this environment.

FootstepAudio is independent of input and uses actual grounded planar travel for player and allied marines. Four gravel and five wood clips play individually, with no consecutive repetition and slight pitch variation. Walk/sprint/crouch stride and volume are editable; idle, airborne, death and teleport/handoff reset travel accumulation. NPC steps use positional audio. FootstepSurface tags farmhouse upper floors, landing and stairs as wood; other surfaces currently use gravel as the prototype's default bank. No walking loop is used.

Frag detonation emits one positional presentation event before destroying the grenade, so its tail survives projectile removal in CombatAudio's shared pool. Explosion clip and volume belong to GrenadeDefinition; plasma stays silent until approved. Rebuild scene version 15 to migrate the existing allied-marine prefab and add surface tags. Source, isolated-contact boundaries, decoding/headroom and exact remote blob integrity were checked; Unity compilation, import, cadence/surface switching, and blast mixing still require an Editor play test.

Scene version 16 assigns the owner's selected plasma-rifle sound. PlasmaRifleSingle.ogg isolates the initial 0.26-second discharge from the supplied recording, excluding the later multi-round burst. Shared successful-shot events play exactly one clip per round for both player and AI; the weapon's configured interval (0.13 seconds by default) controls cadence, rather than the recording. Separate pooled voices let short tails overlap naturally; no burst loop or new firing behavior is introduced. Empty battery/cooldown failures stay silent. The source file identity and unspecified licensing status are documented in Approved/CREDITS.md. Plasma pistol and hit sounds remain pending separate approval. Rebuild through **Harvest > Build The Line Prototype**. Extraction boundaries, audio decoding/headroom, metadata and uploaded blob integrity checked; Unity import/playback/mixing require Editor verification.

Scene version 17 adds alternating owner-supplied #2/#3 plasma environment impacts. A dedicated presentation event is emitted only by plasma contact with world geometry; rifle/shotgun hits and actor armor/shield/flesh hits do not use these clips. Playback is positional at contact, volume 0.22, attenuation from 1.5m to 35m, and lower voice priority; impacts are skipped when the pool is full instead of stealing gunfire. Clip padding is trimmed for immediate response and peaks reduced to 0.45.

Footsteps fall from 0.30 to 0.18 volume (40% lower), retaining crouch/sprint scaling. Existing allied-marine prefabs receive the 0.6 multiplier once through AudioMixRevision; further rebuilds preserve tuning. Approved Nova shotgun audio gains a stronger first 80ms (+2.3dB), tapering to a quieter tail (-4.4dB by 300ms and -8dB by 750ms). Fire cadence and weapon damage are unchanged. Rebuild **Harvest > Build The Line Prototype**. Source routing, migration, audio decoding/headroom, GUID uniqueness and remote blob integrity checked; Unity compilation and in-game mixing remain untested here.

The service rifle now uses the owner's AK47 recording with only the final individual discharge and a shortened natural tail (0.804–1.454s extracted; 0.65-second clip). This avoids carrying another recorded round into playback. The existing asset GUID and shared consumed-shot event path are retained: player, allies and any other actor using the service rifle play one discharge per accepted round, at the weapon's cadence, with no burst loop. Source identity and unspecified licensing are recorded in credits.

### Vertical slice cleanup

Scene version 18 raises shotgun output by approximately 70% overall (+4.6dB): source volume migrates once from 0.65 to 1.0 and the clip gains 1.105, preserving its stronger blast and quieter tail. Later source-volume tuning survives rebuilds through WeaponDefinition.AudioMixRevision. Other weapon and impact levels are unchanged.

One HUD pickup prompt now identifies the nearest reachable weapon that E will actually exchange. Pickup scans run at 10Hz, with an immediate rescan on E; world geometry blocks both prompts and pickups. Per-drop OnGUI searches and overlapping labels are removed. Weapon drops raycast onto nearby walkable surfaces at their actual height, including farmhouse upper floors, and swap placement stops short of intervening geometry. Player/NPC drops retain their ammunition rules. Re-enabled player loadouts restore their death subscription.

Wave number and an inter-wave countdown communicate time to regroup and resupply. Empty or null waves/entry lists advance safely instead of stalling; no enemy count or difficulty changes. Cursor release now blocks player movement, reload/swap and combat input while the HUD explains that the encounter remains live. Evacuation guards missing references, and the ending shows remaining squad survivors and explicit prototype replay instructions.

Rebuild via **Harvest > Build The Line Prototype**. Audio gain/headroom, event/source structure, GUID uniqueness, pickup/drop regression source and exact remote blob integrity checked. Unity compile, Play-mode checks, upper-floor drops, pickup occlusion and HUD appearance still require Editor validation.

**Harvest > Run Slice Interaction Checks (Play Mode)** creates isolated temporary test geometry to exercise upper-floor drop placement, visible pickups, wall/floor pickup occlusion and wall-safe swap placement. This command is supplied for Editor verification and has not run here.

### Farm and weapon detail pass

Scene version 19 adds a field-edge equipment shed with stacked bales, grain silo and ladder, storage crates, farmhouse window trim/gutter/chimney/porch benches, and freight/checkpoint/evac signs. Large props use simple box collision compatible with runtime navigation; small trim and dressing are visual only. Grain geometry leaves gaps around the new structures. Road, existing marine cover/peek lanes, farmhouse entrance, windows and stairs retain their clearance.

WeaponModelGeometry builds distinct service-rifle, pump-shotgun, hunting-rifle and plasma housings with barrels, stocks, grips, magazines, sights and vents. One recipe generates first-person models, NPC held models and world-model prefabs referenced by WeaponDefinition. Existing actor prefabs migrate old held cubes once. Drops display the equipped gun rather than a generic cube; fallback visuals remain available for definitions without a model. Hunting-rifle bolt name and iron-sight coordinates retain their animation/ADS contract. Weapon geometry has no colliders or gameplay effects. These remain procedural prototype models, not final authored art.

Covenant outgoing damage rises 15% in shared faction damage resolution (pistol 20→23, rifle 12→13.8, Brute melee 42→48.3). Weapon assets, player/allied damage, NPC drop ammo and friendly-fire rules retain their values; a captured gun does not inherit extra Covenant damage. Combat regression checks now exercise the actual faction/armor path for the modifier, no double application, base Marine damage, and friendly-fire protection. Rebuild **Harvest > Build The Line Prototype**. Source/model layout, clearance, metadata and remote blob integrity checked; Unity compilation, navigation, visuals/ADS and Play-mode regression commands remain untested here.


## Visual foundation (scene version 20)

Pull, let Unity resolve URP 17.3, then run **Harvest > Build The Line Prototype**. The builder migrates materials and activates URP/linear lighting, with a sky, soft sun shadows, farmhouse lighting, restrained post-processing, world-mapped weathered surfaces and distant landscape. Suppression and combat tint/tracer effects have been adapted to URP.

Run **Harvest > Validate Visual Foundation**, then check the encounter in Play mode. Unity compilation, shader rendering and performance were not testable in the development environment. [Visual authoring and validation workflow](VISUAL_WORKFLOW.md) documents persistent tuning assets, weapon-art overrides and the remaining art/animation work.
