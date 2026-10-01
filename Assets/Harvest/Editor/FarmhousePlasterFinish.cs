using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Harvest.Editor
{
    // Editor-baked variation keeps the stock URP Lit shader and one sample per PBR map at runtime.
    // Offsets/weights are shared across color, tangent normals and the packed data mask.
    public static class FarmhousePlasterFinish
    {
        public const int Resolution = 4096;
        public const float TileMeters = 4f;
        const string Key = "PlasterFinish";
        static readonly string[] Files = { "Albedo.png", "NormalGL.png", "URP_Mask.png" };
        public static string TexturePath(string file) => FarmhouseMaterialLibrary.TexturePath(Key, file);

        [MenuItem("Harvest/Refine Farmhouse Plaster")]
        public static void Build()
        {
            if (Application.isPlaying || EditorApplication.isCompiling || FarmhouseMaterialLibrary.IsInstalling)
            { Debug.Log("Refine plaster in Edit mode after the PBR installation completes."); return; }
            string staging = Path.Combine(FarmhouseMaterialLibrary.CacheFolder, Key);
            try
            {
                var source = Array.Find(FarmhouseMaterialLibrary.ReadManifest().Materials, item => item.Key == "Plaster");
                if (source == null || !Mathf.Approximately(source.WidthMeters, 1f) || !Mathf.Approximately(source.HeightMeters, 1f))
                    throw new InvalidOperationException("The plaster finish requires the approved 1m source scan.");
                foreach (var file in source.Files) FarmhouseMaterialLibrary.VerifyFile(FarmhouseMaterialLibrary.CachePath("Plaster", file.FileName), file);
                bool complete = Array.TrueForAll(Files, file => File.Exists(TexturePath(file)));
                bool partial = Array.Exists(Files, file => File.Exists(TexturePath(file)));
                if (partial && !complete) throw new InvalidOperationException("Incomplete finish assets. Move the PlasterFinish folder aside, then rerun refinement.");
                if (!complete)
                {
                    Directory.CreateDirectory(staging);
                    for (int mode = 0; mode < Files.Length; mode++) Bake(mode, Path.Combine(staging, Files[mode]));
                    // No final assets are written until all three maps are finished. Existing finishes are preserved.
                    FarmhouseMeshLibrary.EnsureFolder(FarmhouseMaterialLibrary.SurfaceFolder + "/" + Key);
                    foreach (string file in Files) File.Copy(Path.Combine(staging, file), TexturePath(file));
                }
                for (int mode = 0; mode < Files.Length; mode++)
                    FarmhouseMaterialLibrary.ConfigureTexture(TexturePath(Files[mode]), mode == 0, mode == 1, Resolution);
                bool applied = ApplyInstalled();
                Debug.Log("Plaster finish ready: a 4m/4K reduced-repeat PBR tile baked from the approved scan. " +
                    (applied ? "Shared plaster material updated." : "Existing custom maps or an already refined material were preserved.") +
                    " Rebuild The Line (version 25), then inspect the walls and material review scene.");
            }
            catch (OperationCanceledException) { Debug.Log("Plaster refinement cancelled. Existing materials are unchanged; rerun to bake again."); }
            catch (Exception error) { Debug.LogError("Plaster refinement failed: " + error.Message); }
            finally
            {
                EditorUtility.ClearProgressBar();
                foreach (string file in Files) { string path = Path.Combine(staging, file); if (File.Exists(path)) File.Delete(path); }
            }
        }
        public static bool ApplyInstalled()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(FarmhouseMaterialLibrary.MaterialPath("Plaster"));
            if (material == null || material.shader.name != "Universal Render Pipeline/Lit") return false;
            // Upgrade only this installer's original map combination, never arbitrary authored maps.
            if (!Owns(material, "_BaseMap", "Albedo.jpg") || !Owns(material, "_BumpMap", "NormalGL.png") ||
                !Owns(material, "_MetallicGlossMap", "URP_Mask.png") || !Owns(material, "_OcclusionMap", "URP_Mask.png")) return false;
            var maps = new Texture2D[3];
            for (int i = 0; i < maps.Length; i++)
            {
                maps[i] = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath(Files[i]));
                if (maps[i] == null || maps[i].width != Resolution || maps[i].height != Resolution) return false;
            }
            material.SetTexture("_BaseMap", maps[0]); material.SetTexture("_BumpMap", maps[1]);
            material.SetTexture("_MetallicGlossMap", maps[2]); material.SetTexture("_OcclusionMap", maps[2]);
            // Preserve the user's relative density/phase while changing from a 1m source to a 4m atlas.
            material.SetTextureScale("_BaseMap", material.GetTextureScale("_BaseMap") / TileMeters);
            material.SetTextureOffset("_BaseMap", material.GetTextureOffset("_BaseMap") / TileMeters);
            EditorUtility.SetDirty(material); AssetDatabase.SaveAssets(); SceneView.RepaintAll(); return true;
        }
        static bool Owns(Material material, string property, string file) =>
            AssetDatabase.GetAssetPath(material.GetTexture(property)) == FarmhouseMaterialLibrary.TexturePath("Plaster", file);

        static void Bake(int mode, string path)
        {
            Texture2D source = null, output = null;
            try
            {
                string input = mode == 0 ? "Albedo.jpg" : mode == 1 ? "NormalGL.png" : "ARM.png";
                source = FarmhouseMaterialLibrary.ReadLinearImage(FarmhouseMaterialLibrary.CachePath("Plaster", input));
                if (source.width != 2048 || source.height != 2048) throw new InvalidOperationException("Expected a square 2K plaster source.");
                int size = source.width; Color32[] original = source.GetPixels32();
                Object.DestroyImmediate(source); source = null;
                if (mode == 2) for (int i = 0; i < original.Length; i++) original[i] = FarmhouseMaterialLibrary.PackARM(original[i]);
                var pixels = new Color32[Resolution * Resolution];
                var phases = new Vector2[16];
                for (int j = 0; j < 4; j++) for (int i = 0; i < 4; i++) phases[i + j * 4] = Offset(i, j);
                var coordinates = new float[Resolution]; var cells = new int[Resolution]; var weights = new float[Resolution];
                for (int i = 0; i < Resolution; i++)
                {
                    coordinates[i] = (i + 0.5f) / Resolution * TileMeters;
                    cells[i] = Mathf.FloorToInt(coordinates[i]); weights[i] = Mathf.SmoothStep(0f, 1f, coordinates[i] - cells[i]);
                }
                var linear = new float[256]; var gamma = new byte[4097];
                for (int i = 0; i < linear.Length; i++) linear[i] = Mathf.GammaToLinearSpace(i / 255f);
                for (int i = 0; i < gamma.Length; i++) gamma[i] = (byte)Mathf.RoundToInt(Mathf.LinearToGammaSpace(i / 4096f) * 255f);
                for (int y = 0; y < Resolution; y++)
                {
                    if (y % 32 == 0 && EditorUtility.DisplayCancelableProgressBar("Refining approved plaster", Files[mode] + " — baking variation", (mode + y / (float)Resolution) / 3f))
                        throw new OperationCanceledException();
                    float v = (y + 0.5f) / Resolution * TileMeters; int cy = Mathf.FloorToInt(v);
                    float fy = Mathf.SmoothStep(0f, 1f, v - cy);
                    for (int x = 0; x < Resolution; x++)
                    {
                        float u = coordinates[x]; int cx = cells[x]; float fx = weights[x];
                        Vector4 a = Sample(original, size, u, v, phases[(cx & 3) + (cy & 3) * 4], mode, linear);
                        Vector4 b = Sample(original, size, u, v, phases[((cx + 1) & 3) + (cy & 3) * 4], mode, linear);
                        Vector4 c = Sample(original, size, u, v, phases[(cx & 3) + ((cy + 1) & 3) * 4], mode, linear);
                        Vector4 d = Sample(original, size, u, v, phases[((cx + 1) & 3) + ((cy + 1) & 3) * 4], mode, linear);
                        Vector4 value = Vector4.LerpUnclamped(Vector4.LerpUnclamped(a, b, fx), Vector4.LerpUnclamped(c, d, fx), fy);
                        if (mode == 0)
                            pixels[y * Resolution + x] = new Color32(Encode(value.x, gamma), Encode(value.y, gamma), Encode(value.z, gamma), 255);
                        else if (mode == 1)
                        {
                            var normal = new Vector3(value.x * 2f - 1f, value.y * 2f - 1f, value.z * 2f - 1f).normalized;
                            pixels[y * Resolution + x] = (Color32)new Color(normal.x * 0.5f + 0.5f, normal.y * 0.5f + 0.5f, normal.z * 0.5f + 0.5f, 1f);
                        }
                        else pixels[y * Resolution + x] = (Color32)new Color(value.x, value.y, 0f, value.w);
                    }
                }
                output = new Texture2D(Resolution, Resolution, TextureFormat.RGBA32, false, true);
                output.SetPixels32(pixels); output.Apply(); File.WriteAllBytes(path, output.EncodeToPNG());
            }
            finally { if (source != null) Object.DestroyImmediate(source); if (output != null) Object.DestroyImmediate(output); }
        }
        static byte Encode(float value, byte[] table) => table[Mathf.Clamp(Mathf.RoundToInt(value * 4096f), 0, 4096)];
        static Vector2 Offset(int x, int y)
        {
            // Periodic node hashes make the atlas seamless at 4m as well as across internal cells.
            unchecked
            {
                uint h = (uint)((x & 3) + (y & 3) * 4 + 173);
                h = (h ^ (h >> 16)) * 0x7feb352du; h = (h ^ (h >> 15)) * 0x846ca68bu; h ^= h >> 16;
                return new Vector2((h & 65535u) / 65536f, (h >> 16) / 65536f);
            }
        }
        static Vector4 Sample(Color32[] pixels, int size, float u, float v, Vector2 phase, int mode, float[] linear)
        {
            float px = Mathf.Repeat(u + phase.x, 1f) * size - 0.5f, py = Mathf.Repeat(v + phase.y, 1f) * size - 0.5f;
            int x = Mathf.FloorToInt(px), y = Mathf.FloorToInt(py), mask = size - 1;
            Vector4 a = Pixel(pixels[(y & mask) * size + (x & mask)], mode, linear);
            Vector4 b = Pixel(pixels[(y & mask) * size + ((x + 1) & mask)], mode, linear);
            Vector4 c = Pixel(pixels[((y + 1) & mask) * size + (x & mask)], mode, linear);
            Vector4 d = Pixel(pixels[((y + 1) & mask) * size + ((x + 1) & mask)], mode, linear);
            return Vector4.LerpUnclamped(Vector4.LerpUnclamped(a, b, px - x), Vector4.LerpUnclamped(c, d, px - x), py - y);
        }
        static Vector4 Pixel(Color32 c, int mode, float[] linear) => mode == 0 ?
            new Vector4(linear[c.r], linear[c.g], linear[c.b], c.a / 255f) : new Vector4(c.r / 255f, c.g / 255f, c.b / 255f, c.a / 255f);
    }
}
