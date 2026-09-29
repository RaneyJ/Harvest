using UnityEngine;

namespace Harvest
{
    public sealed class GruntBehavior : PlasmaBehavior
    {
        public float PanicSeconds = 4f;
        public float WitnessRange = 22f;
        float panicUntil;

        public void WitnessAllyDeath(CovenantEnemy fallen)
        {
            if (fallen == null || fallen.GetComponent<BruteBehavior>() == null) return;
            if (Vector3.Distance(transform.position, fallen.transform.position) <= WitnessRange)
                panicUntil = Time.time + PanicSeconds;
        }

        public override Vector3 DesiredMovement(Vector3 towardTarget, float distance)
        {
            if (Time.time < panicUntil) return -towardTarget * (MoveSpeed * 1.5f);
            return base.DesiredMovement(towardTarget, distance);
        }

        public override void TryAttack(CovenantEnemy self, MarineController target, float distance, bool hasLineOfSight)
        {
            if (Time.time >= panicUntil) base.TryAttack(self, target, distance, hasLineOfSight);
        }

        void OnGUI()
        {
            if (Time.time >= panicUntil || Camera.main == null) return;
            Vector3 point = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 2.3f);
            if (point.z > 0f) GUI.Label(new Rect(point.x - 24f, Screen.height - point.y, 60f, 22f), "PANIC");
        }
    }
}
