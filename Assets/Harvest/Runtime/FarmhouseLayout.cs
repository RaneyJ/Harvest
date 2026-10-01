using UnityEngine;

namespace Harvest
{
    // Authoring dimensions only. Combat, navigation and player movement own their own behavior.
    [CreateAssetMenu(menuName = "Harvest/Environment/Farmhouse Layout")]
    public sealed class FarmhouseLayout : ScriptableObject
    {
        [Tooltip("Optional artist-owned prefab or variant. Store it outside the generated prefab path; rebuilds will instantiate it without rewriting it.")]
        public GameObject AuthoredPrefab;
        public Vector3 WorldOrigin = new Vector3(-19f, 0f, -2f);
        [Min(5.5f)] public float HalfWidth = 6f;
        [Min(6.5f)] public float HalfDepth = 7f;
        [Min(3f)] public float StoreyHeight = 3.2f;
        [Range(10f, 30f)] public float RoofPitch = 14f;
        [Range(0.2f, 0.4f)] public float WallThickness = 0.24f;
        [Range(0.25f, 0.75f)] public float RoofOverhang = 0.45f;
        [Min(2f)] public float StairWidth = 2.2f;
        [Min(0.35f)] public float TreadDepth = 0.4f;
        [Range(16, 20)] public int StairCount = 16;
        [Min(2.4f)] public float PorchDepth = 2.6f;
        public float GroundFloorTop => 0.12f;
        public float UpperFloorTop => StoreyHeight;
        public float EavesHeight => StoreyHeight * 2f;
        public float RidgeHeight => EavesHeight + HalfWidth * Mathf.Tan(RoofPitch * Mathf.Deg2Rad);
        public float StairX => -HalfWidth + StairWidth * 0.5f + 0.6f;
        public float StairStart => -HalfDepth + 2.1f;
        public float StairEnd => StairStart + StairCount * TreadDepth;
        public float StairwellRight => StairX + StairWidth * 0.5f + 0.6f;
        public float Riser => (UpperFloorTop - GroundFloorTop) / StairCount;
        public float TreadTop(int index) => GroundFloorTop + (index + 1) * Riser;
        public Vector3 StairPoint(int index) => new Vector3(StairX, TreadTop(index), StairStart + (index + 0.5f) * TreadDepth);
    }
}
