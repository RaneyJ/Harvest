using System.Collections.Generic;
using UnityEngine;

namespace Harvest
{
    public sealed class PlasmaBolt : MonoBehaviour
    {
        public float Lifetime = 4f;
        Vector3 velocity;
        float damage;
        float shieldMultiplier;
        CombatTeam team;
        bool breaksShield;
        float expires;
        readonly Dictionary<Suppression, float> exposures = new Dictionary<Suppression, float>();

        public void Initialize(Vector3 shotVelocity, CombatTeam sourceTeam, float shotDamage, float shieldDamageMultiplier, bool shotBreaksShield = false)
        {
            exposures.Clear();
            breaksShield = shotBreaksShield;
            velocity = shotVelocity;
            team = sourceTeam;
            damage = shotDamage;
            shieldMultiplier = shieldDamageMultiplier;
            expires = Time.time + Lifetime;
        }

        void Update()
        {
            if (Time.time > expires) { Destroy(gameObject); return; }
            Vector3 start = transform.position;
            Vector3 step = velocity * Time.deltaTime;
            if (step.sqrMagnitude <= 0f) return;
            if (Physics.Raycast(start, step.normalized, out RaycastHit hit, step.magnitude, ~0, QueryTriggerInteraction.Ignore))
            {
                Suppression.ObserveSegment(start, hit.point, team, exposures, true);
                WeaponRuntime.PresentImpact(hit, start);
                WeaponRuntime.ApplyHit(hit.collider, team, damage, shieldMultiplier, start, breaksShield);
                Destroy(gameObject);
                return;
            }
            Suppression.ObserveSegment(start, start + step, team, exposures, true);
            transform.position = start + step;
        }
    }
}
