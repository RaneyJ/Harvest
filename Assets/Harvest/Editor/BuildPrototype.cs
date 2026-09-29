using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Harvest.Editor
{
    public static class BuildPrototype
    {
        const string ScenePath = "Assets/Harvest/Scenes/TheLine.unity";

        [InitializeOnLoadMethod]
        static void CreateOnFirstImport()
        {
            EditorApplication.delayCall += () =>
            {
                if (!File.Exists(ScenePath) && !EditorApplication.isPlayingOrWillChangePlaymode)
                    Build();
            };
        }

        [MenuItem("Harvest/Build The Line Prototype")]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Directory.CreateDirectory("Assets/Harvest/Scenes");
            Directory.CreateDirectory("Assets/Harvest/Materials");
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            Material soil = MakeMaterial("Soil", new Color(0.29f, 0.24f, 0.17f));
            Material road = MakeMaterial("Freight Road", new Color(0.38f, 0.37f, 0.32f));
            Material grain = MakeMaterial("Grain", new Color(0.68f, 0.51f, 0.21f));
            Material concrete = MakeMaterial("Concrete", new Color(0.39f, 0.41f, 0.39f));
            Material rust = MakeMaterial("Rust", new Color(0.40f, 0.22f, 0.15f));
            Material human = MakeMaterial("Marine", new Color(0.24f, 0.32f, 0.27f));
            Material grunt = MakeMaterial("Grunt", new Color(0.40f, 0.38f, 0.47f));
            Material jackal = MakeMaterial("Jackal", new Color(0.25f, 0.55f, 0.78f), true);
            Material brute = MakeMaterial("Brute", new Color(0.29f, 0.25f, 0.26f));
            Material plasma = MakeMaterial("Plasma", new Color(0.4f, 0.15f, 0.85f), true);
            Material beacon = MakeMaterial("Evac Beacon", new Color(0.17f, 0.9f, 0.35f), true);

            Block("Field", soil, new Vector3(0, -0.55f, 10), new Vector3(130, 1, 150));
            Block("Road", road, new Vector3(0, -0.02f, 10), new Vector3(15, 0.1f, 130));
            for (int side = -1; side <= 1; side += 2)
            {
                for (int row = 0; row < 16; row++)
                {
                    for (int col = 0; col < 5; col++)
                    {
                        float x = side * (10f + col * 3.1f);
                        float z = -42f + row * 6f;
                        GameObject crop = Block("Harvest crop", grain, new Vector3(x, 0.65f, z), new Vector3(0.55f, 1.3f, 4.1f));
                        Object.DestroyImmediate(crop.GetComponent<Collider>());
                    }
                }
            }
            // Waist-high cover with open lanes. Everything is ordinary farm or freight infrastructure.
            Block("Checkpoint barricade left", concrete, new Vector3(-5, 0.7f, -7), new Vector3(4.5f, 1.4f, 1.3f));
            Block("Checkpoint barricade right", concrete, new Vector3(5, 0.7f, -7), new Vector3(4.5f, 1.4f, 1.3f));
            Block("Cargo left", rust, new Vector3(-7, 1.4f, 10), new Vector3(3, 2.8f, 4));
            Block("Cargo right", rust, new Vector3(8, 1.1f, 17), new Vector3(3, 2.2f, 5));
            Block("Loading station", concrete, new Vector3(-20, 4, 10), new Vector3(8, 8, 12));
            Block("Freight tower", rust, new Vector3(23, 7, 27), new Vector3(4, 14, 4));
            Block("Road barrier", concrete, new Vector3(3, 0.6f, 8), new Vector3(3, 1.2f, 1));
            Block("Road barrier", concrete, new Vector3(-3, 0.6f, 19), new Vector3(3, 1.2f, 1));
            Block("Burned freight car", rust, new Vector3(-13, 2, 38), new Vector3(5, 4, 12));

            GameObject evacuation = Block("Evacuation pad", concrete, new Vector3(0, 0.08f, -33), new Vector3(10, 0.16f, 8));
            Block("Evac beacon", beacon, new Vector3(0, 2.7f, -36), new Vector3(0.32f, 5.2f, 0.32f));
            for (int i = 0; i < 3; i++) Smoke(new Vector3(-18 + i * 20, 0.3f, 40 + i * 10));

            GameObject player = new GameObject("Marine");
            player.transform.position = new Vector3(0, 1.2f, -21);
            CharacterController cc = player.AddComponent<CharacterController>();
            cc.height = 1.9f; cc.radius = 0.4f;
            MarineController marine = player.AddComponent<MarineController>();
            GameObject view = new GameObject("Eyes");
            view.transform.SetParent(player.transform, false);
            view.transform.localPosition = new Vector3(0, 0.65f, 0);
            Camera camera = view.AddComponent<Camera>();
            camera.fieldOfView = 75f;
            camera.tag = "MainCamera";
            view.AddComponent<AudioListener>();
            marine.View = camera;
            GameObject rifle = Block("Rifle silhouette", human, new Vector3(0, 0, 0), new Vector3(0.13f, 0.14f, 0.65f));
            rifle.transform.SetParent(view.transform, false);
            rifle.transform.localPosition = new Vector3(0.36f, -0.33f, 0.7f);
            Object.DestroyImmediate(rifle.GetComponent<Collider>());

            GameObject director = new GameObject("Encounter Director");
            HarvestEncounter encounter = director.AddComponent<HarvestEncounter>();
            encounter.Marine = marine;
            encounter.GruntMaterial = grunt;
            encounter.JackalMaterial = jackal;
            encounter.BruteMaterial = brute;
            encounter.PlasmaMaterial = plasma;
            encounter.EvacuationPad = evacuation.transform;

            GameObject sun = new GameObject("Dusk sun");
            Light light = sun.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.69f, 0.43f);
            light.intensity = 1.35f;
            sun.transform.rotation = Quaternion.Euler(22, -55, 0);
            RenderSettings.ambientLight = new Color(0.42f, 0.34f, 0.32f);
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.47f, 0.37f, 0.31f);
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.012f;
            camera.backgroundColor = RenderSettings.fogColor;
            camera.clearFlags = CameraClearFlags.SolidColor;

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
            Debug.Log("The Line prototype is ready. Press Play, click the Game view, and hold the road.");
        }

        static Material MakeMaterial(string name, Color color, bool glowing = false)
        {
            string path = $"Assets/Harvest/Materials/{name}.mat";
            Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;
            Shader shader = Shader.Find("Standard");
            Material material = new Material(shader) { name = name, color = color };
            if (glowing)
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * 2f);
            }
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        static GameObject Block(string name, Material material, Vector3 position, Vector3 scale)
        {
            GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = name;
            block.transform.position = position;
            block.transform.localScale = scale;
            block.GetComponent<Renderer>().sharedMaterial = material;
            return block;
        }

        static void Smoke(Vector3 position)
        {
            GameObject objectWithSmoke = new GameObject("Smoke column");
            objectWithSmoke.transform.position = position;
            ParticleSystem particles = objectWithSmoke.AddComponent<ParticleSystem>();
            var main = particles.main;
            main.startLifetime = 8f;
            main.startSpeed = 2f;
            main.startSize = 3f;
            main.startColor = new Color(0.2f, 0.17f, 0.15f, 0.35f);
            main.maxParticles = 150;
            var emission = particles.emission;
            emission.rateOverTime = 12f;
            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.radius = 1.5f;
        }
    }
}
