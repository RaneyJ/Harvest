using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Harvest.Editor
{
    public static class FarmhouseMaterialChecks
    {
        [MenuItem("Harvest/Validate Farmhouse Materials")]
        public static void Run()
        {
            if (Application.isPlaying || FarmhouseMaterialLibrary.IsInstalling)
            { Debug.Log("Validate farmhouse materials in Edit mode after installation."); return; }
            try
            {
                var manifest = FarmhouseMaterialLibrary.ReadManifest();
                foreach (var source in manifest.Materials)
                {
                    foreach (var file in source.Files)
                        FarmhouseMaterialLibrary.VerifyFile(FarmhouseMaterialLibrary.CachePath(source.Key, file.FileName), file);
                    CheckTexture(source.Key, "Albedo.jpg", true, false);
                    CheckTexture(source.Key, "NormalGL.png", false, true);
                    CheckTexture(source.Key, "URP_Mask.png", false, false);
                    CheckPackedChannels(source.Key);
                    var material = AssetDatabase.LoadAssetAtPath<Material>(FarmhouseMaterialLibrary.MaterialPath(source.Key));
                    Require(material != null && material.shader.name == "Universal Render Pipeline/Lit", source.Key + ": missing URP Lit material.");
                    Require(!ShaderUtil.ShaderHasError(material.shader), source.Key + ": shader has compilation errors.");
                    foreach (string property in new[] { "_BaseMap", "_BumpMap", "_MetallicGlossMap", "_OcclusionMap" })
                        Require(material.GetTexture(property) != null, source.Key + ": missing " + property);
                    Require(material.IsKeywordEnabled("_NORMALMAP") && material.IsKeywordEnabled("_METALLICSPECGLOSSMAP") &&
                        material.IsKeywordEnabled("_OCCLUSIONMAP"), source.Key + ": PBR shader keywords are missing.");
                    Require(!material.IsKeywordEnabled("_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A") &&
                        Mathf.Approximately(material.GetFloat("_SmoothnessTextureChannel"), 0f), source.Key + ": smoothness must use packed mask alpha.");
                }
                Debug.Log("Farmhouse material checks passed: 15 verified sources, 2K import settings, sampled packed-mask channels and URP material wiring. Review GPU appearance in the neutral scene and encounter next.");
            }
            catch (Exception error) { Debug.LogError("Farmhouse material check failed: " + error.Message); }
        }
        static void CheckTexture(string key, string file, bool color, bool normal)
        {
            string path = FarmhouseMaterialLibrary.TexturePath(key, file);
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            Require(texture != null && importer != null && texture.width == 2048 && texture.height == 2048, key + ": missing/non-2K " + file);
            Require(importer.sRGBTexture == color && importer.textureType == (normal ? TextureImporterType.NormalMap : TextureImporterType.Default), key + ": incorrect color space/type for " + file);
            Require(!normal || !importer.flipGreenChannel, key + ": OpenGL normal Y channel was flipped.");
            Require(importer.mipmapEnabled && importer.wrapMode == TextureWrapMode.Repeat && importer.anisoLevel >= 8 &&
                importer.filterMode == FilterMode.Trilinear && !importer.isReadable, key + ": import sampling/readability differs for " + file);
        }
        static void CheckPackedChannels(string key)
        {
            Texture2D arm = null, mask = null;
            try
            {
                arm = FarmhouseMaterialLibrary.ReadLinearImage(FarmhouseMaterialLibrary.CachePath(key, "ARM.png"));
                mask = FarmhouseMaterialLibrary.ReadLinearImage(FarmhouseMaterialLibrary.TexturePath(key, "URP_Mask.png"));
                Require(arm.width == mask.width && arm.height == mask.height, key + ": mask resolution differs from source.");
                // Sample the real input/output files, including edge pixels, independently of the pack helper.
                for (int y = 0; y <= 8; y++) for (int x = 0; x <= 8; x++)
                {
                    int px = x * (arm.width - 1) / 8, py = y * (arm.height - 1) / 8;
                    Color32 input = arm.GetPixel(px, py), output = mask.GetPixel(px, py);
                    Require(output.r == input.b && output.g == input.r && output.b == 0 && output.a == 255 - input.g,
                        key + ": packed metal/AO/smoothness channels differ at " + px + "," + py);
                }
            }
            finally { if (arm != null) Object.DestroyImmediate(arm); if (mask != null) Object.DestroyImmediate(mask); }
        }
        static void Require(bool condition, string message)
        { if (!condition) throw new InvalidOperationException(message); }
    }
}
