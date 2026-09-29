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
        Quaternion[] rotations;
        CharacterController controller;
        WeaponDefinition displayed;
        float modelKick;
        float rotationKick;
        PrecisionAim aim;
        float shotStarted = float.NegativeInfinity;
        float swingStarted = float.NegativeInfinity;

        void Start()
        {
            aim = Loadout.GetComponent<PrecisionAim>();
            Loadout.ShotFired += BeginBoltCycle;
            controller = Loadout.GetComponent<CharacterController>();
            rotations = new Quaternion[Models.Length];
            positions = new Vector3[Models.Length];
            for (int i = 0; i < Models.Length; i++)
                if (Models[i] != null) { positions[i] = Models[i].transform.localPosition; rotations[i] = Models[i].transform.localRotation; }
            if (Melee != null) Melee.Swing += BeginSwing;
            Loadout.Changed += Refresh;
            Refresh();
        }

        void OnDisable()
        {
            if (Loadout != null) { Loadout.Changed -= Refresh; Loadout.ShotFired -= BeginBoltCycle; }
            if (Melee != null) Melee.Swing -= BeginSwing;
            if (positions != null)
                for (int i = 0; i < Models.Length; i++)
                    if (Models[i] != null) { Models[i].transform.localPosition = positions[i]; Models[i].transform.localRotation = rotations[i]; }
        }

        void BeginBoltCycle()
        {
            WeaponDefinition definition = Loadout.Current;
            if (definition == null) return;
            if (definition.IsPrecision) shotStarted = Time.time;
            modelKick = Mathf.Min(0.12f, modelKick + definition.ModelKickDistance);
            rotationKick = Mathf.Min(12f, rotationKick + definition.ModelKickDegrees);
        }
        void BeginSwing() => swingStarted = Time.time;
        void LateUpdate()
        {
            if (positions == null) return;
            float decay = Mathf.Exp(-18f * Time.deltaTime);
            modelKick *= decay; rotationKick *= decay;
            float movement = controller != null ? Mathf.Clamp01(new Vector3(controller.velocity.x, 0f, controller.velocity.z).magnitude / 5.5f) : 0f;
            float steadiness = aim != null ? 1f - aim.Blend * 0.9f : 1f;
            float bob = Mathf.Sin(Time.time * 9f) * 0.008f * movement * steadiness;
            float sway = Cursor.lockState == CursorLockMode.Locked ? Mathf.Clamp(Input.GetAxisRaw("Mouse X") * -0.003f, -0.012f, 0.012f) * steadiness : 0f;
            float reload = Loadout.Equipped != null && Loadout.Equipped.ReloadUntil > Time.time ?
                Mathf.Sin(Mathf.PI * Mathf.Clamp01(1f - (Loadout.Equipped.ReloadUntil - Time.time) / Mathf.Max(0.01f, Loadout.Current.ReloadSeconds))) : 0f;
            float t = Mathf.Clamp01((Time.time - swingStarted) / 0.3f);
            float thrust = Mathf.Sin(t * Mathf.PI) * 0.18f;
            for (int i = 0; i < Models.Length; i++)
            {
                if (Models[i] == null) continue;
                Vector3 rest = positions[i];
                if (aim != null && Loadout.Current != null && Loadout.Current.IsPrecision && Models[i].activeSelf)
                    rest = Vector3.Lerp(rest, Loadout.Current.AdsModelPosition, aim.Blend);
                bool active = Models[i].activeSelf;
                Models[i].transform.localPosition = rest + (active ? new Vector3(sway, bob - reload * 0.18f, thrust - modelKick) : Vector3.zero);
                Models[i].transform.localRotation = rotations[i] * Quaternion.Euler(active ? -rotationKick + reload * 22f : 0f, 0f, active ? -reload * 12f : 0f);
                Transform bolt = Models[i].transform.Find("Bolt");
                if (bolt != null)
                {
                    float cycle = Mathf.Clamp01((Time.time - shotStarted - 0.15f) / 0.95f);
                    bolt.localPosition = new Vector3(0.065f, 0.025f, -Mathf.Sin(cycle * Mathf.PI) * 0.09f);
                }
            }
        }
        void Refresh()
        {
            if (displayed != Loadout.Current)
            {
                displayed = Loadout.Current;
                modelKick = rotationKick = 0f;
                shotStarted = float.NegativeInfinity;
            }
            for (int i = 0; i < Models.Length; i++)
                if (Models[i] != null) Models[i].SetActive(Definitions != null && i < Definitions.Length && Definitions[i] == Loadout.Current);
        }
    }
}
