using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Harvest
{
    // Builds the generated graybox's static box geometry once before agents initialize.
    // Replace this surface with a baked map later; marine behavior only depends on NavMeshAgent.
    [DefaultExecutionOrder(-200)]
    public sealed class BattlefieldNavigation : MonoBehaviour
    {
        public Vector3 BoundsCenter = new Vector3(0f, 5f, 10f);
        public Vector3 BoundsSize = new Vector3(130f, 30f, 150f);
        NavMeshData data;
        NavMeshDataInstance instance;

        void Awake()
        {
            if (NavMesh.GetSettingsCount() == 0)
            {
                Debug.LogError("No navigation agent settings exist for the marine squad.", this);
                return;
            }
            var sources = new List<NavMeshBuildSource>();
            foreach (BoxCollider box in FindObjectsByType<BoxCollider>(FindObjectsSortMode.None))
            {
                if (!box.enabled || box.isTrigger || box.GetComponentInParent<CombatTarget>() != null) continue;
                sources.Add(new NavMeshBuildSource
                {
                    shape = NavMeshBuildSourceShape.Box,
                    transform = Matrix4x4.TRS(box.transform.TransformPoint(box.center),
                        box.transform.rotation, box.transform.lossyScale),
                    size = box.size,
                    area = 0
                });
            }
            foreach (MeshCollider mesh in FindObjectsByType<MeshCollider>(FindObjectsSortMode.None))
            {
                if (!mesh.enabled || mesh.isTrigger || mesh.sharedMesh == null ||
                    mesh.GetComponentInParent<CombatTarget>() != null) continue;
                sources.Add(new NavMeshBuildSource
                {
                    shape = NavMeshBuildSourceShape.Mesh,
                    transform = mesh.transform.localToWorldMatrix,
                    sourceObject = mesh.sharedMesh,
                    area = 0
                });
            }
            NavMeshBuildSettings settings = NavMesh.GetSettingsByIndex(0);
            settings.agentRadius = 0.4f;
            settings.agentHeight = 1.9f;
            settings.agentClimb = 0.3f;
            data = NavMeshBuilder.BuildNavMeshData(settings, sources,
                new Bounds(BoundsCenter, BoundsSize), Vector3.zero, Quaternion.identity);
            if (data == null) { Debug.LogError("Marine navigation could not be built.", this); return; }
            instance = NavMesh.AddNavMeshData(data);
        }

        void OnDestroy()
        {
            if (instance.valid) instance.Remove();
            if (data != null) Destroy(data);
        }
    }
}
