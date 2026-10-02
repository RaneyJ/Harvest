# Harvest environment art replacements

The farmhouse establishes the current composition and material direction. Its generated room/prop architecture remains a fallback and a placement reference. The next large gains come from a small, coherent authored asset set, starting outdoors.

| Priority | Candidate | Intended role | Integration work |
|---|---|---|---|
| 1 | [Poly Haven Rock 09](https://polyhaven.com/a/rock_09) | Small stone clusters at field boundaries and soil contacts | Inspect actual imported scale, select/reduce mesh detail, use modest texture resolution and build a ground-origin cluster prefab. Site lists approximately 23K triangles; do not scatter the full-resolution mesh widely. |
| 2 | [Poly Haven Grass Medium 01](https://polyhaven.com/a/grass_medium_01) | Authored verge weeds/grass, tuned toward dry late-season tones | The site lists a geometry-nodes scatter setup at about 2M triangles; extract a small tuft rather than importing/scattering the full setup, create URP materials and LODs, check alpha clipping/shadows before replacing fallback plants. Do not assume a downloaded procedural asset is a ready-made Unity prefab. |
| 3 | [Poly Haven Wooden Military Crate](https://polyhaven.com/a/wooden_military_crate) | Repeated agricultural/evacuation storage | Site lists about 23K triangles; import model/PBR maps, choose or create LODs, build URP material channels, check bounds and create a visual prefab for yard supplies. |

These are a visual shortlist, not imported assets or a verified Unity-ready pack. Poly Haven describes its assets as CC0: https://polyhaven.com/license. Asset-file licenses and sources should remain recorded when importing. Start with one asset, inspect it in the existing light rig, then expand only after quality and frame-time checks. No purchases are needed for these candidates.

The wheat needs a separate realistic crop asset with useful near/mid/far LODs. The initial store search mainly returned stylized packs, which do not fit the approved direction. Keep the current crop geometry until a specific suitable asset is verified; avoid buying a broad farm pack merely for one crop.

## Prepared replacement points

Scene 37 adds `Assets/Harvest/Data/Farmyard Art Profile.asset`. Supply, vegetation and small ground-debris prefabs are optional and artist-owned. Their roots must use metres, with origins at soil level, and contain no colliders, MonoBehaviours or rigidbodies. Supply collision remains a separate 1.4m x 0.8m footprint, 0.7m tall; fit authored visuals to that envelope or revise geometry and review circulation deliberately. Keep vegetation/debris small enough for the roadside placements; large rocks need separate collision/layout authoring.

Track surfaces, vegetation and supplies have independent toggles. Generated geometry stays under `Assets/Harvest/Art/Generated/Farmyard`; keep authored prefabs elsewhere. Preserve downloaded originals, derive game-ready meshes/materials in an authored folder and leave the fallback available for comparison.

## Acceptance review

Check game FOV, metre scale, soil contact, shader conversion, normals/roughness, LOD transitions, repeated silhouettes, shadow cost and material count. Rebuild/bake after replacements, verify the central road/evacuation/access routes, then measure a full encounter in a standalone build. This execution environment cannot validate Unity imports, rendering or frame time.
