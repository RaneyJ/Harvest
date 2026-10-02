# Militia service rifle — authored asset 01

A fictional exterior prop modeled from the approved Halo: Harvest civilian militia rifle concept. This first authored version establishes the long bullpup silhouette, open carry bridge, protected iron sights, ribbed polymer handguard, angled pistol grip, rear magazine, shoulder pad and colony inspection markings. It contains no functional internal mechanism.

![Baked material review](Review/Baked.jpg)

## Files

- `MilitiaServiceRifle.blend`: editable source with individual components, bevel/normal modifiers, procedural materials, lights and four review cameras. It opens in Blender 4.5.3 LTS. The material nodes need no external textures.
- `MilitiaServiceRifle.glb`: portable model preview with embedded 2K textures; uses the standard Draco mesh compression extension.
- `Review/`: actual Cycles renders of the model, including the baked game-material preview.
- `ApprovedConcept.png`: the approved concept reference.
- `../../../Assets/Harvest/Art/Weapons/MilitiaServiceRifle/`: detailed FBX, static distance FBX, base color, tangent normal, metal/roughness and Unity mask textures.
- `build_rifle.py`, `export_rifle.py`, `validate_exports.py`: reproducible modeling, baking/export and round-trip checks.

## Asset specification

| Item | Value |
| --- | --- |
| Length / width / height | 1.055 / 0.161 / 0.361 metres |
| Detailed model | 59,488 triangles, five mesh parts |
| Distance model | 19,036 triangles, one static mesh |
| Export material | One shared material, 2048 × 2048 atlas |
| Detailed parts | Body, Grip, Magazine, Trigger, Bolt |
| Animation preparation | Separate magazine, trigger and bolt with local pivots; no animations or hands rig yet |
| Alignment | Grip-centred root; Unity +Z forward, +Y up |
| Markers | Muzzle, GripAnchor, MagazineAnchor |
| Collision | Visual-only asset; use the existing gameplay pickup/actor collision |

`ServiceRifle_BaseColor.png` is sRGB. Normal, metal/roughness and Unity mask maps are linear. Metal/roughness uses R=1, G=roughness, B=metallic. Unity mask uses R=metallic, G=occlusion (1), B=unused, A=smoothness. The normal map uses the Blender/OpenGL tangent convention. Roughness is currently material-based; color variation, edge detail and fine normal grain are baked from the source. Artist-authored dirt, damage painting and hero close-up polish can build on this version.

The detailed mesh is intended for near views. The distance mesh is an initial reduction for actor/pickup use, not a measured final performance budget. Most small components use actual geometry; subsequent optimization can bake more hardware into normals. The separated parts are prepared for future animation, not a completed animation rig.

## Unity review

After pulling, run **Harvest > Art > Build Militia Service Rifle Prefab**. The explicit menu configures the FBX and textures, creates a URP Lit material and creates a visual prefab with two LODs. Existing created prefab and material edits are retained. The menu checks imported length and height before saving the prefab.

Inspect both sides, the muzzle, material highlights, UV seams and the LOD transition in Unity. Once approved, the resulting prefab can be assigned to the Service Rifle's `ViewModelPrefab` and `WorldModel` references. Existing NPC held models may need their presentation refreshed, as the scene builder preserves authored NPC presentation. This asset commit does not change the weapon definition or replace the active gameplay model automatically.

Blender modeling, source save/reopen, Cycles rendering, texture baking and FBX/GLB round trips were tested. `validation.json` and `round_trip_validation.json` contain measured results. Unity compilation, importer execution, in-game framing, LOD appearance and performance remain to be checked in the Editor.

## Rebuild

With the same Blender version installed, run the scripts through Blender background mode, or use the Python module:

```sh
uv venv .venv --python 3.11
uv pip install --python .venv/bin/python bpy==4.5.3
.venv/bin/python build_rifle.py
.venv/bin/python export_rifle.py
.venv/bin/python validate_exports.py
```

Run from this directory. Scripts resolve project paths from their own location. `build_rifle.py` regenerates and overwrites the source file and review renders; make a separate copy before hand-editing the model. `export_rifle.py` reads the current source file, so manual geometry edits can be rebaked without running the generator again. Changes to the silhouette require updating the independent expected dimensions in the validation workflow as appropriate. Keep `.blend` sources outside Unity's Assets folder so Unity does not depend on a local Blender install.
