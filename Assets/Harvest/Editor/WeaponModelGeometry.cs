using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Harvest.Editor
{
    // A single authored-in-code model recipe serves view, actor and pickup presentation.
    public static class WeaponModelGeometry
    {
        public static GameObject Create(WeaponDefinition definition, Transform parent = null, bool firstPerson = true)
        {
            if (firstPerson && definition.ViewModelPrefab != null)
            {
                GameObject authored = (GameObject)PrefabUtility.InstantiatePrefab(definition.ViewModelPrefab);
                authored.transform.SetParent(parent, false);
                foreach (Collider collider in authored.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
                return authored;
            }
            GameObject root = new GameObject("Detailed " + definition.DisplayName);
            root.transform.SetParent(parent, false);
            Material metal = MaterialFor("Weapon Steel", new Color(0.15f, 0.18f, 0.17f), 0.6f);
            Material grip = MaterialFor("Weapon Grip", new Color(0.07f, 0.08f, 0.07f));
            Material wood = MaterialFor("Hunting Walnut", new Color(0.34f, 0.17f, 0.075f));
            Material accent = MaterialFor("Weapon Markings", new Color(0.58f, 0.62f, 0.48f));
            Material body = definition.PickupMaterial != null ? definition.PickupMaterial : metal;
            bool plasma = definition.ShotKind == WeaponShotKind.PlasmaBolt;
            bool shotgun = definition.DisplayName.Contains("SHOTGUN");
            if (plasma)
            {
                Material glow = MaterialFor("Plasma Vents", new Color(0.12f, 0.75f, 0.78f), 0.3f, true);
                Part(root, "Receiver", body, new Vector3(0f, 0f, 0.12f), new Vector3(0.18f, 0.14f, definition.Automatic ? 0.46f : 0.3f));
                Part(root, "Grip", grip, new Vector3(0f, -0.13f, -0.03f), new Vector3(0.1f, 0.19f, 0.12f), -18f);
                foreach (int side in new[] { -1, 1 })
                {
                    Part(root, "Curved front housing", body, new Vector3(side * 0.095f, 0.05f, 0.38f), new Vector3(0.09f, 0.12f, 0.26f), side * 12f);
                    Part(root, "Lower prong", body, new Vector3(side * 0.095f, -0.07f, 0.3f), new Vector3(0.075f, 0.08f, 0.22f), -8f);
                    for (int i = 0; i < 4; i++)
                        Part(root, "Cooling vent", glow, new Vector3(side * 0.094f, 0.015f, i * 0.06f), new Vector3(0.008f, 0.035f, 0.022f));
                }
                Part(root, "Energy core", glow, new Vector3(0f, 0.04f, 0.39f), new Vector3(0.06f, 0.045f, 0.1f));
            }
            else if (definition.IsPrecision)
            {
                Part(root, "Receiver", metal, new Vector3(0f, 0.025f, 0.08f), new Vector3(0.08f, 0.07f, 0.3f));
                Part(root, "Wood stock", wood, new Vector3(0f, -0.035f, -0.1f), new Vector3(0.1f, 0.12f, 0.65f));
                Part(root, "Butt pad", grip, new Vector3(0f, -0.04f, -0.435f), new Vector3(0.105f, 0.13f, 0.025f));
                Barrel(root, metal, new Vector3(0f, 0.02f, 0.4f), 0.035f, 0.65f);
                Part(root, "Bolt", metal, new Vector3(0.065f, 0.025f, 0f), new Vector3(0.06f, 0.04f, 0.1f));
                Part(root, "Trigger guard", metal, new Vector3(0f, -0.11f, -0.08f), new Vector3(0.055f, 0.018f, 0.11f));
                Part(root, "Front iron sight", metal, new Vector3(0f, 0.07f, 0.68f), new Vector3(0.015f, 0.035f, 0.015f));
                foreach (int side in new[] { -1, 1 })
                    Part(root, "Rear sight", metal, new Vector3(side * 0.024f, 0.07f, 0.1f), new Vector3(0.015f, 0.035f, 0.03f));
            }
            else
            {
                Part(root, "Receiver", metal, new Vector3(0f, 0f, 0.05f), new Vector3(0.12f, 0.12f, 0.35f));
                Part(root, "Butt stock", grip, new Vector3(0f, -0.035f, -0.29f), new Vector3(0.105f, 0.14f, 0.3f));
                Part(root, "Butt pad", metal, new Vector3(0f, -0.035f, -0.45f), new Vector3(0.11f, 0.15f, 0.02f));
                Part(root, "Pistol grip", grip, new Vector3(0f, -0.13f, -0.07f), new Vector3(0.085f, 0.18f, 0.1f), -16f);
                Barrel(root, metal, new Vector3(0f, 0.015f, 0.45f), shotgun ? 0.065f : 0.035f, shotgun ? 0.48f : 0.53f);
                Part(root, shotgun ? "Pump" : "Foregrip", grip, new Vector3(0f, -0.02f, 0.34f), new Vector3(0.11f, 0.09f, 0.26f));
                if (shotgun) Barrel(root, metal, new Vector3(0f, -0.06f, 0.4f), 0.042f, 0.49f);
                else Part(root, "Magazine", metal, new Vector3(0f, -0.15f, 0.13f), new Vector3(0.07f, 0.22f, 0.13f), 8f);
                Part(root, "Ejection port", grip, new Vector3(0.063f, 0.015f, 0.065f), new Vector3(0.005f, 0.038f, 0.11f));
                Part(root, "Front sight", metal, new Vector3(0f, 0.075f, 0.58f), new Vector3(0.025f, 0.06f, 0.02f));
                Part(root, "Rear sight", metal, new Vector3(0f, 0.08f, -0.04f), new Vector3(0.065f, 0.025f, 0.04f));
                Part(root, "Serial plate", accent, new Vector3(0.062f, -0.025f, -0.025f), new Vector3(0.003f, 0.015f, 0.05f));
                for (int i = 0; i < 5; i++)
                    Part(root, "Grip rib", metal, new Vector3(0f, -0.066f, 0.24f + i * 0.042f), new Vector3(0.112f, 0.008f, 0.009f));
            }
            return root;
        }
        public static void AssignWorldPrefab(WeaponDefinition definition)
        {
            string path = "Assets/Harvest/Prefabs/" + definition.DisplayName + " Model.prefab";
            // Keep artist-owned world prefabs. Only the generated fallback path is regenerated.
            if (definition.WorldModel != null && AssetDatabase.GetAssetPath(definition.WorldModel) != path) return;
            GameObject model = Create(definition, firstPerson: false);
            definition.WorldModel = PrefabUtility.SaveAsPrefabAsset(model, path);
            Object.DestroyImmediate(model); EditorUtility.SetDirty(definition);
        }
        static GameObject Part(GameObject root, string name, Material material, Vector3 position, Vector3 size, float tilt = 0f)
        {
            GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube); part.name = name;
            part.transform.SetParent(root.transform, false); part.transform.localPosition = position;
            part.transform.localScale = size; part.transform.localRotation = Quaternion.Euler(tilt, 0f, 0f);
            part.GetComponent<Renderer>().sharedMaterial = material; Object.DestroyImmediate(part.GetComponent<Collider>());
            return part;
        }
        static void Barrel(GameObject root, Material material, Vector3 position, float diameter, float length)
        {
            GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cylinder); part.name = "Barrel";
            part.transform.SetParent(root.transform, false); part.transform.localPosition = position;
            part.transform.localRotation = Quaternion.Euler(90f, 0f, 0f); part.transform.localScale = new Vector3(diameter, length * 0.5f, diameter);
            part.GetComponent<Renderer>().sharedMaterial = material; Object.DestroyImmediate(part.GetComponent<Collider>());
        }
        public static Material MaterialFor(string name, Color color, float metallic = 0f, bool emission = false)
        {
            string path = "Assets/Harvest/Materials/" + name + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            material = new Material(Shader.Find("Universal Render Pipeline/Lit")); material.color = color;
            material.SetFloat("_Metallic", metallic); material.SetFloat("_Smoothness", 0.32f);
            if (emission) { material.EnableKeyword("_EMISSION"); material.SetColor("_EmissionColor", color * 0.7f); }
            AssetDatabase.CreateAsset(material, path); return material;
        }
    }
}
