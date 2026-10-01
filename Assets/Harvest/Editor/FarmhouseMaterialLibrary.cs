using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using Object = UnityEngine.Object;

namespace Harvest.Editor
{
    [Serializable] public sealed class FarmhouseMaterialManifest
    {
        public int Version, Resolution;
        public string Provider, License;
        public FarmhouseMaterialSource[] Materials;
    }
    [Serializable] public sealed class FarmhouseMaterialSource
    {
        public string Key, AssetId, SourcePage, Authors;
        public float WidthMeters, HeightMeters, NormalStrength, OcclusionStrength, SmoothnessScale;
        public Color Tint;
        public FarmhouseTextureSource[] Files;
    }
    [Serializable] public sealed class FarmhouseTextureSource
    {
        public string Role, Url, Md5, Sha256, FileName;
        public long Bytes;
    }
    // A reproducible source installation. Rebuilds only assign maps to untouched base materials.
    public static class FarmhouseMaterialLibrary
    {
        public const string ManifestPath = "Assets/Harvest/Editor/FarmhouseMaterialSources.json";
        public const string SurfaceFolder = "Assets/Harvest/Art/Surfaces/PolyHaven";
        public static string CacheFolder => Path.GetFullPath(Path.Combine(Application.dataPath, "../Library/HarvestMaterialCache"));
        sealed class Job { public FarmhouseMaterialSource Material; public FarmhouseTextureSource File; }
        static Queue<Job> jobs;
        static FarmhouseMaterialManifest installing;
        static Job active;
        static UnityWebRequest request;
        static int completed, total;
        public static bool IsInstalling => jobs != null;

        public static FarmhouseMaterialManifest ReadManifest()
        {
            var manifest = JsonUtility.FromJson<FarmhouseMaterialManifest>(File.ReadAllText(ManifestPath));
            if (manifest == null || manifest.Version != 1 || manifest.Resolution != 2048 || manifest.Materials == null || manifest.Materials.Length != 5)
                throw new InvalidOperationException("Farmhouse PBR source manifest is missing or invalid.");
            var keys = new HashSet<string>();
            foreach (var source in manifest.Materials)
            {
                if (source == null || string.IsNullOrEmpty(source.Key) || !keys.Add(source.Key) || source.Key.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 ||
                    source.WidthMeters <= 0f || source.HeightMeters <= 0f || source.Files == null || source.Files.Length != 3)
                    throw new InvalidOperationException("Invalid material source: " + source.Key);
                var roles = new HashSet<string>();
                foreach (var file in source.Files)
                {
                    if (file == null || string.IsNullOrEmpty(file.FileName)) throw new InvalidOperationException("Missing pinned texture.");
                    var uri = new Uri(file.Url);
                    if (uri.Scheme != "https" || uri.Host != "dl.polyhaven.org" || !uri.AbsolutePath.StartsWith("/file/ph-assets/Textures/") ||
                        file.Bytes <= 0 || file.Sha256 == null || file.Sha256.Length != 64 || Path.GetFileName(file.FileName) != file.FileName ||
                        !roles.Add(file.Role)) throw new InvalidOperationException("Invalid pinned texture: " + source.Key);
                }
                if (!roles.SetEquals(new[] { "Albedo", "NormalGL", "ARM" })) throw new InvalidOperationException("Missing source channels: " + source.Key);
            }
            return manifest;
        }
        public static string MaterialPath(string key) => "Assets/Harvest/Materials/Farmhouse Foundation " + key + ".mat";
        public static string TexturePath(string key, string file) => SurfaceFolder + "/" + key + "/" + file;
        public static string CachePath(string key, string file) => Path.Combine(CacheFolder, key, file);

