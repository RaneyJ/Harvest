using System.Collections.Generic;
using UnityEngine;

namespace Harvest
{
    // Scene-owned presentation listens to resolved shots. Lines never collide or deal damage.
    public sealed class HitscanTracerRenderer : MonoBehaviour
    {
        public Material TracerMaterial;
        [Min(8)] public int MaxTracers = 128;
        sealed class Trace { public LineRenderer Line; public float Until; public float Lifetime; public Color Color; }
        readonly List<Trace> traces = new List<Trace>();
        void OnEnable() => WeaponRuntime.HitscanFired += Show;
        void OnDisable()
        {
            WeaponRuntime.HitscanFired -= Show;
            foreach (Trace trace in traces) if (trace.Line != null) trace.Line.enabled = false;
        }
        void Show(WeaponDefinition definition, Vector3 start, Vector3 end)
        {
            if (TracerMaterial == null || !definition.ShowTracers || (end - start).sqrMagnitude < 0.001f) return;
            Trace trace = traces.Find(item => item.Until <= Time.time);
            if (trace == null && traces.Count < MaxTracers)
            {
                GameObject root = new GameObject("Hitscan tracer");
                root.transform.SetParent(transform, false);
                LineRenderer line = root.AddComponent<LineRenderer>();
                line.sharedMaterial = TracerMaterial;
                line.useWorldSpace = true;
                line.positionCount = 2;
                line.numCapVertices = 2;
                line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                line.receiveShadows = false;
                trace = new Trace { Line = line };
                traces.Add(trace);
            }
            if (trace == null) trace = traces[0];
            trace.Lifetime = Mathf.Max(0.01f, definition.TracerLifetime);
            trace.Until = Time.time + trace.Lifetime;
            trace.Color = definition.TracerColor;
            trace.Line.startWidth = trace.Line.endWidth = definition.TracerWidth;
            // Keep the line clear of the camera while retaining the actual obstruction endpoint.
            trace.Line.SetPosition(0, Vector3.MoveTowards(start, end, Mathf.Min(0.7f, Vector3.Distance(start, end) * 0.2f)));
            trace.Line.SetPosition(1, end);
            trace.Line.enabled = true;
            trace.Line.startColor = trace.Line.endColor = trace.Color;
        }
        void Update()
        {
            foreach (Trace trace in traces)
            {
                trace.Line.enabled = Time.time < trace.Until;
                if (!trace.Line.enabled) continue;
                Color color = trace.Color;
                color.a *= Mathf.Clamp01((trace.Until - Time.time) / trace.Lifetime);
                trace.Line.startColor = trace.Line.endColor = color;
            }
        }
    }
}
