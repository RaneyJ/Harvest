using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.AI;

namespace Harvest.Editor
{
    public static class BuildPrototype
    {
        const string ScenePath = "Assets/Harvest/Scenes/TheLine.unity";
        const string SceneVersionPath = "Assets/Harvest/Scenes/TheLineVersion.txt";
        const string SceneVersion = "32";
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
                    "This scene predates the interior readability pass. Rebuild The Line for two practical fixtures per storey and soft baked window light. Save manual edits first; authored tuning and the window-sill correction are retained. Bake lighting and reflections after rebuilding.",
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
            HarvestRenderSetup.Ensure();
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

            FarmEncounterGeometry.Build(soil, road, grain, concrete, rust);
            FarmDetailGeometry.Build();
            // Waist-high cover with open lanes. Everything is ordinary farm or freight infrastructure.
            Block("Checkpoint barricade left", concrete, new Vector3(-5, 0.7f, -7), new Vector3(4.5f, 1.4f, 1.3f));
            Block("Checkpoint barricade right", concrete, new Vector3(5, 0.7f, -7), new Vector3(4.5f, 1.4f, 1.3f));
            Block("Reserve barricade", concrete, new Vector3(0, 0.7f, -13), new Vector3(3f, 1.4f, 1.3f));
            CreateCover("Left checkpoint", new Vector3(-5f, 0.05f, -8.3f), new Vector3(3.3f, 0f, 0f));
            CreateCover("Right checkpoint", new Vector3(5f, 0.05f, -8.3f), new Vector3(-3.3f, 0f, 0f));
            CreateCover("Reserve checkpoint", new Vector3(0f, 0.05f, -14.3f), new Vector3(-2.4f, 0f, 0f));
            CreateCover("Forward road left", new Vector3(-3f, 0.05f, 17.8f), new Vector3(-2.4f, 0f, 0f));
            CreateCover("Forward road right", new Vector3(3f, 0.05f, 6.8f), new Vector3(2.4f, 0f, 0f));
            new GameObject("Battlefield navigation").AddComponent<BattlefieldNavigation>();
            Block("Cargo left", rust, new Vector3(-7, 1.4f, 10), new Vector3(3, 2.8f, 4));
            Block("Cargo right", rust, new Vector3(8, 1.1f, 17), new Vector3(3, 2.2f, 5));
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
            WeaponDefinition shotgunData = MakeWeapon("Combat Shotgun", "COMBAT SHOTGUN", 6, 24, 20f, 1.35f, 8, 6f, 22f, 0.75f, 2.3f, false, rust);
            shotgunData.DamagePerPellet = Mathf.Max(20f, shotgunData.DamagePerPellet);
            EditorUtility.SetDirty(shotgunData);
            WeaponDefinition huntingRifle = MakeHuntingRifle(rust);
            AssignApprovedAudio(shotgunData, "ShotgunNova", null);
            if (shotgunData.AudioMixRevision < 1)
            {
                shotgunData.FireVolume = 1f; shotgunData.AudioMixRevision = 1;
                EditorUtility.SetDirty(shotgunData);
            }
            AssignApprovedAudio(rifleData, "ServiceRifle", "RifleReload");
            AssignApprovedAudio(huntingRifle, "HuntingRifleMosin", null);
            PlasmaBolt bolt = MakeBoltPrefab(plasma);
            WeaponDefinition plasmaPistol = MakePlasmaWeapon("Plasma Pistol", 20f, 1.7f, 2, 0.55f, 17f, false, bolt, plasma);
            WeaponDefinition plasmaRifle = MakePlasmaWeapon("Plasma Rifle", 12f, 1.25f, 3, 0.13f, 22f, true, bolt, plasmaRifleColor);
            AssignApprovedAudio(plasmaRifle, "PlasmaRifleSingle", null);
            ConfigureFeedback(rifleData, 1.1f, 0.25f, 0.045f, 3f);
            ConfigureFeedback(shotgunData, 3.3f, 0.45f, 0.09f, 7f);
            ConfigureFeedback(huntingRifle, 4f, 0.25f, 0.1f, 8f);
            ConfigureFeedback(plasmaPistol, 0.5f, 0.15f, 0.03f, 2f);
            ConfigureFeedback(plasmaRifle, 0.45f, 0.2f, 0.025f, 1.5f);
            foreach (WeaponDefinition definition in new[] { rifleData, shotgunData, huntingRifle, plasmaPistol, plasmaRifle })
                WeaponModelGeometry.AssignWorldPrefab(definition);
            GrenadeExplosionVisual explosion = MakeGrenadeExplosionPrefab();
            GrenadeDefinition fragData = MakeGrenade("Frag", GrenadeKind.Frag, human, explosion);
            if (fragData.ExplosionSound == null) fragData.ExplosionSound = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Harvest/Audio/Approved/FragDeepBlast.ogg");
            EditorUtility.SetDirty(fragData);
            GrenadeDefinition plasmaGrenadeData = MakeGrenade("Plasma", GrenadeKind.Plasma, plasma, explosion);
            CreateGrenadePickup(new Vector3(-2f, 0.55f, -18f), GrenadeKind.Frag, human);
            CreateGrenadePickup(new Vector3(2f, 0.55f, -18f), GrenadeKind.Plasma, plasma);
            CreateGrenadePickup(new Vector3(4f, 0.55f, 3f), GrenadeKind.Frag, human);
            CreateGrenadePickup(new Vector3(-4f, 0.55f, 14f), GrenadeKind.Plasma, plasma);
            DroppedWeapon dropPrefab = MakeDropPrefab();
            GameObject huntingSupply = (GameObject)PrefabUtility.InstantiatePrefab(dropPrefab.gameObject);
            huntingSupply.name = "Farmhouse hunting rifle supply";
            FarmhouseLayout farmhouseLayout = AssetDatabase.LoadAssetAtPath<FarmhouseLayout>(FarmhouseFoundationBuilder.LayoutPath);
            huntingSupply.transform.position = farmhouseLayout.WorldOrigin + new Vector3(farmhouseLayout.HalfWidth - 2f, farmhouseLayout.UpperFloorTop + 0.55f, 1f);
            huntingSupply.AddComponent<WeaponSupply>().Definition = huntingRifle;
            CovenantEnemy gruntPrefab = MakeEnemyPrefab<GruntBehavior>("Grunt", grunt, plasmaPistol, dropPrefab, 48, 0, 2.6f, 12f, 0.9f);
            CovenantEnemy jackalPrefab = MakeEnemyPrefab<JackalBehavior>("Jackal", jackal, plasmaPistol, dropPrefab, 75, 85, 2.3f, 16f, 0.9f);
            CovenantEnemy brutePrefab = MakeEnemyPrefab<BruteBehavior>("Brute", brute, plasmaRifle, dropPrefab, 280, 0, 4.2f, 1.9f, 1.6f);
            EncounterDefinition encounterData = MakeEncounter(gruntPrefab, jackalPrefab, brutePrefab);
            AlliedMarine allyPrefab = MakeMarinePrefab(human, rifleData, dropPrefab);
            SpawnMarine(allyPrefab, "Cpl. Ortiz", new Vector3(-5f, 0.05f, -17f));
            SpawnMarine(allyPrefab, "Pvt. Chen", new Vector3(5f, 0.05f, -17f));
            SpawnMarine(allyPrefab, "Pvt. Doss", new Vector3(0f, 0.05f, -19f));

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
            ConfigureFootsteps(player);
            player.GetComponent<CombatTarget>().Team = CombatTeam.Marine;
            MarineCombatActions actions = player.AddComponent<MarineCombatActions>();
            GrenadeInventory grenades = player.GetComponent<GrenadeInventory>();
            grenades.Frag = fragData;
            grenades.Plasma = plasmaGrenadeData;
            GameObject view = new GameObject("Eyes");
            view.transform.SetParent(player.transform, false);
            view.transform.localPosition = new Vector3(0, 0.65f, 0);
            Camera camera = view.AddComponent<Camera>();
            camera.fieldOfView = 75f;
            camera.tag = "MainCamera";
            view.AddComponent<AudioListener>();
            SuppressionScreenBlur blur = view.AddComponent<SuppressionScreenBlur>();
            blur.State = player.GetComponent<Suppression>();
            blur.BlurShader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/Harvest/Shaders/SuppressionBlur.shader");
            marine.View = camera;
            loadout.View = camera;
            actions.View = camera;
            PrecisionAim precisionAim = player.AddComponent<PrecisionAim>();
            precisionAim.View = camera;
            player.AddComponent<WeaponRecoil>();
            PlayerDamageFeedback feedback = view.AddComponent<PlayerDamageFeedback>();
            feedback.Armor = marineArmor;
            feedback.View = camera;
            GameObject rifle = WeaponModelGeometry.Create(rifleData, view.transform);
            rifle.transform.localPosition = new Vector3(0.36f, -0.33f, 0.7f);
            GameObject shotgun = WeaponModelGeometry.Create(shotgunData, view.transform);
            shotgun.transform.localPosition = new Vector3(0.36f, -0.35f, 0.62f);
            GameObject pistolModel = WeaponModelGeometry.Create(plasmaPistol, view.transform);
            pistolModel.transform.localPosition = new Vector3(0.33f, -0.32f, 0.6f);
            GameObject rifleModel = WeaponModelGeometry.Create(plasmaRifle, view.transform);
            rifleModel.transform.localPosition = new Vector3(0.36f, -0.32f, 0.75f);
            GameObject hunterModel = WeaponModelGeometry.Create(huntingRifle, view.transform);
            hunterModel.transform.localPosition = new Vector3(0.32f, -0.3f, 0.65f);
            WeaponView weaponView = view.AddComponent<WeaponView>();
            weaponView.Loadout = loadout;
            weaponView.Melee = player.GetComponent<MeleeAttack>();
            weaponView.Definitions = new[] { rifleData, shotgunData, plasmaPistol, plasmaRifle, huntingRifle };
            weaponView.Models = new[] { rifle, shotgun, pistolModel, rifleModel, hunterModel };

