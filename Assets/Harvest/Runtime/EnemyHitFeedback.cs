using UnityEngine;

namespace Harvest
{
    [RequireComponent(typeof(Vitality))]
    public sealed class EnemyHitFeedback : MonoBehaviour
    {
        public float LabelHeight = 2.3f;
        public float FlashSeconds = 0.15f;
        Vitality vitality;
        Renderer body;
        MaterialPropertyBlock block;
        Color flashColor;
        float flashUntil;
        float breakLabelUntil;
        bool flashing;

        void Awake()
        {
            vitality = GetComponent<Vitality>();
            body = GetComponentInChildren<Renderer>();
            block = new MaterialPropertyBlock();
        }

        void OnEnable() => vitality.Damaged += OnDamage;
        void OnDisable()
        {
            if (vitality != null) vitality.Damaged -= OnDamage;
            if (body != null) body.SetPropertyBlock(null);
        }

        void OnDamage(bool shieldHit, bool shieldBroken)
        {
            flashColor = shieldHit ? new Color(0.25f, 0.8f, 1f) : new Color(1f, 0.55f, 0.3f);
            flashUntil = Time.time + FlashSeconds;
            if (shieldBroken) breakLabelUntil = Time.time + 0.8f;
        }

        void LateUpdate()
        {
            if (body == null) return;
            if (Time.time < flashUntil)
            {
                block.SetColor("_BaseColor", flashColor);
                body.SetPropertyBlock(block);
                flashing = true;
            }
            else if (flashing)
            {
                body.SetPropertyBlock(null);
                flashing = false;
            }
        }

        void OnGUI()
        {
            if (Time.time >= breakLabelUntil || Camera.main == null) return;
            Vector3 point = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * LabelHeight);
            if (point.z > 0f)
                GUI.Box(new Rect(point.x - 66f, Screen.height - point.y, 132f, 24f), "SHIELD BROKEN");
        }
    }
}
