# Farmhouse production brief

The approved concepts establish a working colony farmhouse caught in the opening attack: pale plaster wall panels, worn timber, dark utilitarian steel, pitched sheet-metal roofing, a covered porch, warm late-afternoon sun, cool interior shadows and localized Covenant damage. The modeling foundation implements the large forms and movement space. It is not the finished texture, damage or lighting pass.

## Dimensions and gameplay constraints

| Element | Default |
| --- | --- |
| World origin | (-19, 0, -2), matching the existing encounter |
| Main footprint | 12 × 14 metres |
| Ground walking surface | 0.12 metres |
| Upper walking surface | 3.2 metres |
| Eaves / ridge | 6.4 / approximately 7.896 metres |
| Roof | 14° pitch, 0.45 metre overhang |
| Wall shell | 0.24 metres |
| Front / rear doorway width | 2.2 / 1.4 metres |
| Covered porch | 10.8 × 2.6 metres |
| Stairs | 2.2 metres wide, 16 risers of 0.1925 metres, 0.4 metre treads |
| Stair run | Local x=-4.3; z=-4.9 to 1.5 |
| Upstairs road window | 4 metres wide, center z=1; sill 0.85 metres above floor |
| Upstairs front window | 3.2 metres wide, center x=1; sill 0.85 metres above floor |

The stairwell is a genuine opening between three upper-floor modules. The wide exit joins the rear landing without a cross-rail. A side guard and wall handrail preserve circulation. Window openings have no invisible glass collision. Crates flank the upstairs road-facing firing lane. The hunting-rifle supply uses the layout origin and upper-floor height.

The farmhouse keeps the original footprint, floor spacing and road-facing firing lane. Added porch structure and furniture still require a full encounter review. Existing road cover, waves and combat tuning are unchanged.

## Asset ownership

- `Assets/Harvest/Data/Farmhouse Layout.asset`: persistent dimensions, position and optional `AuthoredPrefab`. Changing dimensions requires a rebuild; the builder rejects unsafe stair/landing configurations.
- `Assets/Harvest/Prefabs/Environment/Farmhouse Generated.prefab`: generated assembly. Rebuilds overwrite this path. Save authored changes as a prefab variant or independent prefab at another path and assign `AuthoredPrefab` to protect them. Rebuilds instantiate the assigned asset without rewriting it.
- `Assets/Harvest/Art/Generated/Farmhouse`: dimensioned shared mesh assets. Generation supplies bevels, explicit face normals, metre-scale UV0, separately packed lightmap UV2 and tangents. The main roof slopes rotate UV0 so corrugations run down the pitch. Render objects use unit scale. Generated meshes remain shared by variants, so replace a mesh with an artist-owned asset before editing its geometry.
- `Assets/Harvest/Materials/Farmhouse Foundation *.mat`: persistent stock URP Lit base materials. Existing edits survive generation. The PBR installer assigns scanned maps only while these materials have no authored maps. Existing maps and edited tint, smoothness, normal strength, occlusion strength and UV transforms survive rebuilds. See [MATERIAL_SOURCES.md](MATERIAL_SOURCES.md) for sources and review steps. Final lighting is still pending.
- Hierarchy groups: `Shell`, `Roof`, `Porch`, `Interior`, `Trim`, `Collision`. Trim has no movement or bullet collision. Broad box/mesh collision is isolated in `Collision`, used by the existing navigation builder and footstep surface system. Preserve these groups and the `FarmhouseFoundation` marker in authored variants.

`FarmhouseLayout` owns dimensions; `FarmhouseMeshLibrary` owns reusable topology/UV generation; `FarmhouseFoundationBuilder` owns assembly/material defaults; `FarmhousePlasterFinish` owns the optional reduced-repeat plaster bake; `FarmEncounterGeometry` owns placement within the larger encounter. Encounter logic and runtime actor behavior do not depend on the visual modules.

## Build and review