            GameObject director = new GameObject("Encounter Director");
            Material combatMaterial = MakeTracerMaterial();
            director.AddComponent<HitscanTracerRenderer>().TracerMaterial = combatMaterial;
            director.AddComponent<CombatEffects>().EffectMaterial = combatMaterial;
            CombatAudio audio = director.AddComponent<CombatAudio>();
            audio.Player = loadout;
            audio.PlasmaWorldImpacts = new[] {
                AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Harvest/Audio/Approved/PlasmaWorldImpact2.ogg"),
                AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Harvest/Audio/Approved/PlasmaWorldImpact3.ogg") };
            audio.Wind = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Harvest/Audio/Approved/FieldWind.ogg");
            HarvestEncounter encounter = director.AddComponent<HarvestEncounter>();
            encounter.Marine = marine;
            encounter.Definition = encounterData;
            encounter.EvacuationPad = evacuation.transform;
            HarvestHud hud = director.AddComponent<HarvestHud>();
            hud.Marine = marine;
            hud.Armor = marineArmor;
            hud.Loadout = loadout;
            hud.Encounter = encounter;

            FarmVisualPass.Build(camera);

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
            if (weapon != null)
            {
                if (name == "Plasma Pistol" && !weapon.SupportsCharge)
                {
                    weapon.SupportsCharge = true;
                    EditorUtility.SetDirty(weapon);
                }
                return weapon;
            }
            weapon = ScriptableObject.CreateInstance<WeaponDefinition>();
            AssetDatabase.CreateAsset(weapon, path);
            weapon.DisplayName = name.ToUpperInvariant();
            weapon.ShotKind = WeaponShotKind.PlasmaBolt;
            weapon.UsesEnergy = true;
            weapon.EnergyCapacity = 100;
            weapon.SupportsCharge = name == "Plasma Pistol";
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
            root.AddComponent<CombatTarget>().Team = CombatTeam.Covenant;
            root.AddComponent<Suppression>();
            EnsurePrecisionRegion(root, isBrute, material);
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
            bool changed = EnsurePrecisionRegion(root, typeof(T) == typeof(BruteBehavior), root.GetComponentInChildren<Renderer>()?.sharedMaterial);
            CombatTarget target = root.GetComponent<CombatTarget>();
            if (target == null) { target = root.AddComponent<CombatTarget>(); changed = true; }
            if (target.Team != CombatTeam.Covenant) { target.Team = CombatTeam.Covenant; changed = true; }
            if (root.GetComponent<Suppression>() == null) { root.AddComponent<Suppression>(); changed = true; }
            ActorWeapon enemyWeapon = root.GetComponent<ActorWeapon>();
            if (enemyWeapon == null)
            {
                enemyWeapon = root.AddComponent<ActorWeapon>();
                changed = true;
            }
            if (enemyWeapon.Team != CombatTeam.Covenant) { enemyWeapon.Team = CombatTeam.Covenant; changed = true; }
            if (enemyWeapon.StartingWeapon == null) { enemyWeapon.StartingWeapon = weapon; changed = true; }
            if (enemyWeapon.DropPrefab == null) { enemyWeapon.DropPrefab = dropPrefab; changed = true; }
            if (root.transform.Find("Held weapon") == null || root.transform.Find("Held weapon/Receiver") == null)
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
            Transform existing = root.transform.Find("Held weapon");
            if (existing != null) Object.DestroyImmediate(existing.gameObject);
            GameObject model = definition.WorldModel != null ?
                (GameObject)PrefabUtility.InstantiatePrefab(definition.WorldModel, root.transform) :
                WeaponModelGeometry.Create(definition, root.transform, false);
            model.name = "Held weapon";
            model.transform.localPosition = isBrute ? new Vector3(0.85f, 0.25f, 0.55f) : new Vector3(0.45f, 0f, 0.5f);
            model.transform.localScale = Vector3.one * (isBrute ? 1.1f : 1f);
        }

