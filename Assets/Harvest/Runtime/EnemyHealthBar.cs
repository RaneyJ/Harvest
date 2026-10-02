using UnityEngine;

namespace Harvest
{
    [RequireComponent(typeof(Vitality))]
    public sealed class EnemyHealthBar : MonoBehaviour
    {
        public float Height = 2.1f;
        Vitality vitality;
        float healthFraction;
        float shieldFraction;

        void Awake() => vitality = GetComponent<Vitality>();
        void OnEnable()
        {
            if (vitality == null) vitality = GetComponent<Vitality>();
            vitality.Changed += Refresh;
            Refresh();
        }
        void OnDisable()
        {
            if (vitality != null) vitality.Changed -= Refresh;
        }

        void Refresh()
        {
            healthFraction = vitality.MaxHealth > 0f ? vitality.Health / vitality.MaxHealth : 0f;
            shieldFraction = vitality.MaxShield > 0f ? vitality.Shield / vitality.MaxShield : 0f;
        }

        void OnGUI()
        {
            Camera camera = Camera.main;
            if (camera == null || Vector3.Distance(camera.transform.position, transform.position) > 35f) return;
            Vector3 point = camera.WorldToScreenPoint(transform.position + Vector3.up * Height);
            if (point.z <= 0f) return;
            Rect bar = new Rect(point.x - 25f, Screen.height - point.y, 50f, 4f);
            GUI.color = Color.black;
            GUI.DrawTexture(bar, Texture2D.whiteTexture);
            GUI.color = shieldFraction > 0f ? new Color(0.35f, 0.75f, 1f) : new Color(1f, 0.45f, 0.32f);
            GUI.DrawTexture(new Rect(bar.x, bar.y, 50f * (shieldFraction > 0f ? shieldFraction : healthFraction), 4f), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }
    }
}
