using UnityEngine;

namespace Harvest
{
    [CreateAssetMenu(menuName="Harvest/Environment/Farmyard Art Profile")]
    public sealed class FarmyardArtProfile : ScriptableObject
    {
        public bool EnableTracks=true;
        public bool EnableBoundaryVegetation=true;
        public bool EnableYardSupplies=true;
        [Header("Working spaces and freight construction")]
        public bool EnableShedWorkArea=true;
        public bool EnableGrainStation=true;
        public bool EnableFreightDetails=true;
        public bool EnablePorchStaging=true;
        public bool EnableFenceDetails=true;
        [Header("Artist visual overrides")]
        [Tooltip("Static visual prefab in metres, with a ground-level origin. No colliders or scripts; yard collision is owned separately.")]
        public GameObject SupplyPrefab;
        [Tooltip("A small ground-cover tuft with its origin at soil level. No colliders or scripts.")]
        public GameObject VegetationPrefab;
        [Tooltip("Optional small stone/ground-debris cluster, ground-level origin. No colliders or scripts.")]
        public GameObject GroundDebrisPrefab;
    }
}