        static bool EnsurePrecisionRegion(GameObject root, bool isBrute, Material material)
        {
            bool changed = false;
            PrecisionHitRegion region = root.GetComponent<PrecisionHitRegion>();
            if (region == null) { region = root.AddComponent<PrecisionHitRegion>(); region.AllowsInstantHeadshot = !isBrute; changed = true; }
            if (root.transform.Find("Head silhouette") == null)
            {
                CharacterController controller = root.GetComponent<CharacterController>();
                GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                head.name = "Head silhouette";
                head.transform.SetParent(root.transform, false);
                head.transform.localPosition = controller.center + Vector3.up * (controller.height * 0.34f);
                head.transform.localScale = Vector3.one * (isBrute ? 0.65f : 0.5f);
                if (material != null) head.GetComponent<Renderer>().sharedMaterial = material;
                Object.DestroyImmediate(head.GetComponent<Collider>());
                changed = true;
            }
            return changed;
        }
        static void ConfigureFootsteps(GameObject actor)
        {
            FootstepAudio steps = actor.AddComponent<FootstepAudio>();
            steps.AudioMixRevision = 1;
            steps.Gravel = new AudioClip[4]; steps.Wood = new AudioClip[5];
            for (int i = 0; i < steps.Gravel.Length; i++)
                steps.Gravel[i] = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Harvest/Audio/Approved/FootstepGravel" + (i + 1) + ".ogg");
            for (int i = 0; i < steps.Wood.Length; i++)
                steps.Wood[i] = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Harvest/Audio/Approved/FootstepWood" + (i + 1) + ".ogg");
        }
        static void AssignApprovedAudio(WeaponDefinition definition, string fire, string reload)
        {
            if (definition.FireSound == null) definition.FireSound = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Harvest/Audio/Approved/" + fire + ".ogg");
            if (reload != null && definition.ReloadSound == null)
                definition.ReloadSound = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Harvest/Audio/Approved/" + reload + ".ogg");
            EditorUtility.SetDirty(definition);
        }
        static void ConfigureFeedback(WeaponDefinition definition, float pitch, float yaw, float distance, float rotation)
        {
            if (definition.FeedbackRevision >= 1) return;
            definition.RecoilPitch = pitch; definition.RecoilYaw = yaw;
            definition.ModelKickDistance = distance; definition.ModelKickDegrees = rotation;
            definition.FeedbackRevision = 1;
            EditorUtility.SetDirty(definition);
        }
        static WeaponDefinition MakeHuntingRifle(Material stock)
        {
            const string path = "Assets/Harvest/Data/Hunting Rifle.asset";
            WeaponDefinition definition = AssetDatabase.LoadAssetAtPath<WeaponDefinition>(path);
            if (definition != null) return definition;
            definition = ScriptableObject.CreateInstance<WeaponDefinition>();
            definition.DisplayName = "HUNTING RIFLE";
            definition.IsPrecision = true;
            definition.Automatic = false;
            definition.MagazineSize = 5;
            definition.StartingReserve = 20;
            definition.DamagePerPellet = 90f;
            definition.FireInterval = 1.4f;
            definition.ReloadSeconds = 3.2f;
            definition.SpreadDegrees = 1.8f;
            definition.Range = 150f;
            definition.AdsSpreadDegrees = 0f;
            definition.AdsFieldOfView = 42f;
            definition.AdsModelPosition = new Vector3(0f, -0.085f, 0.65f);
            definition.PickupMaterial = stock;
            AssetDatabase.CreateAsset(definition, path);
            return definition;
        }
        static GameObject MakeHuntingModel(Transform view, Material stock, Material metal)
        {
            GameObject root = new GameObject("Civilian bolt-action hunting rifle");
            root.transform.SetParent(view, false);
            root.transform.localPosition = new Vector3(0.32f, -0.3f, 0.65f);
            HuntingPart(root.transform, "Wood stock", stock, new Vector3(0f, -0.035f, -0.1f), new Vector3(0.1f, 0.12f, 0.65f));
            HuntingPart(root.transform, "Barrel", metal, new Vector3(0f, 0.02f, 0.4f), new Vector3(0.04f, 0.04f, 0.65f));
            HuntingPart(root.transform, "Bolt", metal, new Vector3(0.065f, 0.025f, 0f), new Vector3(0.06f, 0.04f, 0.1f));
            HuntingPart(root.transform, "Front iron sight", metal, new Vector3(0f, 0.07f, 0.68f), new Vector3(0.015f, 0.035f, 0.015f));
            for (int side = -1; side <= 1; side += 2)
                HuntingPart(root.transform, "Rear sight", metal, new Vector3(side * 0.024f, 0.07f, 0.1f), new Vector3(0.015f, 0.035f, 0.03f));
            return root;
        }
        static void HuntingPart(Transform parent, string name, Material material, Vector3 position, Vector3 size)
        {
            GameObject part = MakeViewModel(name, material, parent, position, size);
        }

