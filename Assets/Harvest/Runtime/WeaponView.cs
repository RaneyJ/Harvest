using UnityEngine;

namespace Harvest
{
    // Optional visual representation. The loadout remains usable without a model.
    public sealed class WeaponView : MonoBehaviour
    {
        public MarineLoadout Loadout;
        public WeaponDefinition[] Definitions;
        public GameObject[] Models;

        void Start()
        {
            Loadout.Changed += Refresh;
            Refresh();
        }

        void OnDisable()
        {
            if (Loadout != null) Loadout.Changed -= Refresh;
        }

        void Refresh()
        {
            for (int i = 0; i < Models.Length; i++)
                if (Models[i] != null) Models[i].SetActive(Definitions != null && i < Definitions.Length && Definitions[i] == Loadout.Current);
        }
    }
}
