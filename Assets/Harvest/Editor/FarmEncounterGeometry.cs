using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Harvest.Editor
{
    // Layout generation is separate from encounter waves, actors, and combat configuration.
    public static class FarmEncounterGeometry
    {
        public static void Build(Material soil, Material road, Material grain, Material concrete, Material timber)
        {
            Terrain(soil);
            Box("Freight road", road, new Vector3(0f, 0f, 10f), new Vector3(15f, 0.08f, 130f));
            for (int side = -1; side <= 1; side += 2)
                Box("Road gravel shoulder", soil, new Vector3(side * 8.4f, 0.01f, 10f), new Vector3(1.8f, 0.06f, 130f));
            for (int z = -49; z <= 69; z += 6)
                Box("Faded road center marking", concrete, new Vector3(0f, 0.047f, z), new Vector3(0.12f, 0.012f, 2.8f), false);
            Box("Farm access track", soil, new Vector3(-12f, 0.03f, -12f), new Vector3(10f, 0.06f, 4f));
            Farmhouse(timber, concrete);
            GrainFields(grain);
            for (int side = -1; side <= 1; side += 2)
                for (int z = -39; z <= 57; z += 8)
                {
                    if (side == -1 && z > -18 && z < 11) continue;
                    Box("Fence post", timber, new Vector3(side * 30f, 0.75f, z), new Vector3(0.16f, 1.5f, 0.16f));
                    Box("Field fence rail", timber, new Vector3(side * 30f, 0.95f, z + 3.8f), new Vector3(0.1f, 0.12f, 7.6f));
                }
        }
        static void Farmhouse(Material timber, Material stone)
        {
            Material plaster = Surface("Farmhouse Plaster", new Color(0.65f, 0.58f, 0.43f));
            Material roofing = Surface("Farmhouse Roof", new Color(0.19f, 0.23f, 0.21f));
            GameObject root = new GameObject("Enterable farmhouse");
            root.transform.position = new Vector3(-19f, 0f, -2f);
            // Local footprint x=-6..6, z=-7..7. Storey floors are 3.2m apart.
            Part(root, "Foundation floor", stone, new Vector3(0f, 0.03f, 0f), new Vector3(12f, 0.12f, 14f));
            Wall(root, "Ground front entrance", plaster, false, -7f, 0f, 3.2f, 12f, 0f, 2.2f, 0f, 2.6f);
            Wall(root, "Ground rear window", plaster, false, 7f, 0f, 3.2f, 12f, 0f, 2.4f, 0.9f, 2.2f);
            Wall(root, "Ground road window", plaster, true, 6f, 0f, 3.2f, 14f, 1f, 3.2f, 0.9f, 2.2f);
            Wall(root, "Ground field window", plaster, true, -6f, 0f, 3.2f, 14f, 3f, 2.4f, 0.9f, 2.2f);
            Wall(root, "Upstairs front window", plaster, false, -7f, 3.2f, 3.2f, 12f, 1f, 3.2f, 0.85f, 2.3f);
            Wall(root, "Upstairs rear window", plaster, false, 7f, 3.2f, 3.2f, 12f, 1f, 3.2f, 0.85f, 2.3f);
            Wall(root, "Upstairs road firing window", plaster, true, 6f, 3.2f, 3.2f, 14f, 1f, 4f, 0.85f, 2.3f);
            Wall(root, "Upstairs field window", plaster, true, -6f, 3.2f, 3.2f, 14f, 3f, 2.4f, 0.85f, 2.3f);
            // Leave a genuine stairwell opening. No ceiling collider crosses the flight.
            Part(root, "Upper main floor", timber, new Vector3(1.7f, 3.12f, 0f), new Vector3(8.6f, 0.16f, 14f));
            Part(root, "Upper front floor", timber, new Vector3(-4.3f, 3.12f, -5.95f), new Vector3(3.4f, 0.16f, 2.1f));
            Part(root, "Upper rear landing", timber, new Vector3(-4.3f, 3.12f, 4.25f), new Vector3(3.4f, 0.16f, 5.5f));
            for (int step = 0; step < 16; step++)
            {
                float top = (step + 1) * 0.2f;
                Part(root, "Stair tread " + (step + 1), timber, new Vector3(-4.3f, top * 0.5f, -4.7f + step * 0.4f),
                    new Vector3(2.2f, top, 0.4f));
            }
            // Guard the open upper floor edge without closing the stair exit.
            Part(root, "Stairwell guard", timber, new Vector3(-2.65f, 3.7f, -1.8f), new Vector3(0.1f, 1f, 6.2f));
            Part(root, "Entrance porch", stone, new Vector3(0f, 0.03f, -8.2f), new Vector3(5f, 0.12f, 2.4f));
            for (int side = -1; side <= 1; side += 2)
            {
                Part(root, "Porch post", timber, new Vector3(side * 2.25f, 1.5f, -9f), new Vector3(0.2f, 3f, 0.2f));
                GameObject roof = Part(root, "Pitched roof", roofing, new Vector3(side * 3.1f, 7.2f, 0f), new Vector3(6.7f, 0.2f, 15f));
                roof.transform.localRotation = Quaternion.Euler(0f, 0f, side * -14f);
            }
            for (int side = -1; side <= 1; side += 2)
            {
                Vector3[] vertices = { new Vector3(-6f, 6.4f, side * 7f), new Vector3(6f, 6.4f, side * 7f), new Vector3(0f, 8f, side * 7f) };
                Mesh gable = SaveMesh("Farmhouse Gable " + side,
                    new[] { vertices[0], vertices[1], vertices[2], vertices[2], vertices[1], vertices[0] },
                    new[] { 0, 1, 2, 3, 4, 5 },
                    new[] { Vector2.zero, Vector2.right, Vector2.up, Vector2.up, Vector2.right, Vector2.zero });
                GameObject panel = MeshObject("Roof gable", gable, plaster);
                panel.transform.SetParent(root.transform, false);
                panel.AddComponent<MeshCollider>().sharedMesh = gable;
            }
            Part(root, "Porch roof", roofing, new Vector3(0f, 3f, -8.2f), new Vector3(5.4f, 0.2f, 2.8f));
            Part(root, "Kitchen counter", stone, new Vector3(3.5f, 0.65f, 5f), new Vector3(3f, 1.1f, 0.8f));
            Part(root, "Farm table", timber, new Vector3(1f, 0.75f, 2f), new Vector3(2.2f, 0.15f, 1.2f));
            for (int side = -1; side <= 1; side += 2)
                Part(root, "Table support", timber, new Vector3(1f + side * 0.8f, 0.4f, 2f), new Vector3(0.15f, 0.7f, 0.8f));
        }
        static void Wall(GameObject root, string name, Material material, bool alongZ, float fixedAxis,
            float floor, float height, float width, float openingCenter, float openingWidth, float sill, float lintel)
        {
            float left = openingCenter - openingWidth * 0.5f;
            float right = openingCenter + openingWidth * 0.5f;
            Panel(root, name + " left", material, alongZ, fixedAxis, (-width * 0.5f + left) * 0.5f,
                floor + height * 0.5f, left + width * 0.5f, height);
            Panel(root, name + " right", material, alongZ, fixedAxis, (right + width * 0.5f) * 0.5f,
                floor + height * 0.5f, width * 0.5f - right, height);
            if (sill > 0f) Panel(root, name + " sill", material, alongZ, fixedAxis, openingCenter,
                floor + sill * 0.5f, openingWidth, sill);
            Panel(root, name + " lintel", material, alongZ, fixedAxis, openingCenter,
                floor + (height + lintel) * 0.5f, openingWidth, height - lintel);
        }
        static Material Surface(string name, Color color)
        {
            string path = "Assets/Harvest/Materials/" + name + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            material = new Material(Shader.Find("Standard"));
            material.color = color;
            AssetDatabase.CreateAsset(material, path);
            return material;
        }
        static void Panel(GameObject root, string name, Material material, bool alongZ, float fixedAxis,
            float center, float y, float width, float height)
        {
            Part(root, name, material, alongZ ? new Vector3(fixedAxis, y, center) : new Vector3(center, y, fixedAxis),
                alongZ ? new Vector3(0.22f, height, width) : new Vector3(width, height, 0.22f));
        }
        static void Terrain(Material material)
        {
            const int columns = 66, rows = 76;
            var vertices = new Vector3[columns * rows];
            var uv = new Vector2[vertices.Length];
            var triangles = new int[(columns - 1) * (rows - 1) * 6];
            for (int z = 0; z < rows; z++)
                for (int x = 0; x < columns; x++)
                {
                    float worldX = -65f + x * 2f, worldZ = -65f + z * 2f;
                    float edge = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(32f, 60f, Mathf.Abs(worldX)));
                    float height = edge * (0.5f + Mathf.PerlinNoise(x * 0.12f, z * 0.1f) * 4f) - 0.05f;
                    vertices[z * columns + x] = new Vector3(worldX, height, worldZ);
                    uv[z * columns + x] = new Vector2(worldX * 0.08f, worldZ * 0.08f);
                }
            int index = 0;
            for (int z = 0; z < rows - 1; z++)
                for (int x = 0; x < columns - 1; x++)
                {
                    int a = z * columns + x;
                    triangles[index++] = a; triangles[index++] = a + columns; triangles[index++] = a + 1;
                    triangles[index++] = a + 1; triangles[index++] = a + columns; triangles[index++] = a + columns + 1;
                }
            Mesh mesh = SaveMesh("Farm Terrain", vertices, triangles, uv);
            GameObject root = MeshObject("Farm terrain", mesh, material);
            root.AddComponent<MeshCollider>().sharedMesh = mesh;
        }
        static void GrainFields(Material material)
        {
            GameObject template = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Mesh cube = template.GetComponent<MeshFilter>().sharedMesh;
            var combine = new List<CombineInstance>();
            for (int side = -1; side <= 1; side += 2)
                for (int row = 0; row < 26; row++)
                    for (int column = 0; column < 9; column++)
                    {
                        float x = side * (10.5f + column * 2f), z = -44f + row * 4.5f;
                        if (side < 0 && z > -16f && z < 10f) continue; // House and its yard.
                        if (side < 0 && x > -17f && z > 30f && z < 46f) continue; // Burned freight car.
                        if (side < 0 && x < -18f && z > 14f && z < 24f) continue; // Equipment shed.
                        if (side > 0 && x > 20f && z > 43f && z < 53f) continue; // Grain silo.
                        if (side > 0 && x > 20f && x < 26f && z > 20f && z < 34f) continue;
                        for (int stalk = 0; stalk < 4; stalk++)
                        {
                            Vector3 position = new Vector3(x + (stalk % 2) * 0.35f, 0.65f, z + (stalk / 2) * 0.5f);
                            combine.Add(new CombineInstance { mesh = cube, transform = Matrix4x4.TRS(position,
                                Quaternion.Euler(0f, row * 17f, side * 5f), new Vector3(0.055f, 1.35f, 0.055f)) });
                            combine.Add(new CombineInstance { mesh = cube, transform = Matrix4x4.TRS(position + Vector3.up * 0.65f,
                                Quaternion.identity, new Vector3(0.14f, 0.25f, 0.1f)) });
                        }
                    }
            Object.DestroyImmediate(template);
            const string path = "Assets/Harvest/Data/Grain Fields.asset";
            Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mesh == null) { mesh = new Mesh(); AssetDatabase.CreateAsset(mesh, path); }
            mesh.Clear(); mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.CombineMeshes(combine.ToArray(), true, true);
            mesh.RecalculateBounds(); EditorUtility.SetDirty(mesh);
            MeshObject("Grain fields — visual concealment", mesh, material); // No bullet or movement collider.
        }
        static Mesh SaveMesh(string name, Vector3[] vertices, int[] triangles, Vector2[] uv)
        {
            string path = "Assets/Harvest/Data/" + name + ".asset";
            Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mesh == null) { mesh = new Mesh(); AssetDatabase.CreateAsset(mesh, path); }
            mesh.Clear(); mesh.vertices = vertices; mesh.triangles = triangles; mesh.uv = uv;
            mesh.RecalculateNormals(); mesh.RecalculateBounds(); EditorUtility.SetDirty(mesh); return mesh;
        }
        static GameObject MeshObject(string name, Mesh mesh, Material material)
        {
            GameObject root = new GameObject(name);
            root.AddComponent<MeshFilter>().sharedMesh = mesh;
            root.AddComponent<MeshRenderer>().sharedMaterial = material;
            return root;
        }
        static GameObject Part(GameObject root, string name, Material material, Vector3 position, Vector3 size)
        {
            GameObject part = Box(name, material, Vector3.zero, size);
            part.transform.SetParent(root.transform, false); part.transform.localPosition = position;
            if (name.StartsWith("Upper ") || name.StartsWith("Stair tread "))
                part.AddComponent<FootstepSurface>().Kind = FootstepSurfaceKind.Wood;
            return part;
        }
        static GameObject Box(string name, Material material, Vector3 position, Vector3 size, bool collider = true)
        {
            GameObject root = GameObject.CreatePrimitive(PrimitiveType.Cube);
            root.name = name; root.transform.position = position; root.transform.localScale = size;
            root.GetComponent<Renderer>().sharedMaterial = material;
            if (!collider) Object.DestroyImmediate(root.GetComponent<Collider>());
            return root;
        }
    }
}
