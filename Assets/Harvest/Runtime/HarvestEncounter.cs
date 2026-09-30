using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Harvest
{
    public sealed class HarvestEncounter : MonoBehaviour
    {
        public MarineController Marine;
        public EncounterDefinition Definition;
        public Transform EvacuationPad;
        public Vector3 MarineSpawn = new Vector3(0f, 1.2f, -21f);
        public event Action StateChanged;

        readonly List<CovenantEnemy> alive = new List<CovenantEnemy>();
        readonly string[] names = { "Pvt. Vale", "Cpl. Neri", "Pvt. Solis" };
        int marineIndex;
        int waveIndex;
        bool transitioning;
        bool evacuating;

        public string Status { get; private set; } = "HOLD THE ROAD";
        public int Hostiles => alive.Count;
        public int AlliesAlive
        {
            get
            {
                int count = 0;
                foreach (CombatTarget target in CombatTarget.Active)
                    if (target != null && target.Team == CombatTeam.Marine && target.IsAlive &&
                        target.GetComponent<AlliedMarine>() != null) count++;
                return count;
            }
        }
        public int MarinesLeft => names.Length - marineIndex;
        public int WaveNumber => Mathf.Min(waveIndex + 1, Definition != null && Definition.Waves != null ? Definition.Waves.Length : 0);
        public int WaveCount => Definition != null && Definition.Waves != null ? Definition.Waves.Length : 0;
        public bool IsBetweenWaves => transitioning;
        public float NextWaveIn => transitioning ? Mathf.Max(0f, nextWaveAt - Time.time) : 0f;
        float nextWaveAt;
        public bool IsEvacuating => evacuating;
        public bool IsFinished { get; private set; }

        void OnEnable()
        {
            if (Marine != null && Marine.GetComponent<Vitality>() != null)
                Marine.GetComponent<Vitality>().Died += MarineDied;
        }

        void OnDisable()
        {
            if (Marine != null && Marine.GetComponent<Vitality>() != null)
                Marine.GetComponent<Vitality>().Died -= MarineDied;
        }

        void Start()
        {
            if (Definition == null || Definition.Waves == null || Definition.Waves.Length == 0)
            {
                Debug.LogError("The Line scene needs updating. Stop Play mode and choose Harvest > Build The Line Prototype to create and assign encounter waves.", this);
                enabled = false;
                return;
            }
            if (Marine == null || Marine.GetComponent<MarineArmor>() == null)
            {
                Debug.LogError("The Line scene needs the marine armor update. Stop Play mode and choose Harvest > Build The Line Prototype.", this);
                enabled = false;
                return;
            }
            Marine.TransferTo(names[0], MarineSpawn);
            StartCoroutine(BeginWave(0));
        }

        IEnumerator BeginWave(int index)
        {
            transitioning = true;
            waveIndex = index;
            EncounterWave wave = Definition.Waves[index];
            float delay = wave != null ? Mathf.Max(0f, wave.DelaySeconds) : 0f;
            nextWaveAt = Time.time + delay;
            SetStatus(index == 0 ? "DEFEND THE CHECKPOINT" : "WAVE CLEARED — REGROUP AND RESUPPLY");
            yield return new WaitForSeconds(delay);
            if (IsFinished) yield break;
            waveIndex = index;
            foreach (EnemySpawn entry in wave != null && wave.Enemies != null ? wave.Enemies : Array.Empty<EnemySpawn>())
            {
                if (entry == null || entry.Prefab == null) continue;
                CovenantEnemy enemy = Instantiate(entry.Prefab, entry.Position, Quaternion.identity);
                alive.Add(enemy);
            }
            transitioning = false;
            SetStatus(wave != null ? wave.Callout : "HOLD THE ROAD");
            if (alive.Count == 0)
            {
                Debug.LogWarning("Skipping an empty encounter wave.", this);
                AdvanceWave();
            }
        }

        public void EnemyKilled(CovenantEnemy enemy)
        {
            alive.Remove(enemy);
            foreach (CovenantEnemy survivor in alive)
                if (survivor != null) survivor.GetComponent<GruntBehavior>()?.WitnessAllyDeath(enemy);
            StateChanged?.Invoke();
            if (alive.Count != 0 || transitioning || IsFinished) return;
            AdvanceWave();
        }

        void AdvanceWave()
        {
            if (IsFinished || transitioning) return;
            if (waveIndex + 1 < Definition.Waves.Length)
                StartCoroutine(BeginWave(waveIndex + 1));
            else
            {
                evacuating = true;
                SetStatus(Definition.EvacuationCallout);
            }
        }

        void MarineDied()
        {
            if (!IsFinished) StartCoroutine(TransferMarine());
        }

        IEnumerator TransferMarine()
        {
            SetStatus($"{names[marineIndex]} — KIA");
            yield return new WaitForSeconds(2.5f);
            marineIndex++;
            if (marineIndex >= names.Length)
            {
                IsFinished = true;
                SetStatus("THE ROAD HAS FALLEN");
                yield break;
            }
            Marine.TransferTo(names[marineIndex], MarineSpawn);
            SetStatus(evacuating ? Definition.EvacuationCallout : "HOLD THE ROAD");
        }

        void Update()
        {
            if (!evacuating || IsFinished || Marine == null || EvacuationPad == null || !Marine.Vitality.IsAlive) return;
            if (Vector3.Distance(Marine.transform.position, EvacuationPad.position) < 3.5f)
            {
                IsFinished = true;
                SetStatus("TRANSPORT AWAY — SURVIVORS ABOARD");
            }
        }

        void SetStatus(string value)
        {
            Status = value;
            StateChanged?.Invoke();
        }
    }
}
