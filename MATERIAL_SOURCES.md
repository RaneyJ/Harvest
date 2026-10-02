# Farmhouse PBR surface pass

Five scanned, tileable 2K material sets from **Poly Haven**, licensed **CC0**. These establish surface detail for the approved farmhouse. Local battle damage, bespoke trim/joins and final light baking follow visual approval.

| Surface | Source | Creators | Physical tile |
| --- | --- | --- | --- |
| Plaster | [white_plaster_02](https://polyhaven.com/a/white_plaster_02) | Rob Tuytel | 1 × 1 m |
| Timber | [rough_wood](https://polyhaven.com/a/rough_wood) | Rob Tuytel | 0.5 × 0.5 m |
| Roofing | [corrugated_iron_02](https://polyhaven.com/a/corrugated_iron_02) | Jenelle van Heerden, Sergej Majboroda | 2.7 × 2.7 m |
| Steel | [metal_plate_02](https://polyhaven.com/a/metal_plate_02) | Rob Tuytel | 2 × 2 m |
| Foundation | [concrete_floor_01](https://polyhaven.com/a/concrete_floor_01) | Rob Tuytel | 2 × 2 m |

Poly Haven: https://polyhaven.com — license: https://polyhaven.com/license. Credit remains recorded here and in the pinned source manifest. The material choices, subdued tints and normal strengths are a starting art direction to review in Unity.

## Install and review

1. Pull `prototype/the-line` and allow Unity to finish package resolution/compilation.
2. In Edit mode, run **Harvest > Install Farmhouse PBR Materials**. The first download is about 156 MB; wait for the Console completion message. The progress window supports cancellation; rerunning reuses verified downloads. Network errors leave completed cache entries available for retry.
3. Save any manual scene edits, then run **Harvest > Build The Line Prototype**. Scene version 24 rotates the main roof UVs to align corrugation with the roof pitch. If an artist-owned `AuthoredPrefab` is assigned, its geometry/UVs are preserved and must be reviewed separately.
4. Run **Harvest > Validate Farmhouse Materials**. It checks source hashes, import settings, actual packed channel samples and URP Lit wiring.
5. Run **Harvest > Build Farmhouse Material Review**. Save your current scene when prompted. Inspect the five surfaces under a white directional light, flat ambient light and a reflection probe, without fog or post-processing. Two-metre panels and one-metre blocks establish scale; spheres show highlight response. The scene is not added to build settings.
6. Reopen The Line and inspect the porch, wall panels, roof, stairs and upstairs firing windows at gameplay FOV. Check repeated patterns, stretched grain, oversized corrugations, excessive shine and noisy normal detail. Run the farmhouse foundation checks and traversal checks after rebuilding.

Review approval should be based on the neutral scene **and** the encounter. This environment cannot run Unity; compilation, image decoding/import, shader variants, probe rendering and in-game appearance still require Editor verification.

## Reproducibility and ownership

- `Assets/Harvest/Editor/FarmhouseMaterialSources.json` pins all 15 original files by direct HTTPS URL, byte count, provider MD5 and independently verified SHA-256. The editor verifies SHA-256 before accepting each file. No live search/API is needed during installation.
- `Library/HarvestMaterialCache/<surface>` stores the original downloads. Deleting Unity's Library folder requires downloading again. Keep the manifest as the reproducible recipe; imported texture assets can also be committed in a later art handoff.
- `Assets/Harvest/Art/Surfaces/PolyHaven/<surface>` receives `Albedo.jpg`, `NormalGL.png` and generated `URP_Mask.png`. Existing original texture files must still match their pinned hashes; an edited source is reported instead of silently replaced. Copy textures to an artist-owned path before editing them. Existing generated masks are retained; the validator reports packing differences.
- `Assets/Harvest/Materials/Farmhouse Foundation *.mat` are the existing shared material assets. Maps are assigned only when **all four map slots are empty**. Existing artist maps, custom shaders and deliberately edited values are preserved. Delete/clear maps deliberately before reapplying a source; rebuilding alone will not reset an authored material.
- The installer owns import settings at its source paths. It uses repeat wrapping, mipmaps, trilinear filtering, 8× anisotropy, 2K maximum size, high-quality platform compression and non-readable imported textures. Raw source decoding and mask packing happen only in the Editor.
- `Assets/Harvest/Scenes/FarmhouseMaterialReview.unity` is a generated review scene; rebuilding it replaces that scene. Save custom review arrangements under another name.

## Channel and scale rules

| Input | Import | URP Lit destination |
| --- | --- | --- |
| Albedo | sRGB color | Base Map |
| OpenGL normal | Normal Map, Y+; green channel unflipped | Normal Map |
| ARM red: ambient occlusion | Linear data | Packed mask green, used by Occlusion Map |
| ARM green: roughness | Linear data | Packed mask alpha = 1 − roughness, used by Metallic Map smoothness |
| ARM blue: metallic | Linear data | Packed mask red, used by Metallic Map |

The mask's unused blue channel is zero. Smoothness uses the metallic mask alpha, with the untouched base material's old constant replaced by a multiplier of 1. Albedo alpha is not used for smoothness. Normal and occlusion strengths are deliberately restrained per surface.

Generated farmhouse UV0 is in metres. The material's shared Base Map tiling is `1 / physical tile width` by `1 / physical tile height`; URP Lit applies that UV transform to its PBR maps. Lightmap UV2 remains separately packed. A sphere's primitive UVs are useful for reflections, but the dimensioned blocks and panels are the scale references. Authored meshes and prefab variants need their own UV review.

## Reduced-repeat plaster finish (version 25)

The approved plaster scan remains the source. **Harvest > Refine Farmhouse Plaster** derives a seamless 4m tile at 4096 × 4096 rather than visibly repeating a 1m patch across the house. Sixteen deterministic source phase offsets are blended with periodic smooth weights; all three maps use identical coordinates. Color blending is linear, normals are renormalized, and the packed mask stays linear. No new shader or runtime blending pass is introduced. This is surface variation, not localized battle damage.

- Run refinement once after installation, then rebuild The Line. Baking can take time; the progress window supports cancellation. All maps are staged in the source cache before the final asset files are copied. Cancellation during baking leaves the current material intact.
- Output: `Assets/Harvest/Art/Surfaces/PolyHaven/PlasterFinish/{Albedo.png,NormalGL.png,URP_Mask.png}`. Existing complete output sets are reused; partial sets are reported instead of silently overwritten. Move that generated folder aside to deliberately rebake.
- Only the exact original installer map combination on the plaster material is upgraded. Relative density/phase is retained by dividing its Base Map scale and offset by four. Other material settings are preserved. Artist-owned replacements are skipped.
- The output uses 4K import settings and approximately 1024 pixels/metre at default density. Compare close-up grain with the original if needed; the goal is less repetition with a bounded texture size. The original source maps remain available for comparison.
- **Validate Farmhouse Materials** also checks the derived map import settings. The material review scene labels the derived tile as a 4m finish. Inspect wall joins and the plaster finish in both scenes before considering this correction approved.
