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
            FarmGroundTransitions.Build(soil);
            FarmGrainGeometry.Build();
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
