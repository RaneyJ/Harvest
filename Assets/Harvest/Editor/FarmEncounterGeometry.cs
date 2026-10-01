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
            FarmhouseFoundationBuilder.Build();
            GrainFields(grain);
            for (int side = -1; side <= 1; side += 2)
                for (int z = -39; z <= 57; z += 8)
                {
                    if (side == -1 && z > -18 && z < 11) continue;
                    Box("Fence post", timber, new Vector3(side * 30f, 0.75f, z), new Vector3(0.16f, 1.5f, 0.16f));
                    Box("Field fence rail", timber, new Vector3(side * 30f, 0.95f, z + 3.8f), new Vector3(0.1f, 0.12f, 7.6f));
                }
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
