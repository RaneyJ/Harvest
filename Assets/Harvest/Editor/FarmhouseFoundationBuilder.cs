using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Harvest.Editor
{
    // Generated assets are a modeling foundation; authored variants can live outside GeneratedPath.
    public static class FarmhouseFoundationBuilder
    {
        public const string LayoutPath = "Assets/Harvest/Data/Farmhouse Layout.asset";
        public const string GeneratedPath = "Assets/Harvest/Prefabs/Environment/Farmhouse Generated.prefab";
        const float FloorThickness = 0.16f;
        sealed class Assembly
        {
            public GameObject Root;
            public Transform Shell, Roof, Porch, Interior, Trim, Collision;
            public FarmhouseLayout Layout;
            public Material Plaster, Timber, TrimTimber, StairTimber, Steel, Roofing, Stone;
        }
        readonly struct Opening
        {
            public readonly string Name;
            public readonly float Center, Width, Sill, Top;
            public Opening(string name, float center, float width, float sill, float top)
            { Name = name; Center = center; Width = width; Sill = sill; Top = top; }
        }
        public static GameObject Build()
        {
            FarmhouseMeshLibrary.EnsureFolder("Assets/Harvest/Data");
            FarmhouseMeshLibrary.EnsureFolder("Assets/Harvest/Materials");
            FarmhouseMeshLibrary.EnsureFolder("Assets/Harvest/Prefabs/Environment");
            FarmhouseLayout layout = AssetDatabase.LoadAssetAtPath<FarmhouseLayout>(LayoutPath);
            if (layout == null)
            { layout = ScriptableObject.CreateInstance<FarmhouseLayout>(); AssetDatabase.CreateAsset(layout, LayoutPath); }
            ValidateLayout(layout);
            if (layout.AuthoredPrefab != null)
            {
                if (!AssetDatabase.Contains(layout.AuthoredPrefab) || AssetDatabase.GetAssetPath(layout.AuthoredPrefab) == GeneratedPath ||
                    PrefabUtility.GetPrefabAssetType(layout.AuthoredPrefab) == PrefabAssetType.NotAPrefab)
                    throw new InvalidOperationException("AuthoredPrefab must be an artist-owned prefab outside the generated path.");
                GameObject authored = (GameObject)PrefabUtility.InstantiatePrefab(layout.AuthoredPrefab);
                authored.name = "Enterable farmhouse"; authored.transform.position = layout.WorldOrigin;
                var foundation = authored.GetComponent<FarmhouseFoundation>();
                if (foundation == null) foundation = authored.AddComponent<FarmhouseFoundation>();
                foundation.Layout = layout;
                return authored;
            }
            FarmhouseMeshLibrary.BeginBuild();
            var a = new Assembly { Root = new GameObject("Enterable farmhouse"), Layout = layout };
            try
            {
                a.Root.AddComponent<FarmhouseFoundation>().Layout = layout;
                a.Shell = Group(a.Root, "Shell"); a.Roof = Group(a.Root, "Roof");
                a.Porch = Group(a.Root, "Porch"); a.Interior = Group(a.Root, "Interior");
                a.Trim = Group(a.Root, "Trim"); a.Collision = Group(a.Root, "Collision");
                // Stock Lit supports normal maps and baked lightmaps; no placeholder custom shader dependency.
                a.Plaster = Material("Plaster", new Color(0.72f, 0.69f, 0.60f), 0f, 0.12f);
                a.Timber = Material("Timber", new Color(0.30f, 0.24f, 0.16f), 0f, 0.18f);
                a.Steel = Material("Steel", new Color(0.18f, 0.20f, 0.20f), 0.7f, 0.30f);
                a.Roofing = Material("Roofing", new Color(0.33f, 0.35f, 0.34f), 0.55f, 0.25f);
                a.Stone = Material("Foundation", new Color(0.40f, 0.41f, 0.37f), 0f, 0.10f);
                FarmhouseMaterialLibrary.ApplyInstalled();
                FarmhousePlasterFinish.ApplyInstalled();
                a.TrimTimber=FarmhouseTimberFinish.Material("Trim",a.Timber,.22f);
                a.StairTimber=FarmhouseTimberFinish.Material("Stairs",a.Timber,.18f);
                Shell(a); FloorsAndStairs(a); Roof(a); Porch(a); Interior(a); Utilities(a);
                FarmhouseJoinery.Ceilings(a.Interior,a.Plaster,layout,FloorThickness);
                AssetDatabase.SaveAssets();
                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(a.Root, GeneratedPath);
                if (prefab == null) throw new InvalidOperationException("Farmhouse prefab could not be saved.");
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                instance.name = "Enterable farmhouse";
                instance.transform.position = layout.WorldOrigin;
                return instance;
            }
            finally { Object.DestroyImmediate(a.Root); }
        }
        public static void ValidateLayout(FarmhouseLayout l)
        {
            if (l != null)
                foreach (float value in new[] { l.WorldOrigin.x, l.WorldOrigin.y, l.WorldOrigin.z, l.HalfWidth, l.HalfDepth,
                    l.StoreyHeight, l.RoofPitch, l.WallThickness, l.RoofOverhang, l.PorchDepth, l.StairWidth, l.TreadDepth })
                    if (float.IsNaN(value) || float.IsInfinity(value))
                        throw new InvalidOperationException("Farmhouse dimensions and position must be finite.");
            if (l == null || l.HalfWidth < 5.5f || l.HalfDepth < 6.5f || l.StoreyHeight < 3f ||
                l.RoofPitch < 10f || l.RoofPitch > 30f || l.WallThickness < 0.2f || l.WallThickness > 0.4f ||
                l.RoofOverhang < 0.25f || l.RoofOverhang > 0.75f || l.PorchDepth < 2.4f ||
                l.StairWidth < 2f || l.TreadDepth < 0.35f || l.StairCount < 16 || l.StairCount > 20 ||
                l.Riser <= 0f || l.Riser > 0.25f || l.StairEnd > l.HalfDepth - 1.5f ||
                l.StairwellRight > -1f || l.StairwellRight - l.StairX < 1.4f)
                throw new InvalidOperationException("Farmhouse dimensions do not leave safe stairs, landing or circulation. Check Farmhouse Layout.asset.");
        }
        static void Shell(Assembly a)
        {
            FarmhouseLayout l = a.Layout;
            Wall(a, "Ground front", false, -l.HalfDepth, -1, 0f, (l.HalfWidth * 2f + l.WallThickness),
                new Opening("Kitchen window", -3.8f, 2f, 0.95f, 2.25f),
                new Opening("Front entrance", 0f, 2.2f, 0f, 2.6f),
                new Opening("Front side window", 3.8f, 2f, 0.95f, 2.25f));
            Wall(a, "Ground rear", false, l.HalfDepth, 1, 0f, (l.HalfWidth * 2f + l.WallThickness),
                new Opening("Rear service entrance", 0f, 1.4f, 0f, 2.5f));
            Wall(a, "Ground road", true, l.HalfWidth, 1, 0f, (l.HalfDepth * 2f - l.WallThickness),
                new Opening("Ground road window", 1f, 3.2f, 0.9f, 2.2f));
            Wall(a, "Ground field", true, -l.HalfWidth, -1, 0f, (l.HalfDepth * 2f - l.WallThickness),
                new Opening("Ground field window", 3f, 2.4f, 0.9f, 2.2f));
            Wall(a, "Upper front", false, -l.HalfDepth, -1, l.StoreyHeight, (l.HalfWidth * 2f + l.WallThickness),
                new Opening("Upstairs front firing window", 1f, 3.2f, 0.85f, 2.3f));
            Wall(a, "Upper rear", false, l.HalfDepth, 1, l.StoreyHeight, (l.HalfWidth * 2f + l.WallThickness),
                new Opening("Upstairs rear window", 1f, 3.2f, 0.85f, 2.3f));
            Wall(a, "Upper road", true, l.HalfWidth, 1, l.StoreyHeight, (l.HalfDepth * 2f - l.WallThickness),
                new Opening("Upstairs road firing window", 1f, 4f, 0.85f, 2.3f));
            Wall(a, "Upper field", true, -l.HalfWidth, -1, l.StoreyHeight, (l.HalfDepth * 2f - l.WallThickness),
                new Opening("Upstairs field window", 3f, 2.4f, 0.85f, 2.3f));
            foreach (int x in new[] { -1, 1 })
                foreach (int z in new[] { -1, 1 })
                    Part(a, a.Trim, "Corner steel " + x + " " + z, a.Steel,
                        new Vector3(x * (l.HalfWidth + l.WallThickness * 0.5f - 0.02f), l.StoreyHeight, z * (l.HalfDepth + l.WallThickness * 0.5f - 0.02f)),
                        new Vector3(0.16f, l.EavesHeight, 0.16f));
        }
        static void Wall(Assembly a, string name, bool alongZ, float fixedAxis, int outside,
            float floor, float length, params Opening[] openings)
        {
            var sections = new System.Collections.Generic.List<Bounds>();
            float cursor = -length * 0.5f;
            foreach (Opening o in openings)
            {
                float left = o.Center - o.Width * 0.5f, right = o.Center + o.Width * 0.5f;
                if (left < cursor || right > length * 0.5f) throw new InvalidOperationException("Overlapping wall openings: " + name);
                Panel(a, sections, name + " solid " + o.Name, alongZ, fixedAxis, cursor, left, floor, a.Layout.StoreyHeight);
                FarmhouseJoinery.Skirting(a.Trim,a.TrimTimber,a.Layout,alongZ,fixedAxis,outside,cursor,left,floor==0f?a.Layout.GroundFloorTop:floor,name+" "+o.Name);
                FarmhouseJoinery.Opening(a.Trim,a.TrimTimber,a.Layout,alongZ,fixedAxis,outside,o.Center,o.Width,o.Sill,o.Top,floor,o.Name);
                if (o.Sill > 0f) Panel(a, sections, o.Name + " sill", alongZ, fixedAxis, left, right, floor, o.Sill);
                Panel(a, sections, o.Name + " lintel", alongZ, fixedAxis, left, right, floor + o.Top, a.Layout.StoreyHeight - o.Top);
                float outer = fixedAxis + outside * (a.Layout.WallThickness * 0.5f + 0.045f);
                Frame(a, o.Name + " header", alongZ, outer, o.Center, floor + o.Top, o.Width + 0.18f, 0.10f);
                foreach (int side in new[] { -1, 1 })
                    Frame(a, o.Name + " jamb " + side, alongZ, outer, o.Center + side * o.Width * 0.5f,
                        floor + (o.Sill + o.Top) * 0.5f, 0.09f, o.Top - o.Sill);
                cursor = right;
            }
            Panel(a, sections, name + " end", alongZ, fixedAxis, cursor, length * 0.5f, floor, a.Layout.StoreyHeight);
            FarmhouseJoinery.Skirting(a.Trim,a.TrimTimber,a.Layout,alongZ,fixedAxis,outside,cursor,length*.5f,floor==0f?a.Layout.GroundFloorTop:floor,name+" end");
            foreach(Opening o in openings) if(o.Sill>0f)
                FarmhouseJoinery.Skirting(a.Trim,a.TrimTimber,a.Layout,alongZ,fixedAxis,outside,o.Center-o.Width*.5f,o.Center+o.Width*.5f,floor==0f?a.Layout.GroundFloorTop:floor,name+" below "+o.Name);
            // Flat, touching sections share one facade mesh. No inset seams or per-panel UV resets.
            Visual(a.Shell, name + " wall", FarmhouseMeshLibrary.Wall(name, sections), a.Plaster, Vector3.zero);
        }
        static void Panel(Assembly a, System.Collections.Generic.List<Bounds> sections, string name, bool alongZ, float fixedAxis, float start, float end, float bottom, float height)
        {
            if (end - start < 0.01f || height < 0.01f) return;
            Vector3 size = WallSize(alongZ, end - start, height, a.Layout.WallThickness);
            Solid(a, name, WallPosition(alongZ, fixedAxis, (start + end) * 0.5f, bottom + height * 0.5f), size);
            sections.Add(new Bounds(WallPosition(alongZ, fixedAxis, (start + end) * 0.5f, bottom + height * 0.5f), size));
        }
        static void Frame(Assembly a, string name, bool alongZ, float fixedAxis, float center, float y, float width, float height)
        { Part(a, a.Trim, name, a.TrimTimber, WallPosition(alongZ, fixedAxis, center, y), WallSize(alongZ, width, height, 0.11f), 0.006f); }
        static Vector3 WallPosition(bool alongZ, float fixedAxis, float center, float y) =>
            alongZ ? new Vector3(fixedAxis, y, center) : new Vector3(center, y, fixedAxis);
        static Vector3 WallSize(bool alongZ, float width, float height, float depth) =>
            alongZ ? new Vector3(depth, height, width) : new Vector3(width, height, depth);
        static void FloorsAndStairs(Assembly a)
        {
            FarmhouseLayout l = a.Layout;
            Part(a, a.Shell, "Concrete plinth", a.Stone, new Vector3(0f, -0.06f, 0f), new Vector3(l.HalfWidth * 2f + 0.2f, 0.18f, l.HalfDepth * 2f + 0.2f));
            Floor(a, "Ground floor", new Vector3(0f, l.GroundFloorTop - 0.06f, 0f), new Vector3(l.HalfWidth * 2f, 0.12f, l.HalfDepth * 2f));
            float inner = l.StairwellRight;
            Floor(a, "Upper main floor", new Vector3((inner + l.HalfWidth) * 0.5f, l.UpperFloorTop - FloorThickness * 0.5f, 0f),
                new Vector3(l.HalfWidth - inner, FloorThickness, l.HalfDepth * 2f));
            float strip = inner + l.HalfWidth, centerX = -l.HalfWidth + strip * 0.5f;
            Floor(a, "Upper front floor", new Vector3(centerX, l.UpperFloorTop - FloorThickness * 0.5f, (-l.HalfDepth + l.StairStart) * 0.5f),
                new Vector3(strip, FloorThickness, l.StairStart + l.HalfDepth));
            Floor(a, "Upper rear landing", new Vector3(centerX, l.UpperFloorTop - FloorThickness * 0.5f, (l.StairEnd + l.HalfDepth) * 0.5f),
                new Vector3(strip, FloorThickness, l.HalfDepth - l.StairEnd));
            for (int i = 0; i < l.StairCount; i++)
            {
                Vector3 top = l.StairPoint(i);
                Solid(a, "Stair support " + i, new Vector3(top.x, (l.GroundFloorTop + top.y) * 0.5f, top.z),
                    new Vector3(l.StairWidth, top.y - l.GroundFloorTop, l.TreadDepth));
                Part(a, a.Interior, "Stair tread " + i, a.StairTimber, top - Vector3.up * 0.035f,
                    new Vector3(l.StairWidth, 0.07f, l.TreadDepth), 0.006f);
                Part(a, a.Interior, "Stair riser " + i, a.StairTimber,
                    new Vector3(top.x, top.y - l.Riser * 0.5f, top.z - l.TreadDepth * 0.5f + 0.018f),
                    new Vector3(l.StairWidth, l.Riser, 0.036f), 0.004f);
            }
            float railX = l.StairwellRight + 0.06f, railLength = l.StairEnd - l.StairStart;
            Part(a, a.Interior, "Stairwell top rail", a.Steel,
                new Vector3(railX, l.UpperFloorTop + 1.02f, (l.StairStart + l.StairEnd) * 0.5f),
                new Vector3(0.08f, 0.08f, railLength), 0.012f, true);
            int posts = Mathf.CeilToInt(railLength / 0.75f);
            for (int i = 0; i <= posts; i++)
                Part(a, a.Interior, "Stairwell post " + i, a.Steel,
                    new Vector3(railX, l.UpperFloorTop + 0.5f, Mathf.Lerp(l.StairStart, l.StairEnd, i / (float)posts)),
                    new Vector3(0.075f, 1f, 0.075f), 0.008f, true);
            Beam(a, a.Interior, "Stair wall handrail", a.Steel,
                new Vector3(l.StairX - l.StairWidth * 0.5f - 0.18f, l.TreadTop(0) + 0.85f, l.StairStart),
                new Vector3(l.StairX - l.StairWidth * 0.5f - 0.18f, l.UpperFloorTop + 0.85f, l.StairEnd), 0.065f);
            Part(a, a.Interior, "Upper ceiling", a.Timber, new Vector3(0f, l.EavesHeight - 0.06f, 0f),
                new Vector3(l.HalfWidth * 2f, 0.12f, l.HalfDepth * 2f), 0.008f, true);
            for (int i = -1; i <= 1; i++)
                Part(a, a.Interior, "Floor structural beam " + i, a.Timber,
                    new Vector3((inner + l.HalfWidth) * 0.5f, l.UpperFloorTop - 0.23f, i * 4.5f),
                    new Vector3(l.HalfWidth - inner, 0.14f, 0.18f));
        }
        static void Floor(Assembly a, string name, Vector3 center, Vector3 size)
        {
            Solid(a, name, center, size, true);
            FarmhouseFloorboards.Build(a.Interior,name,center,size,a.Timber);
        }
        static void Roof(Assembly a)
        {
            FarmhouseLayout l = a.Layout;
            float pitch = l.RoofPitch * Mathf.Deg2Rad, span = l.HalfWidth + l.RoofOverhang;
            float slopeLength = span / Mathf.Cos(pitch), depth = (l.HalfDepth + l.RoofOverhang) * 2f;
            float centerY = l.RidgeHeight - span * 0.5f * Mathf.Tan(pitch) + 0.1f / Mathf.Cos(pitch);
            foreach (int side in new[] { -1, 1 })
            {
                GameObject roof = Part(a, a.Roof, "Roof slope " + side, a.Roofing,
                    new Vector3(side * span * 0.5f, centerY, 0f), new Vector3(slopeLength, 0.2f, depth), 0.008f, true, true);
                roof.transform.localRotation = Quaternion.Euler(0f, 0f, -side * l.RoofPitch);
                Transform collision = a.Collision.Find("Roof slope " + side);
                collision.localRotation = roof.transform.localRotation;
                int seams = Mathf.CeilToInt(depth / 0.65f);
                for (int i = 0; i <= seams; i++)
                    Part(a, roof.transform, "Standing seam " + i, a.Steel,
                        new Vector3(0f, 0.11f, -depth * 0.5f + i * depth / seams), new Vector3(slopeLength, 0.035f, 0.035f), 0.006f);
            }
            FarmhouseJoinery.RoofEdges(a.Trim,a.Timber,a.Steel,l);
            Mesh gable = FarmhouseMeshLibrary.Gable(l.HalfWidth * 2f, l.RidgeHeight - l.EavesHeight, l.WallThickness, l.EavesHeight);
            foreach (int side in new[] { -1, 1 })
            {
                Vector3 position = new Vector3(0f, l.EavesHeight, side * l.HalfDepth);
                Visual(a.Roof, "Gable " + side, gable, a.Plaster, position);
                GameObject block = new GameObject("Gable " + side);
                block.transform.SetParent(a.Collision, false); block.transform.localPosition = position;
                block.AddComponent<MeshCollider>().sharedMesh = gable;
            }
        }
        static void Porch(Assembly a)
        {
            FarmhouseLayout l = a.Layout;
            float width = l.HalfWidth * 2f - 1.2f, centerZ = -l.HalfDepth - l.PorchDepth * 0.5f;
            Part(a, a.Porch, "Porch slab", a.Stone, new Vector3(0f, l.GroundFloorTop - 0.06f, centerZ),
                new Vector3(width, 0.12f, l.PorchDepth), 0.012f, true);
            Part(a, a.Porch, "Rear service step", a.Stone, new Vector3(0f, l.GroundFloorTop - 0.06f, l.HalfDepth + 0.55f),
                new Vector3(2.2f, 0.12f, 1.4f), 0.012f, true);
            foreach (float x in new[] { -width * 0.5f + 0.25f, -1.85f, 1.85f, width * 0.5f - 0.25f })
            {
                float z = -l.HalfDepth - l.PorchDepth + 0.2f;
                Part(a, a.Porch, "Porch post " + x, a.Timber, new Vector3(x, 1.46f, z), new Vector3(0.2f, 2.68f, 0.2f), 0.012f, true);
                Part(a, a.Porch, "Post foot bracket " + x, a.Steel, new Vector3(x, 0.25f, z), new Vector3(0.24f, 0.25f, 0.24f));
                Beam(a, a.Porch, "Porch brace " + x, a.Timber, new Vector3(x, 2.2f, z), new Vector3(x + (x < 0f ? 0.5f : -0.5f), 2.78f, z), 0.13f);
            }
            GameObject canopy = Part(a, a.Porch, "Porch canopy", a.Roofing, new Vector3(0f, 3f, centerZ),
                new Vector3(width + 0.6f, 0.14f, l.PorchDepth + 0.5f), 0.006f, true);
            canopy.transform.localRotation = Quaternion.Euler(-5f, 0f, 0f);
            a.Collision.Find("Porch canopy").localRotation = canopy.transform.localRotation;
            Part(a, a.Porch, "Porch front beam", a.Timber, new Vector3(0f, 2.78f, -l.HalfDepth - l.PorchDepth + 0.2f),
                new Vector3(width, 0.18f, 0.22f));
        }
        static void Interior(Assembly a)
        {
            FarmhouseLayout l = a.Layout;
            FarmhouseInteriorDressing.Build(a.Interior,a.Collision,l,a.Timber,a.Steel,a.Stone);
            // Cover beside the opening, with a clear central firing lane and approach route.
            foreach (int side in new[] { -1, 1 })
                Part(a, a.Interior, "Upstairs agricultural crate " + side, a.Timber,
                    new Vector3(l.HalfWidth - 0.8f, l.UpperFloorTop + 0.45f, 1f + side * 3f),
                    new Vector3(1.1f, 0.9f, 1.1f), 0.025f, true);
        }
        static void Utilities(Assembly a)
        {
            FarmhouseLayout l = a.Layout;
            Part(a, a.Trim, "Colony junction box", a.Steel, new Vector3(l.HalfWidth + 0.18f, 1.8f, -l.HalfDepth + 0.7f), new Vector3(0.22f, 0.55f, 0.4f));
            Part(a, a.Trim, "Exterior conduit", a.Steel, new Vector3(l.HalfWidth + 0.15f, l.StoreyHeight, -l.HalfDepth + 0.7f), new Vector3(0.055f, l.EavesHeight, 0.055f), 0.01f);
            Part(a, a.Roof, "Chimney", a.Stone, new Vector3(-3f, l.RidgeHeight - 0.1f, 4f), new Vector3(0.65f, 2.2f, 0.65f), 0.025f);
            Part(a, a.Roof, "Chimney cap", a.Steel, new Vector3(-3f, l.RidgeHeight + 1.04f, 4f), new Vector3(0.85f, 0.10f, 0.85f));
            Part(a, a.Roof, "Comms mast", a.Steel, new Vector3(0.65f, l.RidgeHeight + 0.8f, -l.HalfDepth + 0.8f), new Vector3(0.08f, 2.5f, 0.08f), 0.01f);
            Part(a, a.Roof, "Comms antenna", a.Steel, new Vector3(0.65f, l.RidgeHeight + 1.25f, -l.HalfDepth + 0.8f), new Vector3(0.28f, 0.6f, 0.12f));
        }
        static Transform Group(GameObject root, string name)
        { GameObject group = new GameObject(name); group.transform.SetParent(root.transform, false); return group.transform; }
        static Material Material(string name, Color color, float metal, float smoothness)
        {
            string path = "Assets/Harvest/Materials/Farmhouse Foundation " + name + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material; // Never reset artist edits.
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) throw new InvalidOperationException("URP Lit shader is unavailable.");
            material = new Material(shader); material.SetColor("_BaseColor", color);
            material.SetFloat("_Metallic", metal); material.SetFloat("_Smoothness", smoothness);
            AssetDatabase.CreateAsset(material, path); return material;
        }
        static GameObject Part(Assembly a, Transform group, string name, Material material, Vector3 position,
            Vector3 size, float bevel = 0.015f, bool solid = false, bool rotateUV = false)
        {
            GameObject visual = Visual(group, name, FarmhouseTimberFinish.IsTimber(material) ? FarmhouseTimberFinish.Box(size, bevel) : FarmhouseMeshLibrary.Box(size, bevel, rotateUV), material, position);
            if (solid) Solid(a, name, position, size);
            return visual;
        }
        static GameObject Visual(Transform group, string name, Mesh mesh, Material material, Vector3 position)
        {
            GameObject visual = new GameObject(name);
            visual.transform.SetParent(group, false); visual.transform.localPosition = position;
            visual.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = visual.AddComponent<MeshRenderer>(); renderer.sharedMaterial = material;
            GameObjectUtility.SetStaticEditorFlags(visual, StaticEditorFlags.ContributeGI|StaticEditorFlags.BatchingStatic);
            return visual;
        }
        static void Solid(Assembly a, string name, Vector3 center, Vector3 size, bool wood = false)
        {
            GameObject solid = new GameObject(name); solid.transform.SetParent(a.Collision, false); solid.transform.localPosition = center;
            solid.AddComponent<BoxCollider>().size = size;
            if (wood || name.StartsWith("Stair support ")) solid.AddComponent<FootstepSurface>().Kind = FootstepSurfaceKind.Wood;
        }
        static void Beam(Assembly a, Transform group, string name, Material material, Vector3 from, Vector3 to, float thickness)
        {
            GameObject beam = Part(a, group, name, material, (from + to) * 0.5f, new Vector3(thickness, thickness, (to - from).magnitude), 0.008f);
            beam.transform.localRotation = Quaternion.LookRotation(to - from);
        }
    }
}
