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
        public int MarinesLeft => names.Length - marineIndex;
        public bool IsEvacuating => evacuating;
        public bool IsFinished { get; private set; }

        void OnEnable()
        {
            if (Marine != null) Marine.GetComponent<Vitality>().Died += MarineDied;
        }

        void OnDisable()
        {
            if (Marine != null) Marine.GetComponent<Vitality>().Died -= MarineDied;
        }

        void Start()
        {
            if (Definition == null || Definition.Waves == null || Definition.Waves.Length == 0)
            {
                Debug.LogError("The encounter needs at least one configured wave.", this);
                enabled = false;
                return;
            }
            Marine.TransferTo(names[0], MarineSpawn);
            StartCoroutine(BeginWave(0));
        }

        IEnumerator BeginWave(int index)
        {
            transitioning = true;
            EncounterWave wave = Definition.Waves[index];
            yield return new WaitForSeconds(wave.DelaySeconds);
            if (IsFinished) yield break;
            waveIndex = index;
            foreach (EnemySpawn entry in wave.Enemies)
            {
                if (entry == null || entry.Prefab == null) continue;
                CovenantEnemy enemy = Instantiate(entry.Prefab, entry.Position, Quaternion.identity);
                alive.Add(enemy);
            }
            transitioning = false;
            SetStatus(wave.Callout);
            if (alive.Count == 0) Debug.LogWarning("Wave has no configured enemies; the encounter cannot advance.", this);
        }

        public void EnemyKilled(CovenantEnemy enemy)
        {
            alive.Remove(enemy);
            StateChanged?.Invoke();
            if (alive.Count != 0 || transitioning || IsFinished) return;
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
                SetStatus("THE ROAD HAS FALLEN — PRESS PLAY TO RETRY");
                yield break;
            }
            Marine.TransferTo(names[marineIndex], MarineSpawn);
            SetStatus(evacuating ? Definition.EvacuationCallout : "HOLD THE ROAD");
        }

        void Update()
        {
            if (!evacuating || IsFinished || !Marine.Vitality.IsAlive) return;
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
