using System.Collections.Generic;
using UnityEngine;

namespace Harvest
{
    // Scene audio consumes presentation events. Clip selection belongs to weapon definitions.
    public sealed class CombatAudio : MonoBehaviour
    {
        public MarineLoadout Player;
        public AudioClip Wind;
        public AudioClip[] PlasmaWorldImpacts = new AudioClip[0];
        [Range(0f, 1f)] public float PlasmaImpactVolume = 0.22f;
        int nextPlasmaImpact;
        [Range(0f, 1f)] public float WindVolume = 0.12f;
        [Min(4)] public int MaximumVoices = 32;
        readonly List<AudioSource> voices = new List<AudioSource>();
        AudioSource ambience;
        void OnEnable()
        {
            nextPlasmaImpact = 0;
            WeaponRuntime.PlasmaWorldImpactPresented += OnPlasmaWorldImpact;
            GrenadeProjectile.ExplosionPresented += OnExplosion;
            WeaponRuntime.ShotPresented += OnShot;
            WeaponRuntime.ReloadPresented += OnReload;
            if (ambience == null) ambience = gameObject.AddComponent<AudioSource>();
            ambience.playOnAwake = false; ambience.spatialBlend = 0f; ambience.loop = true;
            ambience.clip = Wind; ambience.volume = WindVolume;
            if (Wind != null) ambience.Play();
        }
        void OnDisable()
        {
            WeaponRuntime.PlasmaWorldImpactPresented -= OnPlasmaWorldImpact;
            GrenadeProjectile.ExplosionPresented -= OnExplosion;
            WeaponRuntime.ShotPresented -= OnShot;
            WeaponRuntime.ReloadPresented -= OnReload;
            if (ambience != null) ambience.Stop();
            foreach (AudioSource voice in voices) if (voice != null) voice.Stop();
        }
        void OnShot(WeaponDefinition definition, Vector3 origin, Vector3 direction, CombatTeam team) =>
            Play(definition.FireSound, definition.FireVolume, origin);
        void OnReload(WeaponDefinition definition, Vector3 origin) => Play(definition.ReloadSound, definition.ReloadVolume, origin);
        void OnExplosion(GrenadeDefinition definition, Vector3 origin) => Play(definition.ExplosionSound, definition.ExplosionVolume, origin, true);
        void OnPlasmaWorldImpact(Vector3 origin)
        {
            if (PlasmaWorldImpacts == null || PlasmaWorldImpacts.Length == 0) return;
            AudioClip clip = PlasmaWorldImpacts[nextPlasmaImpact % PlasmaWorldImpacts.Length];
            nextPlasmaImpact = (nextPlasmaImpact + 1) % PlasmaWorldImpacts.Length;
            Play(clip, PlasmaImpactVolume, origin, true, true);
        }
        void Play(AudioClip clip, float volume, Vector3 origin, bool positional = false, bool quietImpact = false)
        {
            if (clip == null || volume <= 0f) return;
            bool local = !positional && Player != null && Player.View != null && (Player.View.transform.position - origin).sqrMagnitude < 0.04f;
            AudioSource voice = voices.Find(source => !source.isPlaying);
            if (voice == null && voices.Count < Mathf.Max(4, MaximumVoices))
            {
                GameObject root = new GameObject("Pooled combat audio"); root.transform.SetParent(transform, false);
                voice = root.AddComponent<AudioSource>(); voice.playOnAwake = false; voice.loop = false;
                voice.dopplerLevel = 0f; voice.rolloffMode = AudioRolloffMode.Logarithmic;
                voice.minDistance = 5f; voice.maxDistance = 100f;
                voices.Add(voice);
            }
            if (voice == null)
            {
                // Quiet impacts never displace weapon fire or explosions when the pool is full.
                if (quietImpact) return;
                voice = voices.Find(source => source.priority >= 192);
                // Keep local weapon feedback audible; distant voices may be replaced first.
                if (voice == null) voice = voices.Find(source => source.spatialBlend > 0f);
                if (voice == null && !local) return;
                if (voice == null) voice = voices[0];
            }
            voice.Stop(); voice.transform.position = origin;
            voice.spatialBlend = local ? 0f : 1f; voice.priority = quietImpact ? 192 : local ? 64 : 128;
            voice.minDistance = quietImpact ? 1.5f : 5f; voice.maxDistance = quietImpact ? 35f : 100f;
            voice.clip = clip; voice.volume = volume; voice.pitch = 1f; voice.Play();
        }
    }
}