        static GrenadeDefinition MakeGrenade(string name, GrenadeKind kind, Material material, GrenadeExplosionVisual explosion)
        {
            PhysicsMaterial physics = MakeGrenadePhysics(name, kind);
            string path = $"Assets/Harvest/Data/{name} Grenade.asset";
            GrenadeDefinition definition = AssetDatabase.LoadAssetAtPath<GrenadeDefinition>(path);
            if (definition == null)
            {
                definition = ScriptableObject.CreateInstance<GrenadeDefinition>();
                definition.Kind = kind;
                if (kind == GrenadeKind.Plasma)
                {
                    definition.FuseSeconds = 4f;
                    definition.MaxDamage = 320f;
                    definition.DamageRadius = 5f;
                    definition.ShieldMultiplier = 2f;
                    definition.BlastColor = new Color(0.3f, 0.4f, 1f);
                }
                AssetDatabase.CreateAsset(definition, path);
            }
            if (definition.ExplosionPrefab == null) definition.ExplosionPrefab = explosion;
            if (definition.Prefab == null)
            {
                string prefabPath = $"Assets/Harvest/Prefabs/{name} Grenade.prefab";
                GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (existing != null) definition.Prefab = existing.GetComponent<GrenadeProjectile>();
                else
                {
                    GameObject root = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    root.name = name + " Grenade";
                    root.transform.localScale = Vector3.one * 0.24f;
                    root.GetComponent<Renderer>().sharedMaterial = material;
                    Rigidbody body = root.AddComponent<Rigidbody>();
                    body.mass = 0.4f;
                    body.linearDamping = 0.05f;
                    body.angularDamping = 0.1f;
                    body.interpolation = RigidbodyInterpolation.Interpolate;
                    body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                    root.GetComponent<SphereCollider>().sharedMaterial = physics;
                    root.AddComponent<GrenadeProjectile>().Definition = definition;
                    GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                    Object.DestroyImmediate(root);
                    definition.Prefab = saved.GetComponent<GrenadeProjectile>();
                }
            }
            EditorUtility.SetDirty(definition);
            return definition;
        }

