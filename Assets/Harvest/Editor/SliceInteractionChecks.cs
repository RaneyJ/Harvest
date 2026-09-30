using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Harvest.Editor
{
    public static class SliceInteractionChecks
    {
        [MenuItem("Harvest/Run Slice Interaction Checks (Play Mode)")]
        public static void Run()
        {
            if (!Application.isPlaying) { Debug.Log("Enter Play mode before running slice checks."); return; }
            GameObject root = new GameObject("Temporary slice checks");
            Vector3 origin = new Vector3(4000f, 0f, 4000f);
            try
            {
                GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
                floor.transform.SetParent(root.transform); floor.transform.position = origin + Vector3.up * 3.1f;
                floor.transform.localScale = new Vector3(10f, 0.2f, 10f);
                GameObject actor = new GameObject("Drop owner"); actor.transform.SetParent(root.transform);
                actor.transform.position = origin + Vector3.up * 4.2f;
                GameObject pickup = new GameObject("Pickup"); pickup.transform.SetParent(root.transform);
                pickup.transform.position = origin + new Vector3(2f, 3.45f, 0f);
                pickup.AddComponent<SphereCollider>().isTrigger = true;
                DroppedWeapon dropped = pickup.AddComponent<DroppedWeapon>();
                Physics.SyncTransforms();
                Vector3 placed = DroppedWeapon.SurfacePosition(actor.transform.position + Vector3.right * 2f, actor.transform);
                if (Mathf.Abs(placed.y - 3.45f) > 0.01f) throw new Exception("Weapon must remain on the upper floor.");
                Vector3 eye = actor.transform.position + Vector3.up * 0.4f;
                if (!MarineLoadout.CanReachPickup(actor.transform, eye, dropped)) throw new Exception("Visible pickup must be reachable.");
                GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                wall.transform.SetParent(root.transform); wall.transform.position = origin + new Vector3(1f, 4.2f, 0f);
                wall.transform.localScale = new Vector3(0.2f, 2f, 3f);
                Physics.SyncTransforms();
                if (MarineLoadout.CanReachPickup(actor.transform, eye, dropped)) throw new Exception("Wall must block pickup.");
                placed = DroppedWeapon.SurfacePosition(actor.transform.position + Vector3.right * 2f, actor.transform);
                if (placed.x > origin.x + 0.7f || Mathf.Abs(placed.y - 3.45f) > 0.01f)
                    throw new Exception("Swap drop must stop before the wall and stay on its floor.");
                wall.SetActive(false); pickup.transform.position = origin + new Vector3(2f, 0.25f, 0f);
                Physics.SyncTransforms();
                if (MarineLoadout.CanReachPickup(actor.transform, eye, dropped)) throw new Exception("Floor must block downstairs pickup.");
                Debug.Log("Slice checks passed: upper-floor drop placement, visible pickup, wall/floor pickup occlusion, and wall-safe swap placement.");
            }
            catch (Exception error) { Debug.LogException(error); }
            finally { Object.Destroy(root); }
        }
    }
}
