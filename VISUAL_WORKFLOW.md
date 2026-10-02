# Harvest visual foundation

The Line now targets Unity 6000.3 / URP 17.3 with Render Graph enabled and linear color space. This is the first rendering and surface pass toward the Reach reference. Characters, weapon geometry and animations remain prototype assets.

## Pull and rebuild

1. Let Unity Package Manager resolve URP after pulling `prototype/the-line`.
2. Run **Harvest > Install Farmhouse PBR Materials** and wait for completion, then save manual scene changes and run **Harvest > Build The Line Prototype**. Version 26 retains the continuous farmhouse walls and installs the modular interior dressing. Installed PBR maps are applied only to untouched base materials. Persistent rendering assets and existing material tuning are retained. Weapon tuning, audio and configured waves remain on their existing assets.
3. Run **Harvest > Validate Visual Foundation**. Check the Console for shader errors.
4. In Play mode inspect farmhouse interiors, shadows, rifle tracers, plasma, grenade fading, armor/charge tint feedback and sustained-fire suppression. Also check upstairs movement, cover and all waves. The build environment used for this change has no Unity Editor; these runtime and visual checks still need to be run in Unity.

## Authoring boundaries

- `HarvestRenderSetup` owns URP configuration and material migration. Defaults are set only when a rendering asset is first created. All quality levels reference the same starting pipeline; tune separate performance tiers later.
- `Assets/Harvest/Rendering/Harvest Look.asset` controls sun, ambient colors and fog. Rebuild to apply changes.
- `Harvest Post.asset` contains editable ACES tone mapping, bloom, grading and vignette. Volume changes update in the scene without a rebuild; the scene builder preserves this asset.
- `Harvest Sky.mat` controls the procedural sky. `Harvest URP.asset` controls shadow quality and rendering budgets.
- `HarvestSurfaceLibrary` creates deterministic tiled starter textures at editor time. The farm shader maps them in world space to avoid stretched walls. This is placeholder texture work, not scanned/PBR final art. Materials already carrying authored textures are preserved.
- `FarmVisualPass` owns lighting and distant scenery. The oversized sphere hill placeholders are removed in version 21. Distant ground and settlement objects reuse the farm materials and have no colliders and do not extend the combat/navmesh bounds. Two unshadowed interior lights, one 128px reflection probe refreshed on scene load, four sun cascades and two suppression raster passes establish a bounded starting budget. Nothing here is a measured performance guarantee.
- Suppression remains camera-local gameplay state. Its renderer feature runs only on game cameras with active pressure, before post-processing. GUI hit indicators and HUD stay sharp. The old Built-in `OnRenderImage` effect is removed.

## Replacing weapon art

Each WeaponDefinition has separate `ViewModelPrefab` and `WorldModel` references. Assign authored prefabs in meters with +Z forward, an identity root and a grip-centered origin; adjust the existing view/ADS poses for the new asset. First-person child colliders are disabled by the builder. World prefabs must be visual-only (no colliders, gameplay scripts or damage targets).

Keep authored world prefabs outside `Assets/Harvest/Prefabs/<DisplayName> Model.prefab`, which is reserved for the generated fallback. Rebuilds preserve world references to any other path and use the assigned view prefab for the first-person model. On an existing NPC prefab, refresh its `Held weapon` child manually when switching world art; rebuilding does not overwrite previously authored NPC presentation. A child named `Bolt` can participate in the current hunting rifle bolt pose.

Keep imported characters' renderers/rigs separate from Vitality, CombatTarget, Armor, weapons and navigation. Detailed character animation integration is a subsequent pass; this update does not provide finished marine/Covenant rigs or first-person reload animation.

## Next art milestone

Finish one farmhouse courtyard before extending the map: authored house/props and vegetation, a finished rifle with hands/reload, one rigged marine and one Covenant enemy. Review these in motion and during a full wave. Baked indirect lighting, light probes, authored normal maps, final VFX and measured LOD/performance tiers follow once assets and geometry stabilize.

## Suppression rendering correction (version 21)

The blur shader includes URP Core before the shared Blit header, uses an explicit procedural triangle and receives its source texel size per camera. Unsupported shaders skip the effect rather than replacing camera output. The validation menu also reports failed material shaders by asset path. Rebuild to remove the previous oversized horizon spheres; verify sustained-fire blur in Play mode after shader import completes.