        static PhysicsMaterial MakeGrenadePhysics(string name, GrenadeKind kind)
        {
            string path = $"Assets/Harvest/Materials/{name} Grenade Bounce.physicMaterial";
            PhysicsMaterial existing = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(path);
            if (existing != null)
            {
                if (kind == GrenadeKind.Frag)
                {
                    existing.bounciness = 0.22f;
                    existing.bounceCombine = PhysicsMaterialCombine.Average;
                    existing.dynamicFriction = 0.85f;
                    existing.staticFriction = 0.95f;
                    existing.frictionCombine = PhysicsMaterialCombine.Maximum;
                    EditorUtility.SetDirty(existing);
                }
                return existing;
            }
            PhysicsMaterial material = new PhysicsMaterial(name + " Grenade Bounce");
            material.bounciness = kind == GrenadeKind.Frag ? 0.22f : 0.1f;
            material.dynamicFriction = kind == GrenadeKind.Frag ? 0.85f : 0.45f;
            material.staticFriction = kind == GrenadeKind.Frag ? 0.95f : 0.5f;
            material.frictionCombine = kind == GrenadeKind.Frag ? PhysicsMaterialCombine.Maximum : PhysicsMaterialCombine.Average;
            material.bounceCombine = kind == GrenadeKind.Frag ? PhysicsMaterialCombine.Average : PhysicsMaterialCombine.Maximum;
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        static GrenadeExplosionVisual MakeGrenadeExplosionPrefab()
        {
            const string path = "Assets/Harvest/Prefabs/Grenade Explosion.prefab";
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing.GetComponent<GrenadeExplosionVisual>();
            const string materialPath = "Assets/Harvest/Materials/Grenade Blast.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                material.color = new Color(1f, 1f, 1f, 0.35f);
                HarvestRenderSetup.MakeTransparent(material);
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                material.SetInt("_ZWrite", 0);
                material.SetOverrideTag("RenderType", "Transparent");

                material.EnableKeyword("_EMISSION");
                material.renderQueue = 3000;
                AssetDatabase.CreateAsset(material, materialPath);
            }
            GameObject root = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            root.name = "Grenade Explosion";
            Object.DestroyImmediate(root.GetComponent<Collider>());
            root.GetComponent<Renderer>().sharedMaterial = material;
            GrenadeExplosionVisual effect = root.AddComponent<GrenadeExplosionVisual>();
            effect.Shell = root.GetComponent<Renderer>();
            effect.Flash = root.AddComponent<Light>();
            effect.Flash.type = LightType.Point;
            effect.Flash.intensity = 4f;
            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return saved.GetComponent<GrenadeExplosionVisual>();
        }

