using UnityEngine;

namespace Harvest
{
    // Identifies the authored assembly without coupling its visuals to encounter logic.
    public sealed class FarmhouseFoundation : MonoBehaviour
    {
        public FarmhouseLayout Layout;
        void OnDrawGizmosSelected()
        {
            if (Layout == null) return;
            Matrix4x4 previous = Gizmos.matrix;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.7f);
            Gizmos.DrawWireCube(new Vector3(0f, Layout.StoreyHeight, 0f),
                new Vector3(Layout.HalfWidth * 2f, Layout.StoreyHeight * 2f, Layout.HalfDepth * 2f));
            Gizmos.color = new Color(1f, 0.7f, 0.2f, 0.7f);
            for (int i = 0; i < Layout.StairCount; i++)
                Gizmos.DrawWireCube(Layout.StairPoint(i) + Vector3.up * 0.95f, new Vector3(0.8f, 1.9f, 0.3f));
            Gizmos.matrix = previous;
        }
    }
}