## Smoke and property binding correction (version 22)

The original Smoke column particle systems previously had no assigned material, leaving a Built-in default after URP migration. They now use a dedicated transparent URP smoke shader with soft radial opacity, fog, and lifetime fading. Rebuild to assign the material to all three emitters. Validation detects old default particle materials.

Suppression now uses the basic blit parameter constructor and explicitly binds the source texture, scale vector, slice integer and mip integer to distinct Shader.PropertyToID names; it no longer relies on the property-block overload defaults. These changes address the reported `<noninit>` property-sheet conflict. Unity GPU validation remains necessary.

Run **Harvest > Run Suppression Render Check (Play Mode)** with the Game view active. It creates a temporary offscreen camera, exercises the real feature at zero/full/reset pressure and reads a GPU-rendered pixel. It checks constant-color preservation, verifies that the pass actually scheduled, detects property-sheet conflicts and cleans up the camera. A failure is reported with the sampled colors or binding message. This command has not been executed in the development environment.

## Rebuilt suppression presentation

The current effect replaces the earlier replacement-target blit path. It uses two explicit raster passes before post-processing: copy active scene color into a separate texture with Unity's standard Blitter, then sample that copy and alpha-blend onto the existing camera color attachment. The destination is registered as ReadWrite so the scene is loaded for blending. The renderer does not replace cameraColor or use BlitMaterialParameters/property-block constructor overloads. A command-buffer vector supplies radius, opacity and texel dimensions at draw time. The shader caps overlay opacity at 45% and preserves framebuffer alpha. The original scene therefore contributes at least 55% before grading, even if the sampled copy is wrong. HUD and damage indicators remain separate.

Run **Harvest > Run Suppression Render Check (Play Mode)** with the Game view active. The updated test renders four colored quadrants and checks reference → copy-only overlay → actual blur → recovery, first without post-processing and then with HDR, post-processing and FXAA enabled. It checks five GPU-read pixels: quadrant interiors must preserve their colors, the boundary must actually soften, and recovery must restore the reference. Unlike the older constant-color test, it can detect collapsed UVs, white/black source sampling, flipped quadrants and a blur pass that never applies. The test uses an offscreen camera and removes its temporary geometry, textures and materials. It remains unrun here because Unity/GPU rendering is unavailable. This script/shader-only update needs a fresh Play session after import, not a scene asset rebuild.

The blur radius now has a camera-local `BlurRadiusMultiplier` defaulting to 3. Existing 2.5-pixel settings produce a 7.5-pixel sampling radius at full suppression without changing opacity, buildup or gameplay penalties. This new field applies to existing camera components after script import; no scene rebuild is needed. Tune the multiplier in `SuppressionScreenBlur` for the desired intensity.

## Approved farmhouse modeling foundation (version 23)

The approved courtyard, exterior and interior concepts now guide a dimensioned modular farmhouse. Run **Harvest > Build The Line Prototype**, then **Harvest > Validate Farmhouse Foundation** in Edit mode and again in Play mode. The latter additionally checks marine navigation from the entrance to the upstairs firing position. See [FARMHOUSE_FOUNDATION.md](FARMHOUSE_FOUNDATION.md) for dimensions, asset ownership, review steps and subsequent art tasks. The foundation now supports the scanned PBR pass described in [MATERIAL_SOURCES.md](MATERIAL_SOURCES.md). Surface review, battle damage and final baked lighting remain separate stages. Unity mesh unwrapping, scene rendering, controller traversal and navigation validation remain to be exercised in the Editor.

## Farmhouse surface pass (version 24)

`FarmhouseMaterialLibrary` owns the pinned CC0 source installation and URP channel packing. `FarmhouseMaterialSources.json` records URLs, authors, physical tile sizes, byte counts, provider MD5 and independently calculated SHA-256. The installer downloads to a resumable verified cache, imports 2K albedo/normal maps and creates linear packed masks. `FarmhouseMaterialChecks` validates the inputs and output wiring. `FarmhouseMaterialReview` builds a separate neutral-light review scene without gameplay scripts or post-processing. Existing artist maps are preserved. Run the material validation and inspect the review scene before approving the encounter lighting. Unity compilation/import and GPU appearance remain unverified in this development environment.

## Facade continuity and plaster variation (version 25)

