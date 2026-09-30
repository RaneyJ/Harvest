using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace Harvest.Editor
{
    // Exercises the real URP feature on a constant-color offscreen camera. Blur must preserve
    // its color, run above threshold, stop below it, and never log a property-sheet conflict.
    public static class SuppressionRenderChecks
    {
        static GameObject root;
        static Camera camera;
        static Suppression pressure;
        static SuppressionScreenBlur blur;
        static RenderTexture target;
        static Color baseline;
        static int phase;
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
            camera.cullingMask = 0; camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.23f, 0.47f, 0.68f, 1f);
            camera.allowHDR = false; camera.allowMSAA = false;
            camera.GetUniversalAdditionalCameraData().renderPostProcessing = false;
            target = new RenderTexture(64, 64, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            target.Create(); camera.targetTexture = target;
            pressure = root.AddComponent<Suppression>();
            pressure.RecoveryDelay = 1000f;
            blur = root.AddComponent<SuppressionScreenBlur>();
            blur.State = pressure; blur.FollowSpeed = 1000f;
            phase = 0; pending = false; bindingError = null;
            deadline = EditorApplication.timeSinceStartup + 10f;
            RenderPipelineManager.endCameraRendering += OnRendered;
            EditorApplication.update += Watch;
            EditorApplication.playModeStateChanged += OnPlayState;
            Application.logMessageReceived += OnLog;
            Debug.Log("Checking actual offscreen URP output at zero, full and reset suppression...");
        }
        static void OnRendered(ScriptableRenderContext context, Camera rendered)
        {
            if (rendered != camera || pending || (phase == 1 && blur.VisiblePressure < 0.99f) ||
                (phase == 2 && blur.VisiblePressure > 0f)) return;
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
                sample = new Texture2D(1, 1, TextureFormat.RGBA32, false, true);
                sample.ReadPixels(new Rect(32f, 32f, 1f, 1f), 0, 0); sample.Apply();
                Color result = sample.GetPixel(0, 0);
                RenderTexture.active = previous;
                if (phase == 0)
                {
                    if (Mathf.Max(result.r, Mathf.Max(result.g, result.b)) < 0.1f) throw new Exception("Baseline camera output is black; the test camera did not render.");
                    baseline = result;
                    phase = 1; pressure.AddPressure(1f);
                }
                else
                {
                    if (phase == 1 && blur.LastScheduledRenderFrame < capturedFrame)
                        throw new Exception("Suppression feature never scheduled a pass for the test camera.");
                    if (Difference(result, baseline) > 0.025f)
                        throw new Exception("Suppression changed a flat camera color (possibly black output). Baseline: " + baseline + "; rendered: " + result);
                    if (phase == 1) { phase = 2; pressure.ResetPressure(); }
                    else
                    {
                        if (blur.LastScheduledRenderFrame >= capturedFrame)
                            throw new Exception("Suppression pass stayed active after pressure reset.");
                        Cleanup();
                        Debug.Log("Suppression GPU check passed: pass executed, constant color preserved, reset skipped blur, no property-sheet conflicts.");
                        return;
                    }
                }
                pending = false;
            }
            catch (Exception error) { Cleanup(); Debug.LogError("Suppression GPU check failed: " + error.Message); }
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
            root = null; camera = null; target = null; pressure = null; blur = null; pending = false;
        }
    }
}