        static void CreateGrenadePickup(Vector3 position, GrenadeKind kind, Material material)
        {
            GameObject root = new GameObject(kind + " grenade supply");
            root.transform.position = position;
            SphereCollider trigger = root.AddComponent<SphereCollider>();
            trigger.radius = 0.9f;
            trigger.isTrigger = true;
            Rigidbody body = root.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            GrenadePickup pickup = root.AddComponent<GrenadePickup>();
            pickup.Kind = kind;
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            visual.name = "Grenade supply visual";
            visual.transform.SetParent(root.transform, false);
            visual.transform.localScale = Vector3.one * 0.4f;
            visual.GetComponent<Renderer>().sharedMaterial = material;
            Object.DestroyImmediate(visual.GetComponent<Collider>());
            pickup.Visual = visual.transform;
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
            if (definition == null)
            {
                definition = ScriptableObject.CreateInstance<EncounterDefinition>();
                AssetDatabase.CreateAsset(definition, EncounterPath);
            }
            if (definition.Waves == null || definition.Waves.Length == 0)
            {
                definition.Waves = new[]
                {
                    new EncounterWave
                    {
                        Callout = "UNKNOWN HOSTILES — HOLD THE ROAD", DelaySeconds = 2f,
                        Enemies = new[] { Spawn(grunt, -6, 24), Spawn(grunt, 0, 27), Spawn(grunt, 6, 24) }
                    },
                    new EncounterWave
                    {
                        Callout = "HEAVY CONTACT — KEEP THEM OFF THE CHECKPOINT", DelaySeconds = 5f,
                        Enemies = new[] { Spawn(grunt, -8, 31), Spawn(grunt, 8, 31), Spawn(grunt, -3, 34),
                            Spawn(grunt, 3, 34), Spawn(jackal, -6, 36), Spawn(brute, 0, 38) }
                    }
                };
            }
            if (definition.PrototypeWaveRevision < 1)
            {
                var waves = new System.Collections.Generic.List<EncounterWave>(definition.Waves);
                waves.Add(new EncounterWave
                {
                    Callout = "SHIELD LINE ADVANCING — WATCH THE FLANKS", DelaySeconds = 7f,
                    Enemies = new[] { Spawn(grunt, -10, 30), Spawn(grunt, 10, 30), Spawn(grunt, -5, 35),
                        Spawn(grunt, 5, 35), Spawn(jackal, -7, 37), Spawn(jackal, 7, 37), Spawn(brute, 0, 42) }
                });
                waves.Add(new EncounterWave
                {
                    Callout = "BRUTES ON THE ROAD — FALL BACK TO COVER", DelaySeconds = 8f,
                    Enemies = new[] { Spawn(grunt, -10, 30), Spawn(grunt, 10, 30), Spawn(grunt, -4, 34),
                        Spawn(grunt, 4, 34), Spawn(jackal, -8, 38), Spawn(jackal, 8, 38),
                        Spawn(brute, -3, 43), Spawn(brute, 3, 43) }
                });
                waves.Add(new EncounterWave
                {
                    Callout = "FINAL ASSAULT — TRANSPORT INBOUND", DelaySeconds = 9f,
                    Enemies = new[] { Spawn(grunt, -10, 31), Spawn(grunt, 10, 31), Spawn(grunt, -5, 34),
                        Spawn(grunt, 5, 34), Spawn(grunt, 0, 36), Spawn(jackal, -8, 39), Spawn(jackal, 0, 40),
                        Spawn(jackal, 8, 39), Spawn(brute, -4, 44), Spawn(brute, 4, 44) }
                });
                definition.Waves = waves.ToArray();
                definition.PrototypeWaveRevision = 1;
            }
            EditorUtility.SetDirty(definition);
            return definition;
        }

