using UnityEngine;

namespace Harvest
{
    // Samples authored visual clips against existing weapon state. Never grants ammo or fires shots.
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(100)]
    public sealed class AuthoredWeaponAnimation : MonoBehaviour
    {
        public Animation Player;
        public AnimationClip Idle;
        public AnimationClip Fire;
        public AnimationClip Reload;
        MarineLoadout loadout;
        float firedAt = float.NegativeInfinity;
        bool prepared;
        public bool HasReloadPose => Player != null && Reload != null;

        void Awake() => Prepare();
        void OnEnable()
        {
            Prepare(); Bind(); firedAt = float.NegativeInfinity;
            if (Idle != null) Sample(Idle, 0);
        }
        void Prepare()
        {
            if (prepared) return;
            if (Player == null) Player = GetComponentInChildren<Animation>(true);
            if (Player == null) return;
            Player.playAutomatically = false;
            foreach (AnimationClip clip in new[] { Idle, Fire, Reload })
                if (clip != null) Player.AddClip(clip, clip.name);
            Player.Stop(); prepared = true;
        }
        void Bind()
        {
            if (loadout != null) return;
            loadout = GetComponentInParent<MarineLoadout>();
            if (loadout != null) loadout.ShotFired += OnShot;
        }
        void OnDisable()
        {
            if (loadout != null) loadout.ShotFired -= OnShot;
            loadout = null; firedAt = float.NegativeInfinity;
            if (Player != null) Player.Stop();
        }
        void OnShot() => firedAt = Time.time;
        void LateUpdate()
        {
            Prepare(); Bind();
            if (Player == null || loadout == null || loadout.Equipped == null) return;
            WeaponInstance weapon = loadout.Equipped;
            if (Reload != null && weapon.ReloadUntil > Time.time)
            {
                float progress = 1f - (weapon.ReloadUntil - Time.time) / Mathf.Max(.01f, weapon.Definition.ReloadSeconds);
                Sample(Reload, Mathf.Clamp01(progress) * Reload.length);
            }
            else if (Fire != null && Time.time - firedAt < Fire.length)
                Sample(Fire, Mathf.Max(0, Time.time - firedAt));
            else if (Idle != null) Sample(Idle, Mathf.Repeat(Time.time, Mathf.Max(.01f, Idle.length)));
        }
        void Sample(AnimationClip clip, float time)
        {
            if (Player == null || clip == null) return;
            Player.Stop();
            AnimationState state = Player[clip.name];
            if (state == null) return;
            state.enabled = true; state.weight = 1; state.speed = 0;
            state.wrapMode = WrapMode.ClampForever; state.time = time;
            Player.Sample();
            state.enabled = false;
        }
    }
}
