using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace Harvest.Editor
{
    // Real-camera spatial test: copy must preserve four quadrants, blur must soften an edge,
    // interiors must stay recognizable, and recovery must restore the original image.
    public static class SuppressionRenderChecks
    {
        static GameObject root;
        static Camera camera;
        static Suppression pressure;
        static SuppressionScreenBlur blur;
        static RenderTexture target;
        static Color[] baseline;
        static readonly Vector2Int[] Points = { new Vector2Int(16,16), new Vector2Int(48,16),
            new Vector2Int(16,48), new Vector2Int(48,48), new Vector2Int(31,16) };
        static readonly Material[] patternMaterials = new Material[4];
        static int phase;
        static int scenario;
        static int capturedFrame;
        static bool pending;
        static string bindingError;
        static double deadline;

        [MenuItem("Harvest/Run Suppression Render Check (Play Mode)")]
        public static void Run()
        {
            if (!Application.isPlaying || !(GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset))
            {
                Debug.Log("Enter Play mode in The Line with URP active, then run the suppression render check.");
                return;
            }
            Cleanup();
            root = new GameObject("Temporary suppression GPU check");
            root.transform.position = new Vector3(10000f, 10000f, 10000f);
            camera = root.AddComponent<Camera>();
            camera.cullingMask = 1 << 31; camera.clearFlags = CameraClearFlags.SolidColor;
            camera.orthographic = true; camera.orthographicSize = 1f; camera.aspect = 1f;
            camera.nearClipPlane = 0.1f; camera.farClipPlane = 10f;
            Shader unlit = Shader.Find("Universal Render Pipeline/Unlit");
            if (unlit == null || !unlit.isSupported) { Cleanup(); Debug.LogError("The test pattern needs the supported URP Unlit shader."); return; }
            Color[] colors = { new Color(0.8f,0.1f,0.1f), new Color(0.1f,0.1f,0.8f),
                new Color(0.1f,0.8f,0.1f), new Color(0.8f,0.8f,0.1f) };
            for (int i = 0; i < 4; i++)
            {
                GameObject patch = GameObject.CreatePrimitive(PrimitiveType.Quad);
                patch.name = "GPU check quadrant"; patch.layer = 31;
                patch.transform.SetParent(root.transform, false);
                patch.transform.localPosition = new Vector3(i % 2 == 0 ? -0.5f : 0.5f, i < 2 ? -0.5f : 0.5f, 5f);
                Object.Destroy(patch.GetComponent<Collider>());
                patternMaterials[i] = new Material(unlit);
                patternMaterials[i].SetColor("_BaseColor", colors[i]);
                patternMaterials[i].SetFloat("_Cull", 0f);
                patch.GetComponent<Renderer>().sharedMaterial = patternMaterials[i];
            }
            camera.backgroundColor = new Color(0.23f, 0.47f, 0.68f, 1f);
            camera.allowHDR = false; camera.allowMSAA = false;
            var cameraData = camera.GetUniversalAdditionalCameraData();
            cameraData.renderPostProcessing = false;
            cameraData.antialiasing = AntialiasingMode.None;
            target = new RenderTexture(64, 64, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            target.Create(); camera.targetTexture = target;
            pressure = root.AddComponent<Suppression>();
            pressure.RecoveryDelay = 1000f;
            blur = root.AddComponent<SuppressionScreenBlur>();
            blur.State = pressure; blur.FollowSpeed = 1000f;
            phase = 0; scenario = 0; pending = false; bindingError = null;
            deadline = EditorApplication.timeSinceStartup + 10f;
            RenderPipelineManager.endCameraRendering += OnRendered;
            EditorApplication.update += Watch;
            EditorApplication.playModeStateChanged += OnPlayState;
            Application.logMessageReceived += OnLog;
            Debug.Log("Checking URP output with and without HDR/post-processing/FXAA: reference, copy, blur, recovery...");
        }
        static void OnRendered(ScriptableRenderContext context, Camera rendered)
        {
            if (rendered != camera || pending || ((phase == 1 || phase == 2) && blur.VisiblePressure < 0.99f) ||
                (phase == 3 && blur.VisiblePressure > 0f)) return;
            capturedFrame = Time.frameCount;
            pending = true;
            // Read on the next editor tick, after the renderer has submitted this frame.
            EditorApplication.delayCall += ReadResult;
        }
        static void ReadResult()
        {
            if (root == null || !pending) return;
            Texture2D sample = null;
            RenderTexture previous = RenderTexture.active;
            try
            {
                if (bindingError != null) throw new Exception(bindingError);
                RenderTexture.active = target;
                sample = new Texture2D(64, 64, TextureFormat.RGBA32, false, true);
                sample.ReadPixels(new Rect(0f, 0f, 64f, 64f), 0, 0); sample.Apply();
                Color[] result = new Color[Points.Length];
                for (int i = 0; i < Points.Length; i++) result[i] = sample.GetPixel(Points[i].x, Points[i].y);
                RenderTexture.active = previous;
                if (phase == 0)
                {
                    if (Difference(result[0], result[1]) < 0.2f || Difference(result[0], result[2]) < 0.2f)
                        throw new Exception("The test pattern did not render distinct quadrants. Reference samples: " + result[0] + ", " + result[1] + ", " + result[2]);
                    baseline = result;
                    phase = 1; blur.DiagnosticCopyOnly = true; pressure.AddPressure(1f);
                }
                else
                {
                    if (phase < 3 && blur.LastScheduledRenderFrame < capturedFrame)
                        throw new Exception("Suppression feature never scheduled the " + (phase == 1 ? "copy" : "blur") + " pass.");
                    int preservedSamples = phase == 2 ? 4 : Points.Length;
                    for (int i = 0; i < preservedSamples; i++)
                        if (Difference(result[i], baseline[i]) > 0.025f)
                            throw new Exception("Phase " + phase + " changed quadrant/pixel " + i + ". Reference: " + baseline[i] + "; rendered: " + result[i] + ". Check source binding and UVs.");
                    if (phase == 1) { phase = 2; blur.DiagnosticCopyOnly = false; }
                    else if (phase == 2)
                    {
                        if (Difference(result[4], baseline[4]) < 0.015f)
                            throw new Exception("Copy works, but blur did not soften the test edge. Check blur parameters and shader pass.");
                        phase = 3; pressure.ResetPressure();
                    }
                    else
                    {
                        if (blur.LastScheduledRenderFrame >= capturedFrame)
                            throw new Exception("Suppression pass stayed active after pressure reset.");
                        if (scenario == 0)
                        {
                            scenario = 1; phase = 0; baseline = null;
                            camera.allowHDR = true;
                            var cameraData = camera.GetUniversalAdditionalCameraData();
                            cameraData.renderPostProcessing = true;
                            cameraData.antialiasing = AntialiasingMode.FastApproximateAntialiasing;
                            deadline = EditorApplication.timeSinceStartup + 10f;
                        }
                        else
                        {
                            Cleanup();
                            Debug.Log("Suppression GPU check passed with and without HDR/post-processing/FXAA: spatial copy preserved, edge blurred, interior colors visible, recovery restored the image, no property-sheet conflicts.");
                            return;
                        }
                    }
                }
                pending = false;
            }
            catch (Exception error) { Cleanup(); Debug.LogError("Suppression GPU check failed (post-processing " + (scenario == 1 ? "on" : "off") + "): " + error.Message); }
            finally
            {
                RenderTexture.active = previous;
                if (sample != null) Object.Destroy(sample);
            }
        }
        static float Difference(Color a, Color b) => Mathf.Max(Mathf.Abs(a.r - b.r), Mathf.Max(Mathf.Abs(a.g - b.g), Mathf.Abs(a.b - b.b)));
        static void OnLog(string message, string stack, LogType type)
        {
            if (message.IndexOf("property sheet", StringComparison.OrdinalIgnoreCase) >= 0 || message.Contains("<noninit>"))
                bindingError = message;
        }
        static void Watch()
        {
            if (root == null || EditorApplication.timeSinceStartup <= deadline) return;
            Cleanup(); Debug.LogError("Suppression GPU check timed out. Keep the Game view active and Play mode unpaused.");
        }
        static void OnPlayState(PlayModeStateChange state) { if (state == PlayModeStateChange.ExitingPlayMode) Cleanup(); }
        static void Cleanup()
        {
            RenderPipelineManager.endCameraRendering -= OnRendered;
            EditorApplication.update -= Watch;
            EditorApplication.delayCall -= ReadResult;
            EditorApplication.playModeStateChanged -= OnPlayState;
            Application.logMessageReceived -= OnLog;
            if (camera != null) { camera.enabled = false; camera.targetTexture = null; }
            if (target != null) { target.Release(); Object.Destroy(target); }
            if (root != null) Object.Destroy(root);
            for (int i = 0; i < patternMaterials.Length; i++)
            {
                if (patternMaterials[i] != null) Object.Destroy(patternMaterials[i]);
                patternMaterials[i] = null;
            }
            root = null; camera = null; target = null; pressure = null; blur = null; pending = false;
        }
    }
}
