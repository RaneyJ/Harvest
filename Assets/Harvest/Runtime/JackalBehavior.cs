using UnityEngine;

namespace Harvest
{
    public sealed class JackalBehavior : PlasmaBehavior
    {
        [Range(0f, 1f)] public float FrontArcDot = 0.35f;
        public GameObject ShieldVisual;
        Vitality vitality;

        void Start()
        {
            vitality = GetComponent<Vitality>();
            vitality.Changed += UpdateShieldVisual;
            UpdateShieldVisual();
        }

        void OnDestroy()
        {
            if (vitality != null) vitality.Changed -= UpdateShieldVisual;
        }

        void UpdateShieldVisual()
        {
            if (ShieldVisual != null) ShieldVisual.SetActive(vitality.Shield > 0f);
        }

        public bool IsFlanked(Vector3 shotOrigin)
        {
            if (vitality == null) vitality = GetComponent<Vitality>();
            if (vitality.Shield <= 0f) return false;
            Vector3 towardShooter = shotOrigin - transform.position;
            towardShooter.y = 0f;
            return Vector3.Dot(transform.forward, towardShooter.normalized) < FrontArcDot;
        }
    }
}