        [MenuItem("Harvest/Install Farmhouse PBR Materials")]
        public static void Install()
        {
            if (Application.isPlaying || EditorApplication.isCompiling || IsInstalling)
            { Debug.Log("Install farmhouse materials in Edit mode after compilation completes."); return; }
            try
            {
                installing = ReadManifest(); jobs = new Queue<Job>(); completed = 0;
                foreach (var source in installing.Materials)
                    foreach (var file in source.Files) jobs.Enqueue(new Job { Material = source, File = file });
                total = jobs.Count;
                EditorApplication.update += Tick;
                AssemblyReloadEvents.beforeAssemblyReload += Cancel;
                EditorApplication.quitting += Cancel;
                Debug.Log("Installing 2K CC0 materials from Poly Haven (about 156 MB initially). Verified cached files are reused; existing material edits are preserved.");
            }
            catch (Exception error) { Cancel(); Debug.LogError("Farmhouse material installation failed: " + error.Message); }
        }
        static void Tick()
        {
            try
            {
                if (Application.isPlaying || EditorApplication.isCompiling) { Cancel(); return; }
                float fraction = (completed + (request != null ? Mathf.Max(0f, request.downloadProgress) : 0f)) / Mathf.Max(1, total);
                if (EditorUtility.DisplayCancelableProgressBar("Poly Haven farmhouse materials", active != null ? active.Material.Key + " / " + active.File.Role : "Checking source cache", fraction))
                { Cancel(); Debug.Log("Material installation cancelled. Verified cache files are retained; run Install to resume."); return; }
                if (request != null)
                {
                    if (!request.isDone) return;
                    if (request.result != UnityWebRequest.Result.Success) throw new IOException(request.error);
                    string path = CachePath(active.Material.Key, active.File.FileName), partial = path + ".partial";
                    request.Dispose(); request = null;
                    VerifyFile(partial, active.File);
                    File.Copy(partial, path, true); File.Delete(partial); completed++; active = null;
                }
                while (jobs.Count > 0)
                {
                    active = jobs.Dequeue(); string path = CachePath(active.Material.Key, active.File.FileName);
                    if (File.Exists(path))
                    {
                        try { VerifyFile(path, active.File); completed++; active = null; continue; }
                        catch (InvalidOperationException) { /* Replace a corrupt cache entry after verification. */ }
                    }
                    Directory.CreateDirectory(Path.GetDirectoryName(path));
                    request = new UnityWebRequest(active.File.Url, UnityWebRequest.kHttpVerbGET);
                    request.SetRequestHeader("User-Agent", "HarvestMaterialTools/1.0 (Poly Haven)");
                    request.timeout = 90;
                    request.downloadHandler = new DownloadHandlerFile(path + ".partial") { removeFileOnAbort = true };
                    request.SendWebRequest(); return;
                }
                var manifest = installing;
                Stop();
                ImportSources(manifest);
                int applied = ApplyInstalled();
                Debug.Log("Farmhouse PBR installation complete: verified Poly Haven source maps imported, URP masks packed; " + applied +
                    " untouched materials updated. Rebuild The Line once for roof UV alignment, then Build Farmhouse Material Review.");
            }
            catch (Exception error) { Cancel(); Debug.LogError("Farmhouse material installation failed: " + error.Message); }
        }
        static void Stop()
        {
            EditorApplication.update -= Tick; AssemblyReloadEvents.beforeAssemblyReload -= Cancel; EditorApplication.quitting -= Cancel;
            EditorUtility.ClearProgressBar(); jobs = null; installing = null; active = null;
        }
        static void Cancel()
        {
            string partial = active != null ? CachePath(active.Material.Key, active.File.FileName) + ".partial" : null;
            if (request != null) { request.Abort(); request.Dispose(); request = null; }
            if (partial != null && File.Exists(partial)) File.Delete(partial);
            Stop();
        }
        public static void VerifyFile(string path, FarmhouseTextureSource source)
        {
            if (!File.Exists(path) || new FileInfo(path).Length != source.Bytes) throw new InvalidOperationException("Source byte count differs: " + source.FileName);
            using (var stream = File.OpenRead(path))
            using (var sha = SHA256.Create())
                if (BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant() != source.Sha256)
                    throw new InvalidOperationException("Source checksum differs: " + source.FileName);
        }
        static void ImportSources(FarmhouseMaterialManifest manifest)
        {
            try
            {
                for (int i = 0; i < manifest.Materials.Length; i++)
                {
                    var source = manifest.Materials[i];
                    EditorUtility.DisplayProgressBar("Importing farmhouse PBR maps", source.Key, i / (float)manifest.Materials.Length);
                    FarmhouseMeshLibrary.EnsureFolder(SurfaceFolder + "/" + source.Key);
                    foreach (var file in source.Files)
                    {
                        string cache = CachePath(source.Key, file.FileName); VerifyFile(cache, file);
                        if (file.Role == "ARM") continue;
                        string path = TexturePath(source.Key, file.FileName);
                        if (File.Exists(path)) VerifyFile(path, file); // Never silently replace an edited source texture.
                        else File.Copy(cache, path);
                        ConfigureTexture(path, file.Role == "Albedo", file.Role == "NormalGL");
                    }
                    string mask = TexturePath(source.Key, "URP_Mask.png");
                    if (!File.Exists(mask))
                    {
                        Texture2D arm = null, packed = null;
                        try
                        {
                            arm = ReadLinearImage(CachePath(source.Key, "ARM.png"));
                            RequireResolution(arm, manifest.Resolution);
                            Color32[] pixels = arm.GetPixels32();
                            for (int p = 0; p < pixels.Length; p++) pixels[p] = PackARM(pixels[p]);
                            packed = new Texture2D(arm.width, arm.height, TextureFormat.RGBA32, false, true);
                            packed.SetPixels32(pixels); packed.Apply(); File.WriteAllBytes(mask, packed.EncodeToPNG());
                        }
                        finally { if (arm != null) Object.DestroyImmediate(arm); if (packed != null) Object.DestroyImmediate(packed); }
                    }
                    ConfigureTexture(mask, false, false);
                }
                AssetDatabase.SaveAssets();
            }
            finally { EditorUtility.ClearProgressBar(); }
        }
        // Poly Haven ARM = AO / roughness / metal. URP Lit = metal(R), AO(G), smoothness(A).
        public static Color32 PackARM(Color32 arm) => new Color32(arm.b, arm.r, 0, (byte)(255 - arm.g));
        public static Texture2D ReadLinearImage(string path)
        {
            var image = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
            try
            {
                if (!image.LoadImage(File.ReadAllBytes(path))) throw new InvalidOperationException("Cannot decode source image: " + path);
                return image;
            }
            catch { Object.DestroyImmediate(image); throw; }
        }
        static void RequireResolution(Texture2D image, int size)
        { if (image.width != size || image.height != size) throw new InvalidOperationException("Source texture resolution differs from its manifest."); }
        public static void ConfigureTexture(string path, bool color, bool normal, int resolution = 2048)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            importer.sRGBTexture = color; importer.flipGreenChannel = false; // Unity uses Y+ / OpenGL normals.
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = false; importer.isReadable = false;
            importer.mipmapEnabled = true; importer.wrapMode = TextureWrapMode.Repeat;
            importer.filterMode = FilterMode.Trilinear; importer.anisoLevel = 8; importer.maxTextureSize = resolution;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.SaveAndReimport();
        }
        public static int ApplyInstalled()
        {
            int applied = 0;
            foreach (var source in ReadManifest().Materials)
            {
                Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath(source.Key));
                if (material == null || material.shader.name != "Universal Render Pipeline/Lit" || HasAuthoredMaps(material)) continue;
                var albedo = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath(source.Key, "Albedo.jpg"));
                var normal = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath(source.Key, "NormalGL.png"));
                var mask = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath(source.Key, "URP_Mask.png"));
                if (albedo == null || normal == null || mask == null) continue;
                // Preserve deliberately edited tint/smoothness; replace only the foundation defaults.
                Color current = material.GetColor("_BaseColor"), initial = InitialTint(source.Key);
                if (Mathf.Abs(current.r-initial.r) + Mathf.Abs(current.g-initial.g) + Mathf.Abs(current.b-initial.b) < 0.001f)
                    material.SetColor("_BaseColor", source.Tint);
                if (Mathf.Abs(material.GetFloat("_Smoothness") - InitialSmoothness(source.Key)) < 0.001f)
                    material.SetFloat("_Smoothness", source.SmoothnessScale);
                material.SetTexture("_BaseMap", albedo); material.SetTexture("_BumpMap", normal);
                material.SetTexture("_MetallicGlossMap", mask); material.SetTexture("_OcclusionMap", mask);
                if (material.GetTextureScale("_BaseMap") == Vector2.one && material.GetTextureOffset("_BaseMap") == Vector2.zero)
                    material.SetTextureScale("_BaseMap", new Vector2(1f/source.WidthMeters, 1f/source.HeightMeters));
                if (Mathf.Approximately(material.GetFloat("_BumpScale"), 1f)) material.SetFloat("_BumpScale", source.NormalStrength);
                if (Mathf.Approximately(material.GetFloat("_OcclusionStrength"), 1f)) material.SetFloat("_OcclusionStrength", source.OcclusionStrength);
                material.SetFloat("_SmoothnessTextureChannel", 0f);
                material.DisableKeyword("_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A");
                material.EnableKeyword("_NORMALMAP"); material.EnableKeyword("_METALLICSPECGLOSSMAP"); material.EnableKeyword("_OCCLUSIONMAP");
                EditorUtility.SetDirty(material); applied++;
            }
            if (applied > 0) { AssetDatabase.SaveAssets(); SceneView.RepaintAll(); }
            return applied;
        }
        static bool HasAuthoredMaps(Material material) => material.GetTexture("_BaseMap") != null || material.GetTexture("_BumpMap") != null ||
            material.GetTexture("_MetallicGlossMap") != null || material.GetTexture("_OcclusionMap") != null;
        static Color InitialTint(string key)
        {
            switch (key)
            {
                case "Plaster": return new Color(0.72f,0.69f,0.60f);
                case "Timber": return new Color(0.30f,0.24f,0.16f);
                case "Roofing": return new Color(0.33f,0.35f,0.34f);
                case "Steel": return new Color(0.18f,0.20f,0.20f);
                default: return new Color(0.40f,0.41f,0.37f);
            }
        }
        static float InitialSmoothness(string key)
        {
            switch (key)
            { case "Plaster": return 0.12f; case "Timber": return 0.18f; case "Roofing": return 0.25f; case "Steel": return 0.30f; default: return 0.10f; }
        }
    }
}
