using System.Collections.Generic;
using UnityEngine;

namespace Harvest
{
    // Damage origins are gameplay events; camera-relative indicators remain presentation only.
    public sealed class PlayerDamageFeedback : MonoBehaviour
    {
        public MarineArmor Armor;
        public Camera View;
        [Min(0.1f)] public float IndicatorSeconds = 1.2f;
        [Range(0f, 1f)] public float BreachedTint = 0.12f;
        [Range(0f, 1f)] public float CriticalTint = 0.65f;
        struct HitDirection { public Vector3 Origin; public float Until; }
        readonly List<HitDirection> hits = new List<HitDirection>();
        Vitality vitality;
        Texture2D vignette;
        float tint;

        public static float RedStrength(float armor, float healthFraction, float breached, float critical) =>
            armor > 0f ? 0f : Mathf.Lerp(breached, critical, 1f - Mathf.Clamp01(healthFraction));
        public static float Bearing(Vector3 forward, Vector3 right, Vector3 toward) =>
            Mathf.Atan2(Vector3.Dot(toward, right), Vector3.Dot(toward, forward)) * Mathf.Rad2Deg;

        void OnEnable()
        {
            if (Armor == null) Armor = GetComponentInParent<MarineArmor>();
            if (View == null) View = GetComponent<Camera>();
            if (Armor == null) return;
            vitality = Armor.GetComponent<Vitality>();
            Armor.DamageReceived += OnDamage;
            Armor.ArmorReset += ResetFeedback;
            if (vignette == null)
            {
                vignette = new Texture2D(64, 64, TextureFormat.RGBA32, false);
                vignette.wrapMode = TextureWrapMode.Clamp;
                for (int y = 0; y < 64; y++)
                    for (int x = 0; x < 64; x++)
                    {
                        float edge = Mathf.Max(Mathf.Abs(x / 63f * 2f - 1f), Mathf.Abs(y / 63f * 2f - 1f));
                        float alpha = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.35f, 1f, edge));
                        vignette.SetPixel(x, y, new Color(0.7f, 0.02f, 0.01f, alpha));
                    }
                vignette.Apply();
            }
        }
        void OnDisable()
        {
            if (Armor != null) { Armor.DamageReceived -= OnDamage; Armor.ArmorReset -= ResetFeedback; }
            ResetFeedback();
        }
        void OnDestroy() { if (vignette != null) Destroy(vignette); }
        void ResetFeedback() { hits.Clear(); tint = 0f; }
        void OnDamage(Vector3 origin)
        {
            if (vitality == null || !vitality.IsAlive) return;
            if (hits.Count >= 8) hits.RemoveAt(0);
            hits.Add(new HitDirection { Origin = origin, Until = Time.time + IndicatorSeconds });
        }
        void Update()
        {
            hits.RemoveAll(hit => Time.time >= hit.Until);
            if (vitality == null || Armor == null) return;
            float target = vitality.IsAlive ? RedStrength(Armor.Armor,
                vitality.Health / Mathf.Max(1f, vitality.MaxHealth), BreachedTint, CriticalTint) : 0f;
            tint = Mathf.MoveTowards(tint, target, Time.deltaTime * 2f);
        }
        void OnGUI()
        {
            if (vitality == null || !vitality.IsAlive || View == null) return;
            Color color = GUI.color;
            Matrix4x4 matrix = GUI.matrix;
            if (vignette != null && tint > 0f)
            {
                GUI.color = new Color(1f, 1f, 1f, tint);
                GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), vignette);
            }
            Vector2 center = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            float radius = Mathf.Min(Screen.width, Screen.height) * 0.14f;
            foreach (HitDirection hit in hits)
            {
                Vector3 toward = hit.Origin - View.transform.position;
                float angle = Bearing(View.transform.forward, View.transform.right, toward);
                GUI.matrix = matrix;
                GUIUtility.RotateAroundPivot(angle, center);
                GUI.color = new Color(1f, 0.12f, 0.06f, Mathf.Clamp01((hit.Until - Time.time) / IndicatorSeconds));
                GUI.DrawTexture(new Rect(center.x - 20f, center.y - radius, 40f, 5f), Texture2D.whiteTexture);
            }
            GUI.matrix = matrix;
            GUI.color = color;
        }
    }
}