1. Run **Harvest > Install Farmhouse PBR Materials** and wait for its completion message (about 156 MB on first use), then save manual scene edits before running **Harvest > Build The Line Prototype**. Scene version 31 replaces the earlier house and removes its duplicate legacy trim. Generated meshes and prefab are created by Unity; they cannot be pre-baked in this environment.
2. Run **Harvest > Validate Farmhouse Foundation**. It checks prefab connection and persistent mesh ownership in Edit mode. Both modes check hierarchy/collision separation, finite geometry, triangle winding, UV channels, tangents, standing-body clearance, support under circulation points and firing sightlines. Play mode does not require editor prefab/asset ownership for the live objects.
3. Enter Play mode, run the same menu to additionally check a complete marine NavMesh path from downstairs to the upstairs window.
4. Walk the front and rear entrances, stairs in both directions, landing and upstairs firing approaches. Check standing/crouching and grenade behavior. The capsule checks allow adjacent traversable stair risers; they do not replace a real CharacterController traversal check.
5. Review the house from the approach road, porch, kitchen, stairs, upper front window and upper road window. Capture screenshots at the normal game FOV. Verify scale, roof silhouette, seams, underside visibility and readable interior lighting.
6. Run a full wave: validate marine cover behavior, existing sightlines and the rifle supply. Then inspect a standalone build.

Available here: source/API inspection and independent geometry/layout math checks. Not available here: Unity compilation, UV unwrapping execution, Editor/GPU captures, navigation bake or gameplay traversal. Treat those as review gates before declaring this stage approved.

## Material review

Run **Harvest > Refine Farmhouse Plaster** once after installation, then **Harvest > Build The Line Prototype** to rebuild version 25. Run **Harvest > Validate Farmhouse Materials**, then **Harvest > Build Farmhouse Material Review**. This creates an isolated neutral-light scene with five surfaces, metre-scaled panels/blocks and reflection spheres. Review scale, grain direction, normal intensity and roughness before returning to The Line. The scene is excluded from the game build. See [MATERIAL_SOURCES.md](MATERIAL_SOURCES.md).

## Subsequent work packages

| Work package | Deliverable | Acceptance |
| --- | --- | --- |
| Modeling refinement | Review foundation in-game; refine roof/porch/trim silhouette, add physically plausible joins and authored prop meshes | Approved scale, no movement snags, house convincing at gameplay distance |
| Material authoring | Review installed plaster, timber, steel, roofing and concrete PBR maps; refine tiling, tint, grain direction and selective wear | Materials distinguish correctly under neutral light; no stretched or uniformly noisy textures |
| Interior dressing | Agricultural terminal, cupboards, seating, possessions, fixtures | Lived-in colony identity with clear circulation and firing access |
| Local damage | Broken panes, selective plaster chips and plasma scars | Match approved damage placement without making every surface ruined; test bullet behavior |
| Lighting | Stable geometry/UVs, baked bounce, moving-object probes, interior/exterior reflection coverage and practical lights | Readable room transitions, no leaks or floating characters, complete Editor and build review |

Future agents should receive a single work package, owned paths, the approved concept references and these review criteria. Geometry and material work can split after dimensions are approved; final lighting follows stable geometry and materials. Integrate and review each package separately on `prototype/the-line`.

## Continuous facade correction (version 25)

Each storey/facade now uses one persistent wall mesh made from exact, un-inset wall sections around its openings. Removed the 18mm horizontal and 12mm vertical spacing between the old visual panels and their bevel grooves. Wall UV0 is projected from house-space vertex positions, keeping its phase continuous across sections and storeys; gable UVs include their eave-height offset. Front/rear walls extend through the corners while the side walls stop at their inner faces; the corner steel is positioned outside the plaster. Door/window dimensions are retained. Collision uses the same full wall sections as the visual geometry.

The foundation validator additionally compares wall UVs to the house-space projection and tests rendered triangle coverage wherever the wall collision is solid, including samples near facade ends and the top/bottom edges. This check is for generated geometry; an explicitly assigned artist prefab retains its own authoring responsibility. Runtime/editor visual and traversal review are still required.

## Interior dressing (version 26)

Five deliberate prop groups replace the original counter/table blocks: rear kitchen, dining table/benches, front road-side farm storage, upstairs field-side sleeping area and an upstairs desk/terminal. Cabinets have separate doors, drawers and pulls; the utility sink has a raised open basin. Pots, mugs and bowls use reusable hollow vessel meshes with metre UVs, separate lightmap UVs and tangents. The cot, work boots, notebooks and framed field plan add personal/agricultural context. Small decorative items are visual; substantial furniture uses stable collision proxies. Existing upstairs cover crates and weapon supply are retained.

