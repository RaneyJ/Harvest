using UnityEngine;

namespace Harvest
{
    // Presentation listens to gameplay events; combat never calls into the HUD.
    public sealed class HarvestHud : MonoBehaviour
    {
        public MarineController Marine;
        public MarineArmor Armor;
        public MarineLoadout Loadout;
        public HarvestEncounter Encounter;

        Suppression suppression;
        PrecisionAim aim;
        GrenadeInventory grenades;
        MarineCombatActions actions;
        string status;
        string callSign;
        string weapon;
        string ammo;
        int hostiles;
        int marinesLeft;
        int health;
        int armor;
        bool evacuating;
        bool finished;
        float hitMarkerUntil;
        float damageFlashUntil;
        bool healthWasHit;
        float pickupUntil;
        float armorBrokenUntil;
        int pickupAmount;

        void Start()
        {
            suppression = Marine.GetComponent<Suppression>();
            aim = Marine.GetComponent<PrecisionAim>();
            grenades = Marine.GetComponent<GrenadeInventory>();
            actions = Marine.GetComponent<MarineCombatActions>();
            if (actions != null) actions.HitEnemy += MarkHit;
            if (Armor == null)
            {
                Debug.LogError("The Line HUD needs the armor update. Stop Play mode and choose Harvest > Build The Line Prototype.", this);
                enabled = false;
                return;
            }
            Encounter.StateChanged += Refresh;
            Marine.GetComponent<Vitality>().Changed += Refresh;
            Armor.Changed += Refresh;
            Armor.Hit += OnMarineHit;
            Armor.ArmorBroken += OnArmorBroken;
            Armor.ArmorRestored += OnArmorRestored;
            Loadout.Changed += Refresh;
            Loadout.HitEnemy += MarkHit;
            Refresh();
        }

        void OnDisable()
        {
            if (Encounter == null || Marine == null || Loadout == null) return;
            Encounter.StateChanged -= Refresh;
            Marine.GetComponent<Vitality>().Changed -= Refresh;
            if (Armor != null)
            {
                Armor.Changed -= Refresh;
                Armor.Hit -= OnMarineHit;
                Armor.ArmorBroken -= OnArmorBroken;
                Armor.ArmorRestored -= OnArmorRestored;
            }
            Loadout.Changed -= Refresh;
            Loadout.HitEnemy -= MarkHit;
            if (actions != null) actions.HitEnemy -= MarkHit;
        }

        void MarkHit() => hitMarkerUntil = Time.time + 0.12f;
        void OnArmorBroken() => armorBrokenUntil = Time.time + 2f;

        void OnMarineHit(bool healthExposed)
        {
            healthWasHit = healthExposed;
            damageFlashUntil = Time.time + 0.25f;
        }

        void OnArmorRestored(float amount)
        {
            pickupAmount = Mathf.CeilToInt(amount);
            pickupUntil = Time.time + 1.8f;
        }

        void Refresh()
        {
            status = Encounter.Status;
            callSign = Marine.CallSign;
            hostiles = Encounter.Hostiles;
            marinesLeft = Encounter.MarinesLeft;
            evacuating = Encounter.IsEvacuating;
            finished = Encounter.IsFinished;
            health = Mathf.CeilToInt(Marine.Vitality.Health);
            armor = Mathf.CeilToInt(Armor.Armor);
            weapon = Loadout.Current == null ? "NONE" : Loadout.Current.DisplayName;
            ammo = Loadout.AmmoText;
        }

        void OnGUI()
        {
            if (Time.time < damageFlashUntil)
            {
                GUI.color = healthWasHit ? new Color(0.65f, 0.08f, 0.06f, 0.3f) : new Color(0.2f, 0.62f, 0.45f, 0.2f);
                GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            }
            GUI.color = Color.white;
            GUI.Box(new Rect(16, 16, 390, 106), $"{status}\n{callSign}   |   MARINES LEFT: {marinesLeft}\nHOSTILES: {hostiles}   |   SQUAD: {Encounter.AlliesAlive}");
            GUI.Box(new Rect(16, Screen.height - 116, 245, 94), $"ARMOR  {armor} / {Mathf.CeilToInt(Armor.MaxArmor)}\nHEALTH  {health}\n{weapon.ToUpperInvariant()}  {ammo}");
            if (grenades != null)
                GUI.Box(new Rect(16, Screen.height - 162, 245, 42),
                    $"FRAG {grenades.Count(GrenadeKind.Frag)}   |   PLASMA {grenades.Count(GrenadeKind.Plasma)}\n[G] {grenades.Selected.ToString().ToUpperInvariant()}   [Q] SWITCH");
            if (Time.time < pickupUntil)
                GUI.Box(new Rect(270, Screen.height - 64, 136, 40), $"ARMOR +{pickupAmount}");
            if (Time.time < armorBrokenUntil)
                GUI.Box(new Rect(Screen.width * 0.5f - 100f, Screen.height - 104f, 200f, 36f), "ARMOR BREACHED");
            if (!finished && Marine.Vitality.IsAlive)
            {
                float x = Screen.width * 0.5f;
                float y = Screen.height * 0.5f;
                GUI.color = Time.time < hitMarkerUntil ? Color.red : Color.white;
                if (aim == null || !aim.IsAiming) GUI.Label(new Rect(x - 8f, y - 12f, 30f, 30f), "+");
                else if (Time.time < hitMarkerUntil) GUI.Label(new Rect(x - 8f, y - 12f, 30f, 30f), "×");
                if (Loadout.Current != null && Loadout.Current.IsPrecision)
                {
                    GUI.color = Color.white;
                    GUI.Label(new Rect(16f, Screen.height - 22f, 245f, 22f), "[RIGHT MOUSE] AIM — BOLT ACTION");
                }
                if (Loadout.IsCharging)
                {
                    GUI.color = Color.white;
                    string charge = !Loadout.Equipped.CanCharge ? "LOW BATTERY — NORMAL SHOT" :
                        Loadout.ChargeFraction >= 1f ? "CHARGED — RELEASE TO FIRE" :
                        $"CHARGING {Mathf.FloorToInt(Loadout.ChargeFraction * 100f)}%";
                    GUI.Box(new Rect(x - 130f, y + 30f, 260f, 28f), charge);
                }
            }
            GUI.color = Color.white;
            if (suppression != null && suppression.EffectStrength > 0.01f && Marine.Vitality.IsAlive)
                GUI.Box(new Rect(Screen.width - 200f, Screen.height - 64f, 184f, 42f),
                    $"SUPPRESSED {Mathf.CeilToInt(suppression.EffectStrength * 100f)}%");
            if (evacuating && !finished)
                GUI.Box(new Rect(Screen.width - 260, 16, 244, 45), "EVAC PAD: GREEN BEACON BEHIND LINE");
        }
    }
}
