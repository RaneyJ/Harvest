# Harvest visual foundation

The Line now targets Unity 6000.3 / URP 17.3 with Render Graph enabled and linear color space. This is the first rendering and surface pass toward the Reach reference. Characters, weapon geometry and animations remain prototype assets.

## Pull and rebuild

1. Let Unity Package Manager resolve URP after pulling `prototype/the-line`.
2. Save manual scene changes, then run **Harvest > Build The Line Prototype**. Version 20 rebuilds the generated scene, creates persistent rendering assets and migrates Harvest Standard materials in place. Weapon tuning, audio and configured waves remain on their existing assets.
3. Run **Harvest > Validate Visual Foundation**. Check the Console for shader errors.
4. In Play mode inspect farmhouse interiors, shadows, rifle tracers, plasma, grenade fading, armor/charge tint feedback and sustained-fire suppression. Also check upstairs movement, cover and all waves. The build environment used for this change has no Unity Editor; these runtime and visual checks still need to be run in Unity.

## Authoring boundaries

- `HarvestRenderSetup` owns URP configuration and material migration. Defaults are set only when a rendering asset is first created. All quality levels reference the same starting pipeline; tune separate performance tiers later.
- `Assets/Harvest/Rendering/Harvest Look.asset` controls sun, ambient colors and fog. Rebuild to apply changes.
- `Harvest Post.asset` contains editable ACES tone mapping, bloom, grading and vignette. Volume changes update in the scene without a rebuild; the scene builder preserves this asset.
- `Harvest Sky.mat` controls the procedural sky. `Harvest URP.asset` controls shadow quality and rendering budgets.
- `HarvestSurfaceLibrary` creates deterministic tiled starter textures at editor time. The farm shader maps them in world space to avoid stretched walls. This is placeholder texture work, not scanned/PBR final art. Materials already carrying authored textures are preserved.
- `FarmVisualPass` owns lighting and distant scenery. Distant objects have no colliders and do not extend the combat/navmesh bounds. Two unshadowed interior lights, one 128px reflection probe refreshed on scene load, four sun cascades and a single suppression pass establish a bounded starting budget. Nothing here is a measured performance guarantee.
- Suppression remains camera-local gameplay state. Its renderer feature runs only on game cameras with active pressure, after post-processing. GUI hit indicators and HUD stay sharp. The old Built-in `OnRenderImage` effect is removed.

## Replacing weapon art

Each WeaponDefinition has separate `ViewModelPrefab` and `WorldModel` references. Assign authored prefabs in meters with +Z forward, an identity root and a grip-centered origin; adjust the existing view/ADS poses for the new asset. First-person child colliders are disabled by the builder. World prefabs must be visual-only (no colliders, gameplay scripts or damage targets).

Keep authored world prefabs outside `Assets/Harvest/Prefabs/<DisplayName> Model.prefab`, which is reserved for the generated fallback. Rebuilds preserve world references to any other path and use the assigned view prefab for the first-person model. On an existing NPC prefab, refresh its `Held weapon` child manually when switching world art; rebuilding does not overwrite previously authored NPC presentation. A child named `Bolt` can participate in the current hunting rifle bolt pose.

Keep imported characters' renderers/rigs separate from Vitality, CombatTarget, Armor, weapons and navigation. Detailed character animation integration is a subsequent pass; this update does not provide finished marine/Covenant rigs or first-person reload animation.

## Next art milestone

Finish one farmhouse courtyard before extending the map: authored house/props and vegetation, a finished rifle with hands/reload, one rigged marine and one Covenant enemy. Review these in motion and during a full wave. Baked indirect lighting, light probes, authored normal maps, final VFX and measured LOD/performance tiers follow once assets and geometry stabilize.