`FarmhouseInteriorDressing` owns modeling and placement; `FarmhousePropMeshes` owns vessel geometry. `Assets/Harvest/Data/Farmhouse Dressing Layout.asset` stores enabled flags, offsets, yaw adjustments and optional artist prefab overrides for each area. Rebuild after changing it. Generated defaults are saved under `Assets/Harvest/Prefabs/Environment/FarmhouseProps`; never edit these generated prefab paths in place. Put replacements elsewhere and assign their override. Overrides should be static props with floor-level origins, persistent meshes and baked unit transforms. Assembly copies their contents and collider settings without changing the source asset; source changes appear on rebuild. Collider proxies join the house Collision group. The top-level farmhouse AuthoredPrefab override continues to bypass all generation.

Materials under `Assets/Harvest/Materials/Farmhouse Prop *.mat` persist across rebuilds. The props reuse the approved timber, steel and concrete surfaces, with quiet olive paint, canvas/linen, enamel, rubber and paper accents. Existing material edits survive. Decorative geometry is eligible for static batching/GI; there is no completed bake or measured performance claim. Terminal displays are modeled surfaces; this pass adds no lights, new runtime behavior or interactive terminal system.

After pulling, save scene edits and run **Harvest > Build The Line Prototype**, then **Harvest > Validate Farmhouse Foundation** in Edit mode and Play mode. The validator now samples both central corridors, the stair landing-to-center route, the road-facing weapon/window approach and access points near the kitchen, dining area, cot and desk. Check the actual controller, crouching, grenades, marine navigation/cover, sightlines and all waves as well. Move/disable a cluster if an edited placement obstructs a route. Look for floating fittings, clipping, repeated silhouettes and oversized details at gameplay FOV. Unity compilation, unwrap execution, renderer captures and gameplay checks remain to be run locally.

## Prop contact corrections (version 27)

Rebuild The Line after pulling. The rack controller now rests on a top shelf, seed-bin bottoms meet their shelf tops, and tabletop/chest notebooks use their actual support height. The desk has a flat writing pad with exposed paper and its top binding toward the terminal, following the desk's rotation. Source dimensions/contact math were checked; inspect the shelf and desk in Unity after rebuilding. Artist-owned replacement prefabs retain their own geometry and placements.

The same version replaces full-room floor strips with roughly 20cm-wide, 2.3m-long boards, staggered in thirds. Both storeys use the same house-space row/joint grid, clipped around the existing stairwell. Narrow 2mm joints have a backing below them; movement continues to use the original flat floor colliders. Each floor section has a combined mesh and a separate persistent `Farmhouse Plank Floor.mat`, cloned from the approved timber. Grain runs along the boards with a longer longitudinal scale and deterministic per-board UV offsets; furniture's timber material is unchanged. Inspect grain scale, joints and the upstairs stairwell edge after rebuilding. Floor material edits persist across rebuilds.


## Architectural finish (version 28)

Interior openings gain timber casings, sill boards and aprons; floor/wall joins gain low skirting that stops at doorways. Thin plaster ceiling finishes follow the existing upper-floor sections and preserve the stair opening. Roof edges now have fascia, soffits, pitched ridge-cap wings and open gutters with end caps and wall-bracketed downpipes. These finishes have no collision; the original shell, floors, roof collision, movement routes and firing apertures are retained. The shared generated house meshes are eligible for static batching as well as GI.

After rebuilding, inspect window ledges, doorway feet, ceilings over stairs, the roof ridge and rainwater joins. Source dimensions can be checked here; Unity mesh generation, UV unwrap and visual quality still require an Editor review.


## Farmyard and horizon refinement (version 29)

The shed and storage props use dimensioned bevelled meshes with metre UVs, lightmap UVs and tangents instead of scaled primitive cubes. Bare starter timber/roofing surfaces use the approved farmhouse scans; imported or mapped prop material overrides retain their own surfaces. Shed battens, headers, post shoes and corner braces articulate construction. Roof seams now follow the shed pitch. Crates gain corner uprights and strap fasteners.

