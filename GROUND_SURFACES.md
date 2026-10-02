# Ground, checkpoint and farmhouse forecourt — scene 40

The forecourt follows the approved farmhouse concept: a broad, rounded gravel yard in front of the porch, with two farm parking bays to the left and an unobstructed approach to the front door. Its access track meets the freight road. The yard is approximately 15 × 10 metres before the feathered boundary; its centre follows the farmhouse layout. Weathered timber wheel stops, recessed tyre wear and existing supply stacks establish the parking area without adding vehicles or gameplay systems.

## Ground surfaces and source credits

| Surface | Source and author | Physical tile width | Maps used |
| --- | --- | --- | --- |
| Road and forecourt gravel | [Gravelly Sand](https://polyhaven.com/a/gravelly_sand), Dario Barresi | 2.5 m | 2K diffuse JPG and displacement PNG |
| Field soil and gravel margins | [Brown Mud](https://polyhaven.com/a/brown_mud), Rob Tuytel | 1.3 m | 2K diffuse JPG and displacement PNG |

Both assets are published under [Poly Haven’s CC0 license](https://polyhaven.com/license). Fixed downloads use `https://dl.polyhaven.org/file/ph-assets/Textures/{jpg|png}/2k/{asset}/{asset}_{diff|disp}_2k.{jpg|png}`. Downloads and import happen in the Unity Editor on your computer; they have not been performed in the development environment.

`FarmGroundScans` downloads the four files asynchronously to `Library/HarvestGroundScanCache`, checks image decoding and 2048 × 2048 resolution, then records URL, byte count and SHA-256 in a local receipt. Subsequent imports must match that receipt. These are first-download integrity receipts, **not independently pinned publisher checksums**. Corrupt cached files are downloaded again against their retained receipt; partial downloads are discarded on cancellation. Material replacement waits until all sources validate and both packed assets exist.

Diffuse RGB is stored as sRGB; displacement is stored in linear alpha. The opaque farm shader derives a bounded world-space lighting normal from that height. It does not displace geometry or change collision. This is an albedo/height pass, not a complete scanned normal/roughness/AO material system. Defaults leave other farm materials’ relief disabled. Ground tint variation and opaque gravel-to-soil blending remain available.

`FarmGroundTextures` provides deterministic 2K pebble/soil fallback maps while downloads are pending or unavailable. Their first generation can take time and shows an Editor progress bar; generated assets are cached for subsequent rebuilds. Both scanned and fallback tiles use mipmaps, repeat wrapping, trilinear filtering and anisotropy. Two uncompressed 2K RGBA tiles with mipmaps occupy approximately 43 MiB before platform-specific handling; this is not a measured runtime budget.

## Ownership and extension

- `FarmNaturalGround` owns the yard mesh, matching mesh collision, protected grade, rounded edges and shared height sampling. Crop roots and verge plants avoid the parking area. Existing house contact aprons and supply stacks sample the updated surface.
- `FarmGroundScans.ApplyPacked` upgrades only slots still referencing the generated detail maps. Artist-owned texture replacements remain intact. Default legacy tints are neutralized for photographic albedo; custom tint edits are retained.
- Generated textures live under `Assets/Harvest/Rendering/Surfaces` and `Assets/Harvest/Art/Surfaces/FarmGroundScans`. Material variants live under `Assets/Harvest/Materials`. Generated mesh assets live under `Assets/Harvest/Art/Generated/FarmGround` and `Checkpoint`.
- `CheckpointArt` dresses the five established concrete cover bodies with dimensioned beveled meshes, casting panels, lifting eyes, straps, fasteners, identification, safety paint and lower-edge grime. It uses the installed farmhouse foundation material when available. The original cover collider centres and dimensions are retained; the fittings are visual only. Barricade ordering and wear seeds are deterministic.
- Parking stops use separate low box colliders. No encounter, weapon, health, suppression or AI mechanic is changed.

## Local acceptance pass

1. Save manual scene edits, pull the branch, then run **Harvest > Build The Line Prototype** for scene 40.
2. Let the ground installation finish before entering Play mode. Entering Play, compilation or domain reload cancels pending downloads safely. To resume, use **Harvest > Materials > Install Ground Scans**. Finish any farmhouse material installation first.
3. Run **Harvest > Validate Natural Ground**, **Harvest > Validate Checkpoint And Yard** and **Harvest > Validate Farmyard Composition**. Run farmhouse foundation checks in Edit and Play mode.
4. Review gravel scale and lighting relief from standing and crouch height; yard perimeter fading; the access junction; supply contact; concrete fittings and markings; both parking bays and the clear entrance. Walk the road, porch and forecourt and observe marine navigation during a full wave.
5. Bake lighting and reflections once the surface/placement review passes. Recheck shadows at the stops, supplies and barrier bases.

Source review and independent placement/geometry checks were performed during development. Unity compilation, image import, mesh unwrapping, GPU rendering, lighting, navigation and performance remain unverified without a Unity Editor. The source-only commit generates the scene and surface assets locally when rebuilt.