After pulling, run **Harvest > Refine Farmhouse Plaster** once, then **Harvest > Build The Line Prototype**. The refinement uses the existing verified plaster cache; no additional download is required. Its editor bake creates three 4K maps over a 4m physical tile by blending phase-shifted source samples with a periodic, smoothly interpolated pattern. Albedo blends in linear color space, tangent normals are renormalized and packed material channels use the same sample coordinates and weights. It adds no runtime shader feature or additional texture sample. The larger maps use more texture memory and have about 1024 pixels/metre at default scale, versus the original scan's 2048 pixels/metre. Review close-up detail as well as repeated patterns.

Only materials still using the exact original installed plaster maps are upgraded. Authored maps/tint/roughness tuning remain intact. Refined maps and the adjusted material transform persist across rebuilds. Review the neutral scene and wall joins in The Line, then run both material and farmhouse foundation validation. There is no Unity Editor available in the development environment, so Unity bake performance, GPU appearance and traversal remain to be checked locally.

## Farmhouse interior dressing (version 26)

Run **Harvest > Build The Line Prototype**, then the farmhouse foundation validation in Edit and Play mode. The dressing layout asset controls five areas independently and supports artist-owned static prefab overrides. Review the rear kitchen, dining benches, farm seed rack, upstairs cot and work terminal. The source meshes and circulation layout were checked independently; Unity rendering, collision proxy assembly, unwrapping, navigation and performance still require Editor verification. See [FARMHOUSE_FOUNDATION.md](FARMHOUSE_FOUNDATION.md) for ownership and acceptance details. Lighting refinement follows approval of this placement pass.

### Farmyard composition (scene 37)

The farmhouse detail pass is paused while the yard catches up. `FarmyardPolish` supplies paired tyre-worn strips on the existing freight road and access track, sparse low dry vegetation at field/road boundaries, and two supply clusters outside the central access lane. The existing crop layout, terrain collision and encounter configuration remain. Surface accents and grass are visual only; supply boxes have separate stable collision. Ground accents reuse the soil surface with a modest darker tint, with no added shader or particle effect.

`Farmyard Art Profile.asset` controls tracks, vegetation and supplies, and exposes authored supply/vegetation/small-debris prefabs. Generated fallback assets remain separate from artist-owned replacements. See [ENVIRONMENT_ART_PLAN.md](ENVIRONMENT_ART_PLAN.md) for the small initial shortlist and integration criteria. No external model or paid package is imported by this pass.

Rebuild scene 37, inspect tyre strip repetition, grass scale and supply composition at gameplay FOV, run farmhouse checks in Edit and Play mode, then bake lighting/reflections. Check the access track, evacuation pad and both field approaches during a full encounter. Source/math checks do not validate Unity compilation, rendering or performance.


### Working yard and freight construction (scene 38)

Rebuild with **Harvest > Build The Line Prototype**. This is a larger composition pass: the shed has a three metre repair bench, mounted tools and vise, parts storage, oil drums, stacked seed sacks on a slatted pallet, and a wall-mounted irrigation pipe rack. The silo has a tapered grain hopper on a braced frame, a loading slab, motor casing, discharge gate and a feed pipe connected to the silo. The farmhouse porch has palleted transit cases to the left of the entrance. Groups use separate simple collision proxies, leaving the access track and central road/evacuation lane clear.

The three existing freight blocks now have folded side ribs, roof seams, edge rails, lifting corners, double doors, hinges, locking bars, quiet identification markings and rubbed lower edges. The freight tower reads as a grain elevator with stiffeners, a maintenance ladder and a drive housing. Field fences use grain-aligned timber with caps, ground shoes and fixing hardware. Freight/fence bodies now use dimensioned meshes with metre-scale texture coordinates and lightmap UVs; the previous unit-cube scale is baked into their collider dimensions, preserving the original world collision volumes. Raised trim remains visual only.

`FarmyardComposition` runs after freight cover creation, independently of `FarmyardPolish`, terrain, navigation and encounter configuration. Five additional switches in `Farmyard Art Profile.asset` control the shed work area, grain station, freight detail, porch staging and fence detail. Named unit-scale group roots separate the art from collision and make later authored replacements easier. Painted metal variants reuse the approved roofing maps when available and preserve subsequent edits. Repeated round parts reuse meshes within each build. No external model, runtime effect or gameplay mechanic is added.

