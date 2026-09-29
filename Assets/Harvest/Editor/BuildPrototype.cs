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
        const string SceneVersionPath = "Assets/Harvest/Scenes/TheLineVersion.txt";
        const string SceneVersion = "4";
        const string EncounterPath = "Assets/Harvest/Data/The Line.asset";

        [InitializeOnLoadMethod]
        static void CreateOnFirstImport()
        {
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode) return;
                if (!File.Exists(ScenePath))
                {
                    Build();
                    return;
                }
                EncounterDefinition data = AssetDatabase.LoadAssetAtPath<EncounterDefinition>(EncounterPath);
                bool sceneReferencesData = System.Array.IndexOf(AssetDatabase.GetDependencies(ScenePath), EncounterPath) >= 0;
                bool currentScene = File.Exists(SceneVersionPath) && File.ReadAllText(SceneVersionPath).Trim() == SceneVersion;
                if (currentScene && data != null && data.Waves != null && data.Waves.Length > 0 && sceneReferencesData) return;
                if (EditorUtility.DisplayDialog("Update The Line prototype",
                    "This scene predates the current combat and armor setup. Rebuild the graybox scene to play the new version. Save any manual scene edits before continuing; existing weapon and encounter assets are preserved.",
                    "Rebuild scene", "Later"))
                    Build();
            };
        }

        [MenuItem("Harvest/Build The Line Prototype")]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Directory.CreateDirectory("Assets/Harvest/Scenes");
            Directory.CreateDirectory("Assets/Harvest/Materials");
            Directory.CreateDirectory("Assets/Harvest/Prefabs");
            Directory.CreateDirectory("Assets/Harvest/Data");
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
            Material plasmaRifleColor = MakeMaterial("Plasma Rifle", new Color(0.18f, 0.35f, 0.85f), true);
            Material armorMaterial = MakeMaterial("Armor Supply", new Color(0.24f, 0.67f, 0.49f), true);
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
            CreateArmorPickup(new Vector3(2f, 0.8f, -11f), armorMaterial, 80f);
            CreateArmorPickup(new Vector3(-5f, 0.8f, 13f), armorMaterial, 80f);
            for (int i = 0; i < 3; i++) Smoke(new Vector3(-18 + i * 20, 0.3f, 40 + i * 10));

            WeaponDefinition rifleData = MakeWeapon("Service Rifle", "SERVICE RIFLE", 32, 128, 24f, 1f, 1, 0f, 90f, 0.12f, 1.8f, true, human);
            WeaponDefinition shotgunData = MakeWeapon("Combat Shotgun", "COMBAT SHOTGUN", 6, 24, 14f, 1.35f, 8, 6f, 22f, 0.75f, 2.3f, false, rust);
            PlasmaBolt bolt = MakeBoltPrefab(plasma);
            WeaponDefinition plasmaPistol = MakePlasmaWeapon("Plasma Pistol", 20f, 1.7f, 2, 0.55f, 17f, false, bolt, plasma);
            WeaponDefinition plasmaRifle = MakePlasmaWeapon("Plasma Rifle", 12f, 1.25f, 3, 0.13f, 22f, true, bolt, plasmaRifleColor);
            DroppedWeapon dropPrefab = MakeDropPrefab();
            CovenantEnemy gruntPrefab = MakeEnemyPrefab<GruntBehavior>("Grunt", grunt, plasmaPistol, dropPrefab, 48, 0, 2.6f, 12f, 0.9f);
            CovenantEnemy jackalPrefab = MakeEnemyPrefab<JackalBehavior>("Jackal", jackal, plasmaPistol, dropPrefab, 75, 85, 2.3f, 16f, 0.9f);
            CovenantEnemy brutePrefab = MakeEnemyPrefab<BruteBehavior>("Brute", brute, plasmaRifle, dropPrefab, 280, 0, 4.2f, 1.9f, 1.6f);
            EncounterDefinition encounterData = MakeEncounter(gruntPrefab, jackalPrefab, brutePrefab);

            GameObject player = new GameObject("Marine");
            player.transform.position = new Vector3(0, 1.2f, -21);
            CharacterController cc = player.AddComponent<CharacterController>();
            cc.height = 1.9f; cc.radius = 0.4f;
            Vitality playerVitality = player.AddComponent<Vitality>();
            playerVitality.MaxHealth = 100f;
            MarineArmor marineArmor = player.AddComponent<MarineArmor>();
            marineArmor.MaxArmor = 175f;
            marineArmor.HealthRegenDelay = 7f;
            marineArmor.HealthRegenPerSecond = 5f;
            MarineLoadout loadout = player.AddComponent<MarineLoadout>();
            loadout.Weapons = new[] { rifleData, shotgunData };
            loadout.DropPrefab = dropPrefab;
            MarineController marine = player.AddComponent<MarineController>();
            GameObject view = new GameObject("Eyes");
            view.transform.SetParent(player.transform, false);
            view.transform.localPosition = new Vector3(0, 0.65f, 0);
            Camera camera = view.AddComponent<Camera>();
            camera.fieldOfView = 75f;
            camera.tag = "MainCamera";
            view.AddComponent<AudioListener>();
            marine.View = camera;
            loadout.View = camera;
            GameObject rifle = Block("Service rifle silhouette", human, new Vector3(0, 0, 0), new Vector3(0.13f, 0.14f, 0.65f));
            rifle.transform.SetParent(view.transform, false);
            rifle.transform.localPosition = new Vector3(0.36f, -0.33f, 0.7f);
            Object.DestroyImmediate(rifle.GetComponent<Collider>());
            GameObject shotgun = Block("Combat shotgun silhouette", rust, new Vector3(0, 0, 0), new Vector3(0.18f, 0.19f, 0.48f));
            shotgun.transform.SetParent(view.transform, false);
            shotgun.transform.localPosition = new Vector3(0.36f, -0.35f, 0.62f);
            Object.DestroyImmediate(shotgun.GetComponent<Collider>());
            GameObject pistolModel = MakeViewModel("Plasma pistol silhouette", plasma, view.transform,
                new Vector3(0.33f, -0.32f, 0.6f), new Vector3(0.22f, 0.19f, 0.35f));
            GameObject rifleModel = MakeViewModel("Plasma rifle silhouette", plasmaRifleColor, view.transform,
                new Vector3(0.36f, -0.32f, 0.75f), new Vector3(0.25f, 0.2f, 0.7f));
            WeaponView weaponView = view.AddComponent<WeaponView>();
            weaponView.Loadout = loadout;
            weaponView.Definitions = new[] { rifleData, shotgunData, plasmaPistol, plasmaRifle };
            weaponView.Models = new[] { rifle, shotgun, pistolModel, rifleModel };

            GameObject director = new GameObject("Encounter Director");
            HarvestEncounter encounter = director.AddComponent<HarvestEncounter>();
            encounter.Marine = marine;
            encounter.Definition = encounterData;
            encounter.EvacuationPad = evacuation.transform;
            HarvestHud hud = director.AddComponent<HarvestHud>();
            hud.Marine = marine;
            hud.Armor = marineArmor;
            hud.Loadout = loadout;
            hud.Encounter = encounter;

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
            File.WriteAllText(SceneVersionPath, SceneVersion);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
            Debug.Log("The Line prototype is ready. Press Play, click the Game view, and hold the road.");
        }

        static WeaponDefinition MakeWeapon(string assetName, string displayName, int magazine, int reserve,
            float damage, float shieldMultiplier, int pellets, float spread, float range, float interval, float reload,
            bool automatic, Material pickupMaterial)
        {
            string path = $"Assets/Harvest/Data/{assetName}.asset";
            WeaponDefinition weapon = AssetDatabase.LoadAssetAtPath<WeaponDefinition>(path);
            if (weapon != null)
            {
                if (weapon.PickupMaterial == null)
                {
                    weapon.PickupMaterial = pickupMaterial;
                    EditorUtility.SetDirty(weapon);
                }
                return weapon;
            }
            weapon = ScriptableObject.CreateInstance<WeaponDefinition>();
            AssetDatabase.CreateAsset(weapon, path);
            weapon.DisplayName = displayName;
            weapon.MagazineSize = magazine;
            weapon.StartingReserve = reserve;
            weapon.DamagePerPellet = damage;
            weapon.ShieldMultiplier = shieldMultiplier;
            weapon.Pellets = pellets;
            weapon.SpreadDegrees = spread;
            weapon.Range = range;
            weapon.FireInterval = interval;
            weapon.ReloadSeconds = reload;
            weapon.Automatic = automatic;
            weapon.PickupMaterial = pickupMaterial;
            EditorUtility.SetDirty(weapon);
            return weapon;
        }

        static WeaponDefinition MakePlasmaWeapon(string name, float damage, float shieldMultiplier,
            int energyCost, float interval, float speed, bool automatic, PlasmaBolt bolt, Material material)
        {
            string path = $"Assets/Harvest/Data/{name}.asset";
            WeaponDefinition weapon = AssetDatabase.LoadAssetAtPath<WeaponDefinition>(path);
            if (weapon != null) return weapon;
            weapon = ScriptableObject.CreateInstance<WeaponDefinition>();
            AssetDatabase.CreateAsset(weapon, path);
            weapon.DisplayName = name.ToUpperInvariant();
            weapon.ShotKind = WeaponShotKind.PlasmaBolt;
            weapon.UsesEnergy = true;
            weapon.EnergyCapacity = 100;
            weapon.EnergyPerShot = energyCost;
            weapon.DamagePerPellet = damage;
            weapon.ShieldMultiplier = shieldMultiplier;
            weapon.Pellets = 1;
            weapon.SpreadDegrees = automatic ? 2f : 0.8f;
            weapon.Range = 60f;
            weapon.FireInterval = interval;
            weapon.Automatic = automatic;
            weapon.ProjectilePrefab = bolt;
            weapon.ProjectileSpeed = speed;
            weapon.PickupMaterial = material;
            EditorUtility.SetDirty(weapon);
            return weapon;
        }

        static DroppedWeapon MakeDropPrefab()
        {
            const string path = "Assets/Harvest/Prefabs/Dropped Weapon.prefab";
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing.GetComponent<DroppedWeapon>();
            GameObject root = new GameObject("Dropped Weapon");
            SphereCollider trigger = root.AddComponent<SphereCollider>();
            trigger.radius = 0.7f;
            trigger.isTrigger = true;
            DroppedWeapon drop = root.AddComponent<DroppedWeapon>();
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visual.name = "Weapon silhouette";
            visual.transform.SetParent(root.transform, false);
            visual.transform.localScale = new Vector3(0.55f, 0.22f, 0.38f);
            Object.DestroyImmediate(visual.GetComponent<Collider>());
            drop.Visual = visual.GetComponent<Renderer>();
            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return saved.GetComponent<DroppedWeapon>();
        }

        static GameObject MakeViewModel(string name, Material material, Transform parent, Vector3 position, Vector3 scale)
        {
            GameObject model = Block(name, material, Vector3.zero, scale);
            model.transform.SetParent(parent, false);
            model.transform.localPosition = position;
            Object.DestroyImmediate(model.GetComponent<Collider>());
            return model;
        }

        static PlasmaBolt MakeBoltPrefab(Material material)
        {
            string path = "Assets/Harvest/Prefabs/Plasma Bolt.prefab";
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing.GetComponent<PlasmaBolt>();
            GameObject root = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            root.name = "Plasma Bolt";
            root.transform.localScale = Vector3.one * 0.26f;
            Object.DestroyImmediate(root.GetComponent<Collider>());
            root.GetComponent<Renderer>().sharedMaterial = material;
            root.AddComponent<PlasmaBolt>();
            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return saved.GetComponent<PlasmaBolt>();
        }

        static CovenantEnemy MakeEnemyPrefab<T>(string name, Material material, WeaponDefinition weapon, DroppedWeapon dropPrefab,
            float health, float shield, float moveSpeed, float preferredRange, float visualHeight)
            where T : EnemyBehavior
        {
            string path = $"Assets/Harvest/Prefabs/{name}.prefab";
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return EnsureEnemyPrefab<T>(path, weapon, dropPrefab);
            GameObject root = new GameObject(name);
            CharacterController controller = root.AddComponent<CharacterController>();
            bool isBrute = typeof(T) == typeof(BruteBehavior);
            controller.height = isBrute ? 3.2f : 2f;
            controller.radius = isBrute ? 0.62f : 0.42f;
            Vitality vitality = root.AddComponent<Vitality>();
            vitality.MaxHealth = health;
            vitality.MaxShield = shield;
            T behavior = root.AddComponent<T>();
            behavior.MoveSpeed = moveSpeed;
            behavior.PreferredRange = preferredRange;
            ActorWeapon enemyWeapon = root.AddComponent<ActorWeapon>();
            enemyWeapon.Team = CombatTeam.Covenant;
            enemyWeapon.StartingWeapon = weapon;
            enemyWeapon.DropPrefab = dropPrefab;
            root.AddComponent<CovenantEnemy>();
            EnemyHealthBar bar = root.AddComponent<EnemyHealthBar>();
            bar.Height = isBrute ? 3.3f : 2.1f;
            EnemyHitFeedback feedback = root.AddComponent<EnemyHitFeedback>();
            feedback.LabelHeight = isBrute ? 3.7f : 2.5f;
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.name = "Visual";
            visual.transform.SetParent(root.transform, false);
            visual.transform.localScale = isBrute ? new Vector3(1.4f, visualHeight, 1.4f) : new Vector3(0.9f, visualHeight, 0.9f);
            visual.GetComponent<Renderer>().sharedMaterial = material;
            Object.DestroyImmediate(visual.GetComponent<Collider>());
            AddHeldWeapon(root, weapon, isBrute);
            if (behavior is JackalBehavior jackalBehavior) AddJackalShield(root, jackalBehavior);
            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return saved.GetComponent<CovenantEnemy>();
        }

        static CovenantEnemy EnsureEnemyPrefab<T>(string path, WeaponDefinition weapon, DroppedWeapon dropPrefab) where T : EnemyBehavior
        {
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            bool changed = false;
            ActorWeapon enemyWeapon = root.GetComponent<ActorWeapon>();
            if (enemyWeapon == null)
            {
                enemyWeapon = root.AddComponent<ActorWeapon>();
                changed = true;
            }
            if (enemyWeapon.Team != CombatTeam.Covenant) { enemyWeapon.Team = CombatTeam.Covenant; changed = true; }
            if (enemyWeapon.StartingWeapon == null) { enemyWeapon.StartingWeapon = weapon; changed = true; }
            if (enemyWeapon.DropPrefab == null) { enemyWeapon.DropPrefab = dropPrefab; changed = true; }
            if (root.transform.Find("Held weapon") == null)
            {
                AddHeldWeapon(root, enemyWeapon.StartingWeapon, typeof(T) == typeof(BruteBehavior));
                changed = true;
            }
            if (root.GetComponent<EnemyHitFeedback>() == null)
            {
                EnemyHitFeedback feedback = root.AddComponent<EnemyHitFeedback>();
                feedback.LabelHeight = typeof(T) == typeof(BruteBehavior) ? 3.7f : 2.5f;
                changed = true;
            }
            if (root.GetComponent<JackalBehavior>() is JackalBehavior jackal && jackal.ShieldVisual == null)
            {
                AddJackalShield(root, jackal);
                changed = true;
            }
            if (root.GetComponent<BruteBehavior>() is BruteBehavior brute && brute.ChargeTriggerRange <= 0f)
            {
                brute.ChargeTriggerRange = 14f;
                brute.WindupSeconds = 0.85f;
                brute.ChargeSeconds = 1.1f;
                brute.ChargeSpeed = 8f;
                brute.RecoverySeconds = 1.2f;
                changed = true;
            }
            if (changed) PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);
            return AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponent<CovenantEnemy>();
        }

        static void AddJackalShield(GameObject root, JackalBehavior behavior)
        {
            GameObject plate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            plate.name = "Front shield";
            plate.transform.SetParent(root.transform, false);
            plate.transform.localPosition = new Vector3(0f, 0.15f, 0.65f);
            plate.transform.localScale = new Vector3(1.2f, 1.4f, 0.08f);
            plate.GetComponent<Renderer>().sharedMaterial = MakeMaterial("Jackal Shield", new Color(0.13f, 0.77f, 1f), true);
            Object.DestroyImmediate(plate.GetComponent<Collider>());
            behavior.ShieldVisual = plate;
        }

        static void AddHeldWeapon(GameObject root, WeaponDefinition definition, bool isBrute)
        {
            GameObject model = GameObject.CreatePrimitive(PrimitiveType.Cube);
            model.name = "Held weapon";
            model.transform.SetParent(root.transform, false);
            model.transform.localPosition = isBrute ? new Vector3(0.85f, 0.25f, 0.55f) : new Vector3(0.45f, 0f, 0.5f);
            model.transform.localScale = isBrute ? new Vector3(0.22f, 0.22f, 0.7f) : new Vector3(0.17f, 0.2f, 0.35f);
            if (definition != null) model.GetComponent<Renderer>().sharedMaterial = definition.PickupMaterial;
            Object.DestroyImmediate(model.GetComponent<Collider>());
        }

        static void CreateArmorPickup(Vector3 position, Material material, float amount)
        {
            GameObject root = new GameObject("Armor supply");
            root.transform.position = position;
            SphereCollider trigger = root.AddComponent<SphereCollider>();
            trigger.radius = 1.1f;
            trigger.isTrigger = true;
            ArmorPickup pickup = root.AddComponent<ArmorPickup>();
            pickup.ArmorAmount = amount;
            GameObject visual = Block("Armor pack", material, Vector3.zero, new Vector3(0.8f, 0.45f, 0.8f));
            visual.transform.SetParent(root.transform, false);
            Object.DestroyImmediate(visual.GetComponent<Collider>());
        }

        static EncounterDefinition MakeEncounter(CovenantEnemy grunt, CovenantEnemy jackal, CovenantEnemy brute)
        {
            EncounterDefinition definition = AssetDatabase.LoadAssetAtPath<EncounterDefinition>(EncounterPath);
            if (definition != null && definition.Waves != null && definition.Waves.Length > 0) return definition;
            if (definition == null)
            {
                definition = ScriptableObject.CreateInstance<EncounterDefinition>();
                AssetDatabase.CreateAsset(definition, EncounterPath);
            }
            definition.Waves = new[]
            {
                new EncounterWave
                {
                    Callout = "UNKNOWN HOSTILES — HOLD THE ROAD", DelaySeconds = 2f,
                    Enemies = new[] { Spawn(grunt, -6, 24), Spawn(grunt, 0, 27), Spawn(grunt, 6, 24) }
                },
                new EncounterWave
                {
                    Callout = "THE LINE IS BROKEN — CLEAR A PATH", DelaySeconds = 4f,
                    Enemies = new[] { Spawn(grunt, -8, 31), Spawn(grunt, 8, 31), Spawn(grunt, -3, 34),
                        Spawn(grunt, 3, 34), Spawn(jackal, -6, 36), Spawn(brute, 0, 38) }
                }
            };
            EditorUtility.SetDirty(definition);
            return definition;
        }

        static EnemySpawn Spawn(CovenantEnemy prefab, float x, float z)
        {
            return new EnemySpawn { Prefab = prefab, Position = new Vector3(x, prefab.GetComponent<CharacterController>().height / 2f, z) };
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
