using System;
using UnityEngine;

namespace Harvest
{
    [Serializable]
    public sealed class FarmhouseDressingCluster
    {
        public bool Enabled = true;
        [Tooltip("Added to this area's default anchor in house space. Rebuild to apply.")]
        public Vector3 Offset;
        public float YawOffset;
        [Tooltip("Optional artist-owned prefab. Keep its origin at floor level. Its colliders are moved into the farmhouse Collision group during assembly.")]
        public GameObject AuthoredPrefab;
    }
    [CreateAssetMenu(menuName = "Harvest/Environment/Farmhouse Dressing Layout")]
    public sealed class FarmhouseDressingLayout : ScriptableObject
    {
        public FarmhouseDressingCluster Kitchen = new FarmhouseDressingCluster();
        public FarmhouseDressingCluster Dining = new FarmhouseDressingCluster();
        public FarmhouseDressingCluster FarmStorage = new FarmhouseDressingCluster();
        public FarmhouseDressingCluster Sleeping = new FarmhouseDressingCluster();
        public FarmhouseDressingCluster UtilityRoom = new FarmhouseDressingCluster();
        public FarmhouseDressingCluster EntryDetails = new FarmhouseDressingCluster();
        public FarmhouseDressingCluster BedroomDetails = new FarmhouseDressingCluster();
        public FarmhouseDressingCluster OfficeDetails = new FarmhouseDressingCluster();
        public FarmhouseDressingCluster FiringPosition = new FarmhouseDressingCluster();
        public FarmhouseDressingCluster WorkDesk = new FarmhouseDressingCluster();
    }
}