        static void CreateCover(string name, Vector3 position, Vector3 peekOffset)
        {
            GameObject root = new GameObject(name + " cover slot");
            root.transform.position = position;
            root.AddComponent<CoverPoint>().PeekOffset = peekOffset;
        }

        static AlliedMarine MakeMarinePrefab(Material material, WeaponDefinition rifle, DroppedWeapon drop)
        {
            const string path = "Assets/Harvest/Prefabs/Allied Marine.prefab";
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null)
            {
                GameObject contents = PrefabUtility.LoadPrefabContents(path);
                bool changed = false;
                if (contents.GetComponent<Suppression>() == null) { contents.AddComponent<Suppression>(); changed = true; }
                if (contents.GetComponent<MeleeAttack>() == null) { contents.AddComponent<MeleeAttack>(); changed = true; }
                if (contents.GetComponent<FootstepAudio>() == null) { ConfigureFootsteps(contents); changed = true; }
                FootstepAudio steps = contents.GetComponent<FootstepAudio>();
                if (steps.AudioMixRevision < 1) { steps.Volume *= 0.6f; steps.AudioMixRevision = 1; changed = true; }
                if (contents.transform.Find("Held weapon/Receiver") == null)
                {
                    AddHeldWeapon(contents, rifle, false);
                    AlliedMarine ally = contents.GetComponent<AlliedMarine>();
                    ally.GunVisual = contents.transform.Find("Held weapon");
                    ally.GunVisual.localPosition = new Vector3(0.38f, 1.25f, 0.45f);
                    changed = true;
                }
                if (changed) PrefabUtility.SaveAsPrefabAsset(contents, path);
                PrefabUtility.UnloadPrefabContents(contents);
                return AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponent<AlliedMarine>();
            }
            GameObject root = new GameObject("Allied Marine");
            NavMeshAgent agent = root.AddComponent<NavMeshAgent>();
            agent.enabled = false; // Enabled in Start after the navigation surface is ready.
            agent.speed = 3.2f;
            agent.acceleration = 12f;
            agent.angularSpeed = 360f;
            agent.radius = 0.4f;
            agent.height = 1.9f;
            agent.stoppingDistance = 0.15f;
            CapsuleCollider collider = root.AddComponent<CapsuleCollider>();
            collider.height = 1.9f;
            collider.radius = 0.35f;
            collider.center = Vector3.up * 0.95f;
            root.AddComponent<Vitality>().MaxHealth = 100f;
            root.AddComponent<MarineArmor>().MaxArmor = 175f;
            CombatTarget target = root.AddComponent<CombatTarget>();
            target.Team = CombatTeam.Marine;
            target.AimOffset = Vector3.up * 1.45f;
            root.AddComponent<Suppression>();
            ActorWeapon weapon = root.AddComponent<ActorWeapon>();
            weapon.Team = CombatTeam.Marine;
            weapon.StartingWeapon = rifle;
            weapon.DropPrefab = drop;
            root.AddComponent<MeleeAttack>();
            AlliedMarine marine = root.AddComponent<AlliedMarine>();
            ConfigureFootsteps(root);
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.name = "Visual";
            visual.transform.SetParent(root.transform, false);
            visual.transform.localPosition = Vector3.up * 0.95f;
            visual.transform.localScale = new Vector3(0.7f, 0.95f, 0.7f);
            visual.GetComponent<Renderer>().sharedMaterial = material;
            Object.DestroyImmediate(visual.GetComponent<Collider>());
            marine.BodyVisual = visual.transform;
            AddHeldWeapon(root, rifle, false);
            marine.GunVisual = root.transform.Find("Held weapon");
            marine.GunVisual.localPosition = new Vector3(0.38f, 1.25f, 0.45f);
            marine.GunVisual.localScale = Vector3.one;
            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return saved.GetComponent<AlliedMarine>();
        }

