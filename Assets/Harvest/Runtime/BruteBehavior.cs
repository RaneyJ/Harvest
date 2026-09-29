using UnityEngine;

namespace Harvest
{
    public sealed class BruteBehavior : EnemyBehavior
    {
        public float ChargeTriggerRange = 14f;
        public float WindupSeconds = 0.85f;
        public float ChargeSeconds = 1.1f;
        public float ChargeSpeed = 8f;
        public float RecoverySeconds = 1.2f;
        public float MeleeRange = 2.9f;
        public float MeleeDamage = 42f;

        enum Phase { Approaching, Windup, Charging, Recovering }
        Phase phase;
        float phaseUntil;
        Vector3 chargeDirection;
        Renderer body;
        MaterialPropertyBlock tint;

        void Start()
        {
            body = GetComponentInChildren<Renderer>();
            tint = new MaterialPropertyBlock();
        }

        public override Vector3 DesiredMovement(Vector3 towardTarget, float distance)
        {
            if (phase == Phase.Charging) return chargeDirection * ChargeSpeed;
            if (phase == Phase.Windup || phase == Phase.Recovering) return Vector3.zero;
            return base.DesiredMovement(towardTarget, distance);
        }

        public override void TryAttack(CovenantEnemy self, MarineController target, float distance, bool hasLineOfSight)
        {
            if (phase == Phase.Windup || phase == Phase.Charging)
                SetTint(new Color(1f, 0.24f, 0.12f));
            if (phase == Phase.Approaching)
            {
                if (distance > ChargeTriggerRange)
                {
                    if (hasLineOfSight)
                    {
                        Vector3 muzzle = self.transform.position + Vector3.up * 1.6f;
                        self.GetComponent<ActorWeapon>()?.TryFire(muzzle, (target.View.transform.position - muzzle).normalized);
                    }
                    return;
                }
                if (!hasLineOfSight || Time.time < phaseUntil) return;
                phase = Phase.Windup;
                phaseUntil = Time.time + WindupSeconds;
                SetTint(new Color(1f, 0.24f, 0.12f));
            }
            else if (phase == Phase.Windup && Time.time >= phaseUntil)
            {
                chargeDirection = target.transform.position - transform.position;
                chargeDirection.y = 0f;
                chargeDirection.Normalize();
                phase = Phase.Charging;
                phaseUntil = Time.time + ChargeSeconds;
            }
            else if (phase == Phase.Charging)
            {
                if (distance < MeleeRange && hasLineOfSight)
                {
                    target.GetComponent<MarineArmor>()?.ApplyDamage(MeleeDamage);
                    Recover();
                }
                else if (Time.time >= phaseUntil) Recover();
            }
            else if (phase == Phase.Recovering && Time.time >= phaseUntil)
            {
                phase = Phase.Approaching;
                phaseUntil = Time.time + 1f;
            }
        }

        void Recover()
        {
            phase = Phase.Recovering;
            phaseUntil = Time.time + RecoverySeconds;
            SetTint(Color.white);
        }

        void SetTint(Color color)
        {
            if (body == null) return;
            if (color == Color.white) { body.SetPropertyBlock(null); return; }
            tint.SetColor("_Color", color);
            body.SetPropertyBlock(tint);
        }

        void OnGUI()
        {
            if ((phase != Phase.Windup && phase != Phase.Charging) || Camera.main == null) return;
            Vector3 point = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 3.5f);
            if (point.z > 0f) GUI.Box(new Rect(point.x - 48f, Screen.height - point.y, 96f, 24f), "CHARGE!");
        }
    }
}
