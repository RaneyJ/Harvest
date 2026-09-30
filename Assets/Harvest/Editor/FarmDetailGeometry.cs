using UnityEngine;
using Object = UnityEngine.Object;

namespace Harvest.Editor
{
    // Large props use simple collision; trim and small dressing remain visual only.
    public static class FarmDetailGeometry
    {
        public static void Build()
        {
            Material timber = WeaponModelGeometry.MaterialFor("Farm Aged Timber", new Color(0.31f, 0.25f, 0.16f));
            Material iron = WeaponModelGeometry.MaterialFor("Farm Corrugated Iron", new Color(0.32f, 0.35f, 0.31f), 0.35f);
            Material pale = WeaponModelGeometry.MaterialFor("Farm Trim", new Color(0.65f, 0.61f, 0.48f));
            Material hay = WeaponModelGeometry.MaterialFor("Hay Bales", new Color(0.55f, 0.43f, 0.17f));
            Material dark = WeaponModelGeometry.MaterialFor("Farm Soot", new Color(0.12f, 0.12f, 0.105f));
            Material warning = WeaponModelGeometry.MaterialFor("Checkpoint Stripe", new Color(0.71f, 0.52f, 0.17f));
            // Shed occupies a field edge, clear of the road and existing marine cover lanes.
            Vector3 shed = new Vector3(-23f, 0f, 19f);
            Box("Shed rear wall", timber, shed + new Vector3(0f, 1.5f, 3.8f), new Vector3(8f, 3f, 0.18f), true);
            Box("Shed field wall", timber, shed + new Vector3(-3.9f, 1.5f, 0f), new Vector3(0.18f, 3f, 8f), true);
            foreach (int side in new[] { -1, 1 })
                Box("Shed front post", timber, shed + new Vector3(side * 3.8f, 1.5f, -3.8f), new Vector3(0.2f, 3f, 0.2f), true);
            GameObject roof = Box("Shed metal roof", iron, shed + Vector3.up * 3.15f, new Vector3(8.5f, 0.16f, 8.5f), true);
            roof.transform.rotation = Quaternion.Euler(0f, 0f, -5f);
            for (int i = 0; i < 13; i++)
            {
                float x = -3.9f + i * 0.65f;
                GameObject seam = Box("Shed roof seam", dark, shed + new Vector3(x, 3.26f - x * Mathf.Tan(5f * Mathf.Deg2Rad), 0f), new Vector3(0.025f, 0.018f, 8.3f));
                seam.transform.rotation = roof.transform.rotation;
            }
            for (int i = 0; i < 3; i++)
            {
                Vector3 center = shed + new Vector3(-2.4f + i * 1.8f, 0.55f, 2.3f);
                Box("Stacked hay bale", hay, center, new Vector3(1.4f, 1.1f, 1.1f), true);
                foreach (float band in new[] { -0.42f, 0.42f })
                    Box("Bale binding", timber, center + Vector3.right * band, new Vector3(0.035f, 1.115f, 1.115f));
            }
            Vector3 bin = new Vector3(24f, 3.3f, 48f);
            GameObject silo = GameObject.CreatePrimitive(PrimitiveType.Cylinder); silo.name = "Harvest grain silo";
            silo.transform.position = bin; silo.transform.localScale = new Vector3(6f, 3.3f, 6f);
            silo.GetComponent<Renderer>().sharedMaterial = iron; Object.DestroyImmediate(silo.GetComponent<Collider>());
            silo.AddComponent<BoxCollider>().size = new Vector3(1f, 2f, 1f);
            for (int i = 0; i < 16; i++)
            {
                float angle = i * Mathf.PI * 2f / 16f;
                Box("Silo vertical rib", pale, bin + new Vector3(Mathf.Sin(angle) * 3.01f, 0f, Mathf.Cos(angle) * 3.01f), new Vector3(0.045f, 6.5f, 0.045f));
            }
            Box("Silo access door", dark, new Vector3(24f, 1f, 44.98f), new Vector3(1f, 2f, 0.03f));
            for (int i = 0; i < 11; i++)
                Box("Silo ladder rung", pale, new Vector3(26f, 0.6f + i * 0.5f, 45.65f), new Vector3(0.6f, 0.045f, 0.045f));
            foreach (Vector3 site in new[] { new Vector3(-28f, 0f, -16f), new Vector3(19f, 0f, 6f), new Vector3(19f, 0f, -24f) })
            {
                Box("Farm storage crate", timber, site + Vector3.up * 0.6f, new Vector3(1.5f, 1.2f, 1.3f), true);
                for (int i = 0; i < 5; i++)
                    Box("Crate plank seam", dark, site + new Vector3(0f, 0.17f + i * 0.22f, -0.656f), new Vector3(1.4f, 0.018f, 0.012f));
                foreach (int side in new[] { -1, 1 })
                    Box("Crate metal strap", iron, site + new Vector3(side * 0.5f, 0.6f, 0f), new Vector3(0.045f, 1.21f, 1.31f));
            }
            GameObject house = GameObject.Find("Enterable farmhouse");
            if (house != null)
            {
                // Road-facing windows: exact openings from the main farmhouse recipe.
                foreach (float storey in new[] { 0f, 3.2f })
                {
                    float width = storey == 0f ? 3.2f : 4f;
                    float sill = storey == 0f ? 0.9f : 0.85f;
                    float lintel = storey == 0f ? 2.2f : 2.3f;
                    Vector3 origin = house.transform.position;
                    Box("Window upper trim", pale, origin + new Vector3(6.13f, storey + lintel, 1f), new Vector3(0.08f, 0.1f, width + 0.18f));
                    Box("Window sill trim", pale, origin + new Vector3(6.15f, storey + sill, 1f), new Vector3(0.16f, 0.1f, width + 0.18f));
                    foreach (int side in new[] { -1, 1 })
                        Box("Window side trim", pale, origin + new Vector3(6.13f, storey + (sill + lintel) * 0.5f, 1f + side * width * 0.5f), new Vector3(0.08f, lintel - sill, 0.08f));
                }
                Box("Farmhouse chimney", iron, house.transform.position + new Vector3(-3f, 7.1f, 4f), new Vector3(0.6f, 2.2f, 0.6f));
                Box("Farmhouse gutter", iron, house.transform.position + new Vector3(6.3f, 6.4f, 0f), new Vector3(0.12f, 0.1f, 14.9f));
                Box("Farmhouse drain pipe", iron, house.transform.position + new Vector3(6.3f, 3.2f, 6.5f), new Vector3(0.09f, 6.4f, 0.09f));
                foreach (int side in new[] { -1, 1 })
                    Box("Farmhouse porch bench", timber, house.transform.position + new Vector3(side * 1.8f, 0.45f, -8.5f), new Vector3(0.6f, 0.18f, 1.5f));
            }
            Sign("HARVEST / FREIGHT ROAD", new Vector3(10.2f, 2f, -28f), pale, timber, 180f);
            Sign("CHECKPOINT / KEEP CLEAR", new Vector3(-9.5f, 1.7f, -7f), warning, dark, 180f);
            Sign("EVACUATION", new Vector3(7f, 1.8f, -34f), pale, dark, 180f);
        }
        static void Sign(string words, Vector3 position, Material paint, Material pole, float yaw)
        {
            Box("Sign post", pole, new Vector3(position.x, position.y * 0.5f, position.z), new Vector3(0.12f, position.y, 0.12f));
            Box("Sign board", paint, position, new Vector3(2.4f, 0.75f, 0.08f));
            foreach (int side in new[] { -1, 1 })
            {
                GameObject text = new GameObject(words); text.transform.position = position + Vector3.forward * (side * 0.055f);
                text.transform.rotation = Quaternion.Euler(0f, side < 0 ? 0f : 180f, 0f);
                TextMesh label = text.AddComponent<TextMesh>();
                label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (label.font != null) text.GetComponent<MeshRenderer>().sharedMaterial = label.font.material;
                label.text = words.Replace(" / ", "\n");
                label.anchor = TextAnchor.MiddleCenter; label.alignment = TextAlignment.Center;
                label.fontSize = 40; label.characterSize = 0.045f; label.color = new Color(0.1f, 0.13f, 0.1f);
            }
        }
        static GameObject Box(string name, Material material, Vector3 position, Vector3 size, bool solid = false)
        {
            GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube); part.name = name;
            part.transform.position = position; part.transform.localScale = size;
            part.GetComponent<Renderer>().sharedMaterial = material;
            if (!solid) Object.DestroyImmediate(part.GetComponent<Collider>());
            return part;
        }
    }
}