        static void SpawnMarine(AlliedMarine prefab, string callSign, Vector3 position)
        {
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab.gameObject);
            instance.transform.position = position;
            AlliedMarine marine = instance.GetComponent<AlliedMarine>();
            marine.CallSign = callSign;
            marine.name = callSign;
        }

        static EnemySpawn Spawn(CovenantEnemy prefab, float x, float z)
        {
            return new EnemySpawn { Prefab = prefab, Position = new Vector3(x, prefab.GetComponent<CharacterController>().height / 2f, z) };
        }

        static Material MakeTracerMaterial()
        {
            const string path = "Assets/Harvest/Materials/Hitscan Tracer.mat";
            Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;
            Material material = new Material(Shader.Find("Harvest/Combat Unlit"));
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        static Material MakeMaterial(string name, Color color, bool glowing = false)
        {
            string path = $"Assets/Harvest/Materials/{name}.mat";
            Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
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

        static Material MakeSmokeMaterial()
        {
            const string path = "Assets/Harvest/Materials/Farm Smoke.mat";
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/Harvest/Shaders/Smoke.shader");
            if (shader == null || ShaderUtil.ShaderHasError(shader))
                throw new System.InvalidOperationException("Harvest smoke shader is missing or failed to compile. Check the Console shader errors.");
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader) { name = "Farm Smoke" };
                AssetDatabase.CreateAsset(material, path);
            }
            else if (material.shader != shader) { material.shader = shader; EditorUtility.SetDirty(material); }
            return material;
        }

        static void Smoke(Vector3 position)
        {
            GameObject objectWithSmoke = new GameObject("Smoke column");
            objectWithSmoke.transform.position = position;
            ParticleSystem particles = objectWithSmoke.AddComponent<ParticleSystem>();
            var renderer = particles.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = MakeSmokeMaterial();
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            var fade = particles.colorOverLifetime;
            fade.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.15f),
                    new GradientAlphaKey(0.65f, 0.7f), new GradientAlphaKey(0f, 1f) });
            fade.color = gradient;
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
