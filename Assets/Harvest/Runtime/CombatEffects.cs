using System.Collections.Generic;
using UnityEngine;

namespace Harvest
{
    public enum CombatImpactKind { World, Armor, Flesh, Shield }
    // Bounded scene-owned effects pool. Combat reports events; presentation never applies damage.
    public sealed class CombatEffects : MonoBehaviour
    {
        public Material EffectMaterial;
        [Min(8)] public int MaximumEffects = 96;
        sealed class Effect { public LineRenderer Line; public float Until; public float Lifetime; public Color Color; }
        readonly List<Effect> effects = new List<Effect>();
        MarineLoadout player;
        PrecisionAim aim;
        void Start() { player = FindFirstObjectByType<MarineLoadout>(); if (player != null) aim = player.GetComponent<PrecisionAim>(); }
        void OnEnable() { WeaponRuntime.ShotPresented += OnShot; WeaponRuntime.ImpactPresented += OnImpact; }
        void OnDisable()
        {
            WeaponRuntime.ShotPresented -= OnShot; WeaponRuntime.ImpactPresented -= OnImpact;
            foreach (Effect effect in effects) if (effect.Line != null) effect.Line.enabled = false;
        }
        void OnShot(WeaponDefinition definition, Vector3 origin, Vector3 direction, CombatTeam team)
        {
            Vector3 position = origin + direction * 0.8f;
            if (team == CombatTeam.Marine && player != null && player.View != null &&
                (player.View.transform.position - origin).sqrMagnitude < 0.01f)
            {
                Vector3 local = aim != null && aim.IsAiming ? new Vector3(0f, -0.03f, 1.1f) : new Vector3(0.34f, -0.28f, 1.05f);
                position = player.View.transform.TransformPoint(local);
            }
            Color color = definition.ShotKind == WeaponShotKind.PlasmaBolt ? new Color(0.35f, 0.6f, 1f) : new Color(1f, 0.8f, 0.35f);
            Spawn(position, direction, color, definition.Pellets > 1 ? 0.14f : 0.09f, 0.045f);
        }
        void OnImpact(Vector3 position, Vector3 normal, CombatImpactKind kind)
        {
            Color color = kind == CombatImpactKind.Shield ? new Color(0.25f, 0.8f, 1f) :
                kind == CombatImpactKind.Armor ? new Color(1f, 0.85f, 0.5f) :
                kind == CombatImpactKind.Flesh ? new Color(0.9f, 0.25f, 0.12f) : new Color(0.85f, 0.7f, 0.5f);
            Spawn(position + normal * 0.025f, normal, color, kind == CombatImpactKind.Shield ? 0.22f : 0.1f, 0.14f);
        }
        void Spawn(Vector3 position, Vector3 normal, Color color, float radius, float lifetime)
        {
            if (EffectMaterial == null) return;
            Effect effect = effects.Find(item => item.Until <= Time.time);
            if (effect == null && effects.Count < MaximumEffects)
            {
                GameObject root = new GameObject("Pooled combat spark");
                root.transform.SetParent(transform, false);
                LineRenderer line = root.AddComponent<LineRenderer>();
                line.sharedMaterial = EffectMaterial; line.useWorldSpace = true; line.positionCount = 5;
                line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; line.receiveShadows = false;
                effect = new Effect { Line = line }; effects.Add(effect);
            }
            if (effect == null) effect = effects[0];
            Vector3 right = Vector3.Cross(normal, Mathf.Abs(normal.y) > 0.9f ? Vector3.right : Vector3.up).normalized * radius;
            Vector3 up = Vector3.Cross(normal, right).normalized * radius;
            effect.Line.SetPosition(0, position + right); effect.Line.SetPosition(1, position - right);
            effect.Line.SetPosition(2, position); effect.Line.SetPosition(3, position + up); effect.Line.SetPosition(4, position - up);
            effect.Line.startWidth = effect.Line.endWidth = radius * 0.25f;
            effect.Color = color; effect.Lifetime = lifetime; effect.Until = Time.time + lifetime;
            effect.Line.startColor = effect.Line.endColor = color; effect.Line.enabled = true;
        }
        void Update()
        {
            foreach (Effect effect in effects)
            {
                effect.Line.enabled = Time.time < effect.Until;
                if (!effect.Line.enabled) continue;
                Color color = effect.Color; color.a = Mathf.Clamp01((effect.Until - Time.time) / effect.Lifetime);
                effect.Line.startColor = effect.Line.endColor = color;
            }
        }
    }
}
