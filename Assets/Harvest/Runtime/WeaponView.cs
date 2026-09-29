using UnityEngine;

namespace Harvest
{
    // Optional visual representation. The loadout remains usable without a model.
    public sealed class WeaponView : MonoBehaviour
    {
        public MarineLoadout Loadout;
        public WeaponDefinition[] Definitions;
        public GameObject[] Models;
        public MeleeAttack Melee;
        Vector3[] positions;
        float swingStarted = float.NegativeInfinity;

        void Start()
        {
            positions = new Vector3[Models.Length];
            for (int i = 0; i < Models.Length; i++)
                if (Models[i] != null) positions[i] = Models[i].transform.localPosition;
            if (Melee != null) Melee.Swing += BeginSwing;
            Loadout.Changed += Refresh;
            Refresh();
        }

        void OnDisable()
        {
            if (Loadout != null) Loadout.Changed -= Refresh;
            if (Melee != null) Melee.Swing -= BeginSwing;
            if (positions != null)
                for (int i = 0; i < Models.Length; i++)
                    if (Models[i] != null) Models[i].transform.localPosition = positions[i];
        }

        void BeginSwing() => swingStarted = Time.time;
        void LateUpdate()
        {
            if (positions == null) return;
            float t = Mathf.Clamp01((Time.time - swingStarted) / 0.3f);
            float thrust = Mathf.Sin(t * Mathf.PI) * 0.18f;
            for (int i = 0; i < Models.Length; i++)
                if (Models[i] != null) Models[i].transform.localPosition = positions[i] +
                    Vector3.forward * (Models[i].activeSelf ? thrust : 0f);
        }
        void Refresh()
        {
            for (int i = 0; i < Models.Length; i++)
                if (Models[i] != null) Models[i].SetActive(Definitions != null && i < Definitions.Length && Definitions[i] == Loadout.Current);
        }
    }
}