The grain silo gains a pitched lid, rolled rings, a foundation collar and door handle. Its ladder is tangent to the cylindrical surface, with two rails and stand-offs instead of floating axis-aligned rungs. The original six-metre-square collision envelope remains. Small finish geometry remains visual-only. No encounter placements, waves or combat systems change.

Distant settlement boxes become pitched agricultural buildings with selective annexes and silo silhouettes, backed by rolling ground outside the playable terrain. They have no collision, navigation or shadow casting; distant geometry does not contribute to the farmhouse lightmap atlas. Meshes are persistent generated assets. Check the shed interior, silo ladder and horizon from the road in Unity; source checks are not a rendered approval.


## Lighting rig and bake review (version 30)

Practical lights follow the farmhouse layout and have modeled ceiling/wall fixtures, controlled warm bulbs and mixed-light shadows. The upper and lower room captures have separate volumes; the farmyard has its own lower-priority capture. Preview reflections render individual faces once at startup; the explicit reflection bake replaces them with saved cubemaps. Classic light-probe samples cover both rooms, stairs and approach, excluding points inside collision. The rig adds no gameplay or collision. An artist-owned farmhouse replacement supplies its own fixture/probe rig.

Practical color/intensity/range live in `Harvest Look.asset`. New fields leave existing daylight/atmosphere tuning untouched. On the first rig build, URP's additional-light shadow support/per-pixel mode is enabled with a minimum 4096 shadow atlas; subsequent builds retain pipeline edits. The renderer/suppression/smoke features are not changed. `Harvest Lighting.lighting` is persistent and editable: stock Progressive CPU, directional lightmaps, baked indirect with realtime direct lights/shadows, 24 texels/metre, 256 indirect/environment samples, three maximum bounces and limited indirect contact AO. These are starting settings, not a completed or measured bake. No custom post-processing is added.

Review steps after pulling:

1. Save manual changes; run **Harvest > Build The Line Prototype** (version 30).
2. Run **Harvest > Validate Farmhouse Foundation**, **Harvest > Validate Visual Foundation**, and **Harvest > Lighting > Validate The Line Lighting**. Inspect ceiling-fixture contacts and bulb placement.
3. Outside Play mode, run **Harvest > Lighting > Bake The Line Lighting**. Wait for Unity's bake to finish; inspect noise, UV seams, bounce and room/exterior transitions. Adjust the persistent settings if needed. Rebuilding the scene invalidates baked data, so bake after geometry is approved.
4. Run **Harvest > Lighting > Bake The Line Reflections** after GI. It stores successful cubemaps under `Assets/Harvest/Rendering/Reflections`, changes those probes to saved custom captures, and saves the scene. Check window reflections and steel roughness. Save again after any lightmap or scene edits.
5. Review the road approach, porch, downstairs dining/kitchen, stairs, upper desk and firing window at gameplay FOV. Check moving marines and weapons against the baked room lighting, then the full encounter and standalone build. Reflection boundaries, shadows and performance still need a GPU review.

Saved reflection capture temporarily hides actor renderers and restores them in a finally block, avoiding frozen characters/viewmodel stacks in environment cubemaps. The bake menus operate only on The Line outside Play mode. Rebuilds do not start long bakes automatically and do not reset authored material, atmosphere, post-profile or lighting-settings values. Unity compilation, generated mesh/UV execution, bake execution and GPU visuals could not be exercised in this environment.

API references: [Unity 6.3 LightingSettings](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/LightingSettings.html), [BakeAsync](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Lightmapping.BakeAsync.html), [BakeReflectionProbe](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Lightmapping.BakeReflectionProbe.html), [URP 17.3 asset API](https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@17.3/api/UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset.html).


## Window-sill contact correction (version 31)

The timber board's top previously coincided with the plaster reveal, and the older exterior ledge overlapped it. Each window now uses one board spanning the wall and both overhangs. Its underside rests on the plaster sill; its top is 50mm above the reveal. The interior apron meets the board underside and embeds its back 1.5mm into the wall rather than floating. Removed the duplicate exterior ledges. Window/wall collision is unchanged; inspect the joint inside and outside after rebuilding, then re-bake lighting/reflections. Contact/overlap arithmetic passed; Unity rendering remains to be checked.
