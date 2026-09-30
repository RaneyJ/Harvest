using System.IO;
using UnityEditor;
using UnityEngine;

namespace Harvest.Editor
{
    // Deterministic starter materials. World-space mapping avoids stretched textures on graybox walls.
    public static class HarvestSurfaceLibrary
    {
        const string Folder = "Assets/Harvest/Rendering/Surfaces";
        public static void Apply()
        {
            Directory.CreateDirectory(Folder);
            Set("Soil", "Soil", 0.8f, 0.06f);
            Set("Freight Road", "Gravel", 1.6f, 0.08f);
            Set("Concrete", "Concrete", 0.85f, 0.12f);
            Set("Farmhouse Plaster", "Plaster", 0.7f, 0.07f);
            Set("Farmhouse Roof", "Roof", 0.65f, 0.22f, 0.25f);
            Set("Rust", "Wood", 0.65f, 0.10f);
            Set("Hunting Walnut", "Wood", 1f, 0.25f, world: false);
            Set("Weapon Steel", "Steel", 1f, 0.32f, 0.65f, false);
            Set("Weapon Grip", "Grip", 2f, 0.12f, world: false);
        }
        static void Set(string name, string pattern, float scale, float smoothness, float metallic = 0f, bool world = true)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Harvest/Materials/" + name + ".mat");
            if (material == null) return;
            // Only migrate known starter shaders once; edited/imported materials remain authored assets.
            if (material.shader.name != "Universal Render Pipeline/Lit") return;
            if (material.GetTexture("_BaseMap") != null) return;
            Color tint = material.color;
            Texture2D texture = Texture(pattern);
            if (world)
            {
                material.shader = Shader.Find("Harvest/Farm Surface");
                material.SetColor("_BaseColor", tint);
                material.SetTexture("_SurfaceMap", texture);
                material.SetFloat("_WorldScale", scale);
            }
            else
            {
                material.SetTexture("_BaseMap", texture);
                material.SetTextureScale("_BaseMap", Vector2.one * scale);
            }
            material.SetFloat("_Smoothness", smoothness);
            material.SetFloat("_Metallic", metallic);
            EditorUtility.SetDirty(material);
        }
        static Texture2D Texture(string pattern)
        {
            string path = Folder + "/" + pattern + ".asset";
            Texture2D existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (existing != null) return existing;
            const int size = 256;
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float u = x / (float)size, v = y / (float)size;
                    float broad = Noise(u, v, 5f), fine = Noise(u, v, 48f), detail = Noise(u, v, 100f);
                    float value = 0.68f + broad * 0.22f + fine * 0.10f;
                    if (pattern == "Gravel") value = 0.58f + fine * 0.35f + detail * 0.16f;
                    if (pattern == "Wood") value = 0.58f + 0.18f * Mathf.Sin(u * Mathf.PI * 24f + broad * 3f) + fine * 0.16f;
                    if (pattern == "Roof") value *= Mathf.Repeat(u * 8f, 1f) < 0.07f ? 0.55f : 1f;
                    if (pattern == "Steel") value = 0.82f + fine * 0.13f - (detail > 0.7f ? 0.15f : 0f);
                    if (pattern == "Grip") value = 0.65f + ((Mathf.Repeat(u * 32f, 1f) < 0.12f || Mathf.Repeat(v * 32f, 1f) < 0.12f) ? 0f : 0.25f);
                    pixels[y * size + x] = new Color(value, value, value, fine);
                }
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, true) { name = pattern, wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Trilinear, anisoLevel = 4 };
            texture.SetPixels(pixels); texture.Apply(true, false);
            AssetDatabase.CreateAsset(texture, path);
            return texture;
        }
        static float Noise(float u, float v, float frequency)
        {
            // Blend opposite edges for seamless repetition; no runtime texture generation.
            float a = Mathf.PerlinNoise(u * frequency + 13f, v * frequency + 37f);
            float b = Mathf.PerlinNoise((u - 1f) * frequency + 13f, v * frequency + 37f);
            float c = Mathf.PerlinNoise(u * frequency + 13f, (v - 1f) * frequency + 37f);
            float d = Mathf.PerlinNoise((u - 1f) * frequency + 13f, (v - 1f) * frequency + 37f);
            return Mathf.Lerp(Mathf.Lerp(a, b, u), Mathf.Lerp(c, d, u), v);
        }
    }
}