Run **Harvest > Validate Farmyard Composition** after rebuilding. It checks persistent meshes, lightmap UVs, tangents, materials, unit scale, clear road/access/entrance collision footprints and unchanged freight cover bounds. Also run farmhouse foundation checks in Edit and Play mode. Inspect the shed aisle, crop edges around the grain loader, porch entrance, container door hardware and the tower at gameplay FOV; then bake The Line lighting and reflections. Source review and independent placement math were performed here; Unity compilation, mesh unwrapping, visual appearance, bake cost and gameplay navigation remain unverified without the Editor.


### Natural road and field ground (scene 39)

Rebuild The Line for the new ground. The freight road and farmhouse access track are persistent meshes with matching mesh collision, replacing the long rectangular slabs and obsolete shoulder aprons. The gravel road retains a flat central travel lane, has irregular shoulder widths, tapers into the soil and fades into the terrain at its ends. The access track has rounded, weathered boundaries and meets the road at the same grade. Painted center dashes are removed from this rural gravel road. Continuous tyre wear uses feathered edges and variable strength instead of repeated dark rectangles.

`FarmNaturalGround` owns deterministic surface height and placement. A one metre terrain grid provides gentle field contours, shallow drainage swales and broader outer hills. Protected pads keep the farmhouse, shed, silo, freight structures, storage crates and fence footings level. Grass, crop stalks, yard supplies and optional debris now sample the actual triangulated ground/road/access height. Verge vegetation uses uneven loose clumps and bare gaps rather than regularly spaced tufts. The existing farmhouse contact aprons remain; the obsolete road/access apron strips are removed.

The opaque `Harvest/Farm Surface` shader adds an opt-in vertex blend between the existing gravel and soil textures, plus quiet broad tint variation. Red vertex values control soil coverage and green controls brightness. Both features default to zero on existing materials. New generated materials explicitly enable them; these copies preserve subsequent material edits. The source road and soil are migrated before ground generation. Natural ground currently requires these sources to use the Harvest surface shader; authored replacements should retain a compatible generated ground material. No new texture download, transparent ground overlay, post-processing effect or gameplay mechanic is added.

Run **Harvest > Validate Natural Ground** and **Harvest > Validate Farmyard Composition**, followed by farmhouse foundation checks in Edit and Play mode. Natural-ground validation checks shader status, material wiring, persistent mesh data, upward triangle winding, matched visual/collision meshes, sampled collision heights, original central road grade and protected prop pads. Inspect the road/soil transition from crouch height, tyre wear, crop contact, access junction and the whole encounter navigation before baking lighting/reflections. Independent geometry/source checks were performed here; Unity shader compilation, GPU appearance, collision cooking, navigation and performance still require the Editor.


### Detailed ground, checkpoint and forecourt (scene 40)

Rebuild The Line for a rounded gravel forecourt joining the farmhouse driveway, two timber parking stops to the left of the entrance, faded tyre wear, and crop/grass clearance around the yard. Supply stacks and farmhouse contact aprons use the shared surface height. Five existing concrete cover bodies gain beveled dimensioned meshes, casting panels, embedded lifting eyes, retaining hardware, faded safety paint, identification and irregular lower-edge grime. Original cover collision dimensions and encounter configuration are retained.

The farm shader now supports opt-in bounded height relief from packed texture alpha. Two cached 2K generated detail maps provide a fallback. After rebuilding, the Editor starts an asynchronous Poly Haven gravel/soil installation; let it finish before Play. Failed or cancelled downloads leave the fallback active. Retry with **Harvest > Materials > Install Ground Scans**. All four sources validate before the packed scanned maps replace matching generated material slots; artist-owned replacements and tuning are preserved. See [GROUND_SURFACES.md](GROUND_SURFACES.md) for source credits, physical scales, ownership, integrity receipt limits and memory considerations. This is an albedo/height improvement rather than a full normal/ARM ground system.

Run **Harvest > Validate Natural Ground**, **Harvest > Validate Checkpoint And Yard** and **Harvest > Validate Farmyard Composition**, then farmhouse checks in Edit and Play mode. Inspect the driveway seam, yard edges, grain/grass contact, gravel relief, parking circulation and barrier details from gameplay height before baking lighting/reflections. Source and placement review were performed here; Unity compilation, downloads/import, rendering, traversal and performance still need local Editor verification.
