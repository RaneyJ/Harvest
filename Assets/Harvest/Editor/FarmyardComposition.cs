using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Harvest.Editor
{
    // Complete, independently switchable prop groups. Unit-scale roots keep trim dimensions
    // independent of the scaled greybox cover. Gameplay scripts and cover anchors stay separate.
    public static class FarmyardComposition
    {
        const string RootName = "Farmyard working spaces";
        static readonly Dictionary<string, Mesh> RoundMeshes = new Dictionary<string, Mesh>();
        static Material timber, steel, painted, dark, canvas, pale;

        public static void Build()
        {
            RoundMeshes.Clear();
            var profile = AssetDatabase.LoadAssetAtPath<FarmyardArtProfile>(FarmyardPolish.ProfilePath);
            if (profile == null) throw new InvalidOperationException("Build FarmyardPolish before FarmyardComposition.");
            timber = Surface("Timber", "Yard aged timber", new Color(.32f, .27f, .19f));
            steel = Surface("Steel", "Yard hardware", new Color(.32f, .34f, .31f));
            painted = Painted("Yard freight blue", new Color(.27f, .36f, .38f));
            dark = WeaponModelGeometry.MaterialFor("Yard dark rubber", new Color(.075f, .082f, .07f));
            canvas = WeaponModelGeometry.MaterialFor("Yard seed sacks", new Color(.48f, .43f, .30f));
            pale = WeaponModelGeometry.MaterialFor("Yard faded stencil", new Color(.66f, .64f, .51f));
            Transform root = Group(null, RootName, Vector3.zero);
            if (profile.EnableShedWorkArea) Shed(root);
            if (profile.EnableGrainStation) GrainStation(root);
            if (profile.EnableFreightDetails)
            {
                Container(root, "Cargo left", "HARVEST\nAGRI / 014", painted);
                Container(root, "Cargo right", "SEED / 207", Painted("Yard freight olive", new Color(.36f, .38f, .28f)));
                Container(root, "Burned freight car", "FREIGHT / 031", Painted("Yard burned freight", new Color(.19f, .17f, .14f)));
                Tower(root);
            }
            if (profile.EnablePorchStaging) Porch(root);
            if (profile.EnableFenceDetails) FieldFences(root);
        }

        [MenuItem("Harvest/Validate Farmyard Composition")]
        public static void Validate()
        {
            GameObject root = GameObject.Find(RootName);
            if (root == null) { Debug.LogError("Rebuild The Line before validating the farmyard."); return; }
            Physics.SyncTransforms();
            var errors = new List<string>();
            if (root.GetComponentInChildren<MonoBehaviour>(true) != null || root.GetComponentInChildren<Rigidbody>(true) != null)
                errors.Add("Farmyard dressing must remain static environment art.");
            foreach (MeshFilter filter in root.GetComponentsInChildren<MeshFilter>(true))
            {
                Mesh mesh = filter.sharedMesh;
                if (mesh == null || !AssetDatabase.Contains(mesh) || mesh.uv2.Length != mesh.vertexCount || mesh.tangents.Length != mesh.vertexCount)
                    errors.Add("Missing persistent mesh, lightmap UVs or tangents: " + filter.name);
                MeshRenderer renderer = filter.GetComponent<MeshRenderer>();
                if (renderer == null || renderer.sharedMaterial == null) errors.Add("Missing yard material: " + filter.name);
                if (filter.transform.lossyScale != Vector3.one) errors.Add("Yard mesh dimensions must be baked into vertices: " + filter.name);
            }
            var house = Object.FindFirstObjectByType<FarmhouseFoundation>();
            foreach (Collider collider in root.GetComponentsInChildren<Collider>(true))
            {
                Bounds b = collider.bounds;
                if (Overlaps(b, -5, 5, -55, 75)) errors.Add("Yard collision overlaps central freight/evacuation lane: " + collider.name);
                if (Overlaps(b, -17, -7, -14, -10)) errors.Add("Yard collision overlaps farm access track: " + collider.name);
                if (house != null)
                {
                    FarmhouseLayout l = house.Layout;
                    if (Overlaps(b, l.WorldOrigin.x - 1, l.WorldOrigin.x + 1,
                        l.WorldOrigin.z - l.HalfDepth - l.PorchDepth - 1, l.WorldOrigin.z - l.HalfDepth + .2f))
                        errors.Add("Yard collision blocks farmhouse entrance: " + collider.name);
                }
            }
            // Art refinements must leave the established freight cover volumes exactly in place.
            CheckCover("Cargo left", new Vector3(-7, 1.4f, 10), new Vector3(3, 2.8f, 4), errors);
            CheckCover("Cargo right", new Vector3(8, 1.1f, 17), new Vector3(3, 2.2f, 5), errors);
            CheckCover("Burned freight car", new Vector3(-13, 2, 38), new Vector3(5, 4, 12), errors);
            CheckCover("Freight tower", new Vector3(23, 7, 27), new Vector3(4, 14, 4), errors);
            foreach (string error in errors) Debug.LogError("Farmyard composition: " + error);
            if (errors.Count == 0) Debug.Log("Farmyard composition checks passed. Review surface scale, crop clearance, ground contact and baked lighting in Game view.");
        }
        static bool Overlaps(Bounds bounds, float left, float right, float front, float back) =>
            bounds.min.x < right && bounds.max.x > left && bounds.min.z < back && bounds.max.z > front;
        static void CheckCover(string name, Vector3 point, Vector3 size, List<string> errors)
        {
            GameObject body = GameObject.Find(name); BoxCollider collider = body == null ? null : body.GetComponent<BoxCollider>();
            if (collider == null || (collider.bounds.center - point).sqrMagnitude > .000001f || (collider.bounds.size - size).sqrMagnitude > .000001f)
                errors.Add("Original freight cover bounds changed: " + name);
        }

        static void Shed(Transform parent)
        {
            Transform root = Group(parent, "Shed maintenance bay", new Vector3(-26.2f, -.05f, 18));
            // Three metre bench along the field wall; rear hay stacks and the open road side stay clear.
            Box(root, "Workbench top", timber, new Vector3(0, .91f, 0), new Vector3(.86f, .09f, 3));
            foreach (float z in new[] { -1.28f, 1.28f }) foreach (float x in new[] { -.30f, .30f })
            {
                Box(root, "Bench leg", timber, new Vector3(x, .43f, z), new Vector3(.095f, .86f, .095f));
                Box(root, "Bench foot plate", steel, new Vector3(x, .035f, z), new Vector3(.13f, .07f, .13f));
            }
            Box(root, "Lower parts shelf", timber, new Vector3(0, .23f, 0), new Vector3(.72f, .055f, 2.65f));
            Box(root, "Bench wall tool board", timber, new Vector3(-.54f, 1.67f, 0), new Vector3(.055f, 1.10f, 2.85f));
            foreach (float z in new[] { -1.2f, -.6f, 0, .6f, 1.2f })
            {
                // Hammers/spanners: handles, heads and two mounting pegs, rather than floating marks.
                Box(root, "Hanging tool handle", z == 0 ? timber : steel, new Vector3(-.487f, 1.65f, z), new Vector3(.05f, .40f, .045f));
                Box(root, "Hanging tool head", steel, new Vector3(-.467f, 1.85f, z), new Vector3(.06f, .07f, .16f));
                Pipe(root, "Tool peg", steel, new Vector3(-.51f, 1.88f, z), new Vector3(-.42f, 1.88f, z), .015f);
            }
            Box(root, "Bench vise base", steel, new Vector3(.13f, .98f, -.90f), new Vector3(.25f, .045f, .30f));
            foreach (float z in new[] { -1.02f, -.85f }) Box(root, "Vise jaw", steel, new Vector3(.13f, 1.07f, z), new Vector3(.23f, .14f, .05f));
            Pipe(root, "Vise lead screw", steel, new Vector3(.13f, 1.015f, -.98f), new Vector3(.13f, 1.015f, -.62f), .022f);
            Pipe(root, "Vise turning handle", steel, new Vector3(.02f, 1.015f, -.63f), new Vector3(.24f, 1.015f, -.63f), .012f);
            Crate(root, new Vector3(0, .955f, .9f), new Vector3(.62f, .30f, .65f), "Parts bin");
            Crate(root, new Vector3(0, .258f, -.55f), new Vector3(.57f, .45f, .64f), "Spare filters");
            Collision(root, "Workbench collision", new Vector3(0, .4775f, 0), new Vector3(.86f, .955f, 3));
            Label(root, "REPAIRS / FIELD 04", new Vector3(-.505f, 2.02f, 0), -90, .035f);

            Transform drums = Group(parent, "Shed oil and irrigation stock", new Vector3(-20.05f, -.05f, 21.65f));
            foreach (Vector3 p in new[] { Vector3.zero, new Vector3(.85f, 0, -.12f) }) Barrel(drums, p);
            Transform stock = Group(parent, "Shed seed pallet", new Vector3(-22.1f, -.05f, 16.35f));
            Pallet(stock, Vector3.zero, new Vector2(1.6f, 1.15f));
            for (int layer = 0; layer < 3; layer++) for (int side = -1; side <= 1; side += 2)
            {
                Vector3 p = new Vector3(side * .36f + (layer == 1 ? .05f : 0), .16f + .18f * layer, 0);
                Box(stock, "Bound seed sack", canvas, p + Vector3.up * .09f, new Vector3(.65f, .18f, .78f), .055f);
                Box(stock, "Sack stitched end", pale, p + new Vector3(0, .09f, -.395f), new Vector3(.55f, .035f, .012f));
            }
            Collision(stock, "Seed pallet collision", new Vector3(0, .35f, 0), new Vector3(1.6f, .70f, 1.15f));
            // Pipe rack stays on the rear wall above the hay, not across the work aisle.
            Transform rack = Group(parent, "Shed spare pipe rack", new Vector3(-23, 0, 22.69f));
            foreach (float x in new[] { -1.5f, 1.5f })
            {
                Box(rack, "Pipe rack wall plate", steel, new Vector3(x, 2.38f, 0), new Vector3(.10f, .43f, .065f));
                Beam(rack, "Rack shelf bracket", steel, new Vector3(x, 2.45f, -.02f), new Vector3(x, 2.45f, -.40f), .055f);
                Beam(rack, "Rack brace", steel, new Vector3(x, 2.2f, -.02f), new Vector3(x, 2.45f, -.35f), .055f);
            }
            for (int i = 0; i < 3; i++) Pipe(rack, "Stored irrigation pipe", steel, new Vector3(-1.8f, 2.5255f, -.10f - i * .12f), new Vector3(1.8f, 2.5255f, -.10f - i * .12f), .048f);
        }

        static void GrainStation(Transform parent)
        {
            // Between crop rows in front of the silo: no crop removal or road narrowing.
            Transform root = Group(parent, "Silo grain loading station", new Vector3(24, -.05f, 43.65f));
            Box(root, "Loading slab", Surface("Foundation", "Yard loading concrete", new Color(.42f, .42f, .37f)), new Vector3(0, .07f, 0), new Vector3(2.5f, .14f, 2.25f));
            foreach (float x in new[] { -.83f, .83f }) foreach (float z in new[] { -.75f, .75f })
                Box(root, "Hopper support leg", steel, new Vector3(x, 1.25f, z), new Vector3(.09f, 2.22f, .09f));
            foreach (float z in new[] { -.75f, .75f })
                Box(root, "Hopper top crossmember", steel, new Vector3(0, 2.20f, z), new Vector3(1.76f, .10f, .10f));
            foreach (float x in new[] { -.83f, .83f })
                Box(root, "Hopper top side member", steel, new Vector3(x, 2.20f, 0), new Vector3(.10f, .10f, 1.60f));
            foreach (float z in new[] { -.75f, .75f })
                Beam(root, "Hopper cross brace", steel, new Vector3(-.83f, .25f, z), new Vector3(.83f, 1.7f, z), .055f);
            Round(root, "Grain hopper", painted, new Vector3(0, 1.1f, 0),
                new Vector2(0, 0), new Vector2(.12f, 0), new Vector2(.13f, .20f), new Vector2(.82f, .95f), new Vector2(.82f, 1.25f), new Vector2(0, 1.25f));
            Round(root, "Hopper lid rim", steel, new Vector3(0, 2.335f, 0),
                new Vector2(0, 0), new Vector2(.86f, 0), new Vector2(.86f, .045f), new Vector2(0, .045f));
            foreach (float x in new[] { -.83f, .83f })
                Box(root, "Motor support side channel", steel, new Vector3(x, .80f, 0), new Vector3(.09f, .09f, 1.60f));
            Box(root, "Motor support crossmember", steel, new Vector3(0, .80f, .15f), new Vector3(1.76f, .07f, .13f));
            Pipe(root, "Motor feed coupling", steel, new Vector3(.35f, 1.10f, .15f), new Vector3(.35f, 1.35f, .20f), .095f);
            Box(root, "Feed motor housing", painted, new Vector3(.67f, 1.02f, .15f), new Vector3(.48f, .37f, .52f));
            for (int i = 0; i < 5; i++) Box(root, "Motor cooling fin", steel, new Vector3(.925f, .89f + i * .06f, .15f), new Vector3(.025f, .025f, .44f));
            Pipe(root, "Silo feed elbow upright", steel, new Vector3(.35f, 1.35f, .2f), new Vector3(.35f, 2.85f, .2f), .095f);
            Pipe(root, "Silo feed elbow run", steel, new Vector3(.35f, 2.85f, .2f), new Vector3(.35f, 2.85f, 1.50f), .095f);
            foreach (float z in new[] { .36f, 1.26f }) Box(root, "Feed pipe coupling", painted, new Vector3(.35f, 2.85f, z), new Vector3(.24f, .24f, .09f));
            Pipe(root, "Hopper discharge", steel, new Vector3(0, 1.1f, 0), new Vector3(0, .52f, 0), .10f);
            Box(root, "Discharge gate", painted, new Vector3(.16f, .85f, 0), new Vector3(.36f, .06f, .23f));
            Box(root, "Control casing mounting bracket", steel, new Vector3(-.90f, 1.65f, -.75f), new Vector3(.15f, .20f, .07f));
            Box(root, "Grain control casing", painted, new Vector3(-.98f, 1.65f, -.75f), new Vector3(.18f, .35f, .34f));
            Box(root, "Control inset", dark, new Vector3(-1.08f, 1.70f, -.75f), new Vector3(.025f, .15f, .23f));
            Box(root, "Loader warning plaque", pale, new Vector3(0, 1.95f, -.833f), new Vector3(.42f, .18f, .018f));
            Label(root, "04 / GRAIN", new Vector3(0, 1.95f, -.845f), 0, .024f, new Color(.16f, .18f, .14f));
            Collision(root, "Grain station collision", new Vector3(0, 1.19f, 0), new Vector3(2.5f, 2.38f, 2.25f));
        }

        static void Container(Transform parent, string blockName, string marking, Material material)
        {
            GameObject body = GameObject.Find(blockName);
            if (body == null) throw new InvalidOperationException("Missing freight block: " + blockName);
            Vector3 size = body.transform.localScale, h = size * .5f;
            // Keep the solid body and original collider. Art roots are never children of its scaled transform.
            FinishBody(body, material, size);
            Transform root = Group(parent, blockName + " construction", body.transform.position);
            root.rotation = body.transform.rotation;
            foreach (int side in new[] { -1, 1 })
            {
                for (int i = 0; i < Mathf.FloorToInt(size.z / .32f); i++)
                {
                    float z = -h.z + .19f + i * .32f;
                    Box(root, "Container folded side rib", material, new Vector3(side * (h.x + .015f), 0, z), new Vector3(.03f, size.y - .18f, .075f));
                }
                foreach (float y in new[] { -h.y + .065f, h.y - .065f })
                    Box(root, "Container long edge rail", steel, new Vector3(side * (h.x + .025f), y, 0), new Vector3(.07f, .13f, size.z + .07f));
                foreach (float z in new[] { -h.z, h.z })
                {
                    Box(root, "Freight corner ground packer", dark, new Vector3(side * (h.x - .045f), -h.y - .025f, z), new Vector3(.17f, .05f, .16f));
                    Box(root, "Container corner post", steel, new Vector3(side * (h.x - .045f), 0, z), new Vector3(.15f, size.y, .12f));
                    foreach (float y in new[] { -h.y + .09f, h.y - .09f })
                        Box(root, "Container lifting corner", dark, new Vector3(side * (h.x - .045f), y, z - .01f), new Vector3(.17f, .18f, .16f));
                }
                float doorX = side * size.x * .235f;
                Box(root, "Freight double door", material, new Vector3(doorX, 0, -h.z - .02f), new Vector3(size.x * .46f, size.y - .24f, .04f));
                foreach (float x in new[] { doorX - size.x * .14f, doorX + size.x * .14f })
                {
                    Pipe(root, "Door locking bar", steel, new Vector3(x, -h.y + .22f, -h.z - .061f), new Vector3(x, h.y - .22f, -h.z - .061f), .018f);
                    Box(root, "Lock bar handle", steel, new Vector3(x + .06f, -h.y + .62f, -h.z - .087f), new Vector3(.16f, .045f, .025f));
                    foreach (float y in new[] { -.6f * h.y, 0, .6f * h.y })
                        Box(root, "Door locking bracket", steel, new Vector3(x, y, -h.z - .076f), new Vector3(.08f, .065f, .02f));
                }
                foreach (float y in new[] { -.65f * h.y, .65f * h.y })
                    Box(root, "Door hinge", steel, new Vector3(side * (h.x - .14f), y, -h.z - .06f), new Vector3(.16f, .095f, .04f));
            }
            for (int i = 0; i < Mathf.FloorToInt(size.z / .45f); i++)
                Box(root, "Container roof seam", material, new Vector3(0, h.y + .013f, -h.z + .24f + i * .45f), new Vector3(size.x - .18f, .025f, .035f));
            Box(root, "Door centre gasket", dark, new Vector3(0, 0, -h.z - .044f), new Vector3(.028f, size.y - .20f, .012f));
            Box(root, "Freight marking panel", material, new Vector3(-size.x * .235f, .32f, -h.z - .048f), new Vector3(size.x * .24f, .45f, .018f));
            Label(root, marking, new Vector3(-size.x * .235f, .32f, -h.z - .060f), 0, .04f);
            // Small quiet paint chips use deterministic placements, not random rebuilding or floating decals.
            for (int i = 0; i < 9; i++)
            {
                float z = -h.z + .35f + i * (size.z - .70f) / 8;
                Box(root, "Lower edge rubbed paint", steel, new Vector3(h.x + .004f, -h.y + .16f + (i % 3) * .035f, z), new Vector3(.008f, .035f, .10f));
            }
        }

        static void Tower(Transform parent)
        {
            GameObject body = GameObject.Find("Freight tower");
            if (body == null) throw new InvalidOperationException("Missing freight tower.");
            FinishBody(body, painted, body.transform.localScale);
            Transform root = Group(parent, "Freight grain elevator construction", body.transform.position);
            foreach (float x in new[] { -1.92f, 1.92f }) foreach (float z in new[] { -2.025f, 2.025f })
                Box(root, "Elevator corner upright", steel, new Vector3(x, 0, z), new Vector3(.16f, 14, .09f));
            for (int storey = 0; storey < 5; storey++)
            {
                float y = -6.65f + storey * 2.65f;
                foreach (float z in new[] { -2.04f, 2.04f })
                {
                    Box(root, "Elevator horizontal channel", steel, new Vector3(0, y, z), new Vector3(3.85f, .10f, .07f));
                    Beam(root, "Elevator diagonal stiffener", steel, new Vector3(-1.82f, y + .12f, z), new Vector3(1.82f, y + 2.5f, z), .075f);
                }
            }
            Box(root, "Elevator foundation shoe", Surface("Foundation", "Yard loading concrete", new Color(.42f, .42f, .37f)), new Vector3(0, -6.99f, 0), new Vector3(4.20f, .12f, 4.20f));
            Box(root, "Elevator machinery cap", steel, new Vector3(0, 7.15f, 0), new Vector3(4.25f, .30f, 4.25f));
            Box(root, "Elevator top drive housing", painted, new Vector3(0, 7.63f, 0), new Vector3(1.8f, .66f, 2.3f));
            foreach (float x in new[] { -.42f, .42f }) Pipe(root, "Elevator ladder stile", steel, new Vector3(x, -6.7f, -2.13f), new Vector3(x, 6.6f, -2.13f), .026f);
            for (int rung = 0; rung < 42; rung++) Pipe(root, "Elevator ladder rung", steel, new Vector3(-.42f, -6.55f + rung * .31f, -2.13f), new Vector3(.42f, -6.55f + rung * .31f, -2.13f), .018f);
            Box(root, "Elevator identification plate", pale, new Vector3(1.05f, -4.8f, -2.055f), new Vector3(.92f, .6f, .02f));
            Label(root, "HARVEST\nELEVATOR 04", new Vector3(1.05f, -4.8f, -2.073f), 0, .032f, new Color(.18f, .21f, .17f));
        }

        static void Porch(Transform parent)
        {
            var house = Object.FindFirstObjectByType<FarmhouseFoundation>();
            if (house == null) return;
            FarmhouseLayout l = house.Layout;
            Transform root = Group(parent, "Porch evacuation staging", l.WorldOrigin + new Vector3(-l.HalfWidth + 2.2f, l.GroundFloorTop, -l.HalfDepth - .9f));
            Pallet(root, Vector3.zero, new Vector2(1.65f, .95f));
            Crate(root, new Vector3(-.34f, .16f, 0), new Vector3(.83f, .52f, .68f), "Transit case");
            Crate(root, new Vector3(.53f, .16f, 0), new Vector3(.49f, .38f, .65f), "Emergency case");
            foreach (float x in new[] { -.58f, -.12f }) Box(root, "Transit case strap", steel, new Vector3(x, .69f, 0), new Vector3(.035f, .02f, .70f));
            Collision(root, "Porch staging collision", new Vector3(0, .34f, 0), new Vector3(1.65f, .68f, .95f));
            Label(root, "FIELD 04", new Vector3(-.34f, .43f, -.36f), 0, .028f);
        }

        static void FieldFences(Transform parent)
        {
            foreach (MeshRenderer renderer in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
            {
                if (renderer.name != "Fence post" && renderer.name != "Field fence rail") continue;
                Vector3 size = renderer.transform.localScale;
                FinishBody(renderer.gameObject, timber, size);
                if (renderer.name != "Fence post") continue;
                Transform root = Group(parent, "Fence post hardware", renderer.transform.position);
                Box(root, "Fence weather cap", steel, new Vector3(0, size.y * .5f + .016f, 0), new Vector3(.20f, .032f, .20f));
                Box(root, "Fence ground shoe", steel, new Vector3(0, -size.y * .5f + .01f, 0), new Vector3(.19f, .12f, .19f));
                float roadSide = -Mathf.Sign(renderer.transform.position.x);
                Box(root, "Fence rail fixing plate", steel, new Vector3(roadSide * .09f, .20f, .05f), new Vector3(.025f, .18f, .20f));
                foreach (float y in new[] { .16f, .24f })
                    Box(root, "Rail fixing bolt", dark, new Vector3(roadSide * .107f, y, .03f), new Vector3(.012f, .025f, .025f));
            }
        }

        static void Pallet(Transform root, Vector3 origin, Vector2 footprint)
        {
            foreach (float x in new[] { -footprint.x * .39f, 0, footprint.x * .39f })
                Box(root, "Pallet runner", timber, origin + new Vector3(x, .065f, 0), new Vector3(.12f, .13f, footprint.y));
            for (int board = 0; board < 6; board++)
                Box(root, "Pallet deck board", timber, origin + new Vector3(0, .145f, -footprint.y * .5f + footprint.y / 12 + board * footprint.y / 6), new Vector3(footprint.x, .03f, footprint.y / 6 - .025f));
        }
        static void Crate(Transform root, Vector3 origin, Vector3 size, string name)
        {
            Box(root, name, timber, origin + Vector3.up * (size.y * .5f), size);
            foreach (float x in new[] { -size.x * .40f, size.x * .40f })
                Box(root, name + " corner batten", timber, origin + new Vector3(x, size.y * .5f, -size.z * .5f - .02f), new Vector3(.055f, size.y, .04f));
            Box(root, name + " lid", timber, origin + Vector3.up * (size.y + .017f), new Vector3(size.x + .04f, .034f, size.z + .04f));
            Box(root, name + " latch", steel, origin + new Vector3(0, size.y * .68f, -size.z * .5f - .035f), new Vector3(.07f, .13f, .018f));
        }
        static void Barrel(Transform root, Vector3 origin)
        {
            Round(root, "Oil drum body", painted, origin, new Vector2(0, 0), new Vector2(.32f, 0), new Vector2(.34f, .035f), new Vector2(.34f, .86f), new Vector2(.32f, .9f), new Vector2(0, .9f));
            foreach (float y in new[] { .03f, .27f, .64f, .87f })
                Round(root, "Oil drum rolling hoop", steel, origin + Vector3.up * y, new Vector2(.338f, 0), new Vector2(.35f, 0), new Vector2(.35f, .024f), new Vector2(.338f, .024f), new Vector2(.338f, 0));
            Round(root, "Drum filler bung", dark, origin + new Vector3(.14f, .899f, 0), new Vector2(0, 0), new Vector2(.032f, 0), new Vector2(.032f, .014f), new Vector2(0, .014f));
            Collision(root, "Drum collision", origin + Vector3.up * .45f, new Vector3(.70f, .90f, .70f));
        }

        static void FinishBody(GameObject body, Material material, Vector3 size)
        {
            // Replace stretched unit-cube UVs with metre-scale, lightmap-ready geometry.
            // Bake the former scale into collider dimensions so the world collision is identical.
            var collider = body.GetComponent<BoxCollider>();
            collider.center = Vector3.Scale(collider.center, size);
            collider.size = Vector3.Scale(collider.size, size);
            body.transform.localScale = Vector3.one;
            body.GetComponent<MeshFilter>().sharedMesh = FarmhouseTimberFinish.IsTimber(material) ? FarmhouseTimberFinish.Box(size, .006f) : FarmhouseMeshLibrary.Box(size, .006f);
            body.GetComponent<MeshRenderer>().sharedMaterial = material; Static(body);
        }

        static Material Surface(string kind, string fallback, Color tint) => AssetDatabase.LoadAssetAtPath<Material>(FarmhouseMaterialLibrary.MaterialPath(kind)) ?? WeaponModelGeometry.MaterialFor(fallback, tint, kind == "Steel" ? .7f : kind == "Roofing" ? .45f : 0f);
        static Material Painted(string name, Color tint)
        {
            string path = "Assets/Harvest/Materials/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material; // Preserve edited/imported maps and finish.
            material = new Material(Surface("Roofing", "Yard sheet steel", new Color(.4f, .4f, .36f))) { name = name };
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", tint);
            AssetDatabase.CreateAsset(material, path); return material;
        }
        static Transform Group(Transform parent, string name, Vector3 point)
        { var go = new GameObject(name); go.transform.SetParent(parent, false); go.transform.localPosition = point; return go.transform; }
        static GameObject Box(Transform root, string name, Material material, Vector3 point, Vector3 size, float bevel = .006f)
        {
            var go = new GameObject(name); go.transform.SetParent(root, false); go.transform.localPosition = point;
            go.AddComponent<MeshFilter>().sharedMesh = FarmhouseTimberFinish.IsTimber(material) ? FarmhouseTimberFinish.Box(size, bevel) : FarmhouseMeshLibrary.Box(size, bevel);
            go.AddComponent<MeshRenderer>().sharedMaterial = material; Static(go); return go;
        }
        static void Beam(Transform root, string name, Material material, Vector3 from, Vector3 to, float width)
        { Box(root, name, material, (from + to) * .5f, new Vector3(width, width, Vector3.Distance(from, to))).transform.localRotation = Quaternion.LookRotation(to - from); }
        static void Pipe(Transform root, string name, Material material, Vector3 from, Vector3 to, float radius)
        {
            float length = Vector3.Distance(from, to);
            var go = Round(root, "Tube_" + radius.ToString("F4", CultureInfo.InvariantCulture) + "_" + length.ToString("F4", CultureInfo.InvariantCulture), material, from,
                new Vector2(0, 0), new Vector2(radius, 0), new Vector2(radius, length), new Vector2(0, length));
            go.name = name; go.transform.localRotation = Quaternion.FromToRotation(Vector3.up, to - from);
        }
        static GameObject Round(Transform root, string name, Material material, Vector3 point, params Vector2[] profile)
        {
            if (!RoundMeshes.TryGetValue(name, out Mesh mesh)) { mesh = FarmMachineryMeshes.Revolve("Yard_" + name.Replace(" ", "_"), profile); RoundMeshes.Add(name, mesh); }
            var go = new GameObject(name); go.transform.SetParent(root, false); go.transform.localPosition = point;
            go.AddComponent<MeshFilter>().sharedMesh = mesh; go.AddComponent<MeshRenderer>().sharedMaterial = material; Static(go); return go;
        }
        static void Collision(Transform root, string name, Vector3 centre, Vector3 size)
        {
            Transform proxy = Group(root, name, Vector3.zero);
            var collider = proxy.gameObject.AddComponent<BoxCollider>(); collider.center = centre; collider.size = size;
        }
        static void Static(GameObject go) => GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.ContributeGI | StaticEditorFlags.BatchingStatic);
        static void Label(Transform root, string words, Vector3 point, float yaw, float scale, Color? tint = null)
        {
            Transform go = Group(root, words, point); go.localRotation = Quaternion.Euler(0, yaw, 0);
            var text = go.gameObject.AddComponent<TextMesh>(); text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (text.font != null) go.GetComponent<MeshRenderer>().sharedMaterial = text.font.material;
            text.text = words; text.fontSize = 40; text.characterSize = scale; text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center; text.color = tint ?? new Color(.65f, .64f, .51f);
            go.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
    }
}
