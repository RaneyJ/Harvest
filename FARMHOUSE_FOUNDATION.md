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

1. Run **Harvest > Install Farmhouse PBR Materials** and wait for its completion message (about 156 MB on first use), then save manual scene edits before running **Harvest > Build The Line Prototype**. Scene version 25 replaces the earlier house and removes its duplicate legacy trim. Generated meshes and prefab are created by Unity; they cannot be pre-baked in this environment.
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
