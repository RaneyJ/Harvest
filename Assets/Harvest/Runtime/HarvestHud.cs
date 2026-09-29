using UnityEngine;

namespace Harvest
{
    // Presentation listens to gameplay events; combat never calls into the HUD.
    public sealed class HarvestHud : MonoBehaviour
    {
        public MarineController Marine;
        public MarineLoadout Loadout;
        public HarvestEncounter Encounter;

        string status;
        string callSign;
        string weapon;
        string ammo;
        int hostiles;
        int marinesLeft;
        int health;
        bool evacuating;
        bool finished;
        float hitMarkerUntil;

        void Start()
        {
            Encounter.StateChanged += Refresh;
            Marine.GetComponent<Vitality>().Changed += Refresh;
            Loadout.Changed += Refresh;
            Loadout.HitEnemy += MarkHit;
            Refresh();
        }

        void OnDisable()
        {
            if (Encounter == null || Marine == null || Loadout == null) return;
            Encounter.StateChanged -= Refresh;
            Marine.GetComponent<Vitality>().Changed -= Refresh;
            Loadout.Changed -= Refresh;
            Loadout.HitEnemy -= MarkHit;
        }

        void MarkHit() => hitMarkerUntil = Time.time + 0.12f;

        void Refresh()
        {
            status = Encounter.Status;
            callSign = Marine.CallSign;
            hostiles = Encounter.Hostiles;
            marinesLeft = Encounter.MarinesLeft;
            evacuating = Encounter.IsEvacuating;
            finished = Encounter.IsFinished;
            health = Mathf.CeilToInt(Marine.Vitality.Health);
            weapon = Loadout.Current == null ? "NONE" : Loadout.Current.DisplayName;
            ammo = Loadout.AmmoText;
        }

        void OnGUI()
        {
            GUI.color = Color.white;
            GUI.Box(new Rect(16, 16, 390, 86), $"{status}\n{callSign}   |   MARINES LEFT: {marinesLeft}\nHOSTILES: {hostiles}");
            GUI.Box(new Rect(16, Screen.height - 94, 245, 72), $"HEALTH  {health}\n{weapon.ToUpperInvariant()}  {ammo}");
            if (!finished && Marine.Vitality.IsAlive)
            {
                float x = Screen.width * 0.5f;
                float y = Screen.height * 0.5f;
                GUI.color = Time.time < hitMarkerUntil ? Color.red : Color.white;
                GUI.Label(new Rect(x - 8f, y - 12f, 30f, 30f), "+");
            }
            GUI.color = Color.white;
            if (evacuating && !finished)
                GUI.Box(new Rect(Screen.width - 260, 16, 244, 45), "EVAC PAD: GREEN BEACON BEHIND LINE");
        }
    }
}
