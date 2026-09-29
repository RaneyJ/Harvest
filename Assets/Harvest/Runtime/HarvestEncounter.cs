using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Harvest
{
    public sealed class HarvestEncounter : MonoBehaviour
    {
        public MarineController Marine;
        public Material GruntMaterial;
        public Material JackalMaterial;
        public Material BruteMaterial;
        public Material PlasmaMaterial;
        public Transform EvacuationPad;
        public Vector3 MarineSpawn = new Vector3(0f, 1.2f, -21f);

        readonly List<CovenantEnemy> alive = new List<CovenantEnemy>();
        readonly string[] names = { "Pvt. Vale", "Cpl. Neri", "Pvt. Solis" };
        int marineIndex;
        int wave;
        string message;
        bool transitioning;
        bool evacuating;
        public bool IsFinished { get; private set; }

        void Start()
        {
            Marine.ResetMarine(names[0], MarineSpawn);
            message = "HOLD THE ROAD";
            StartCoroutine(BeginWave(1, 2f));
        }

        IEnumerator BeginWave(int number, float delay)
        {
            transitioning = true;
            yield return new WaitForSeconds(delay);
            if (IsFinished) yield break;
            wave = number;
            if (number == 1)
            {
                Spawn(CovenantEnemy.Kind.Grunt, new Vector3(-6f, 1f, 24f));
                Spawn(CovenantEnemy.Kind.Grunt, new Vector3(0f, 1f, 27f));
                Spawn(CovenantEnemy.Kind.Grunt, new Vector3(6f, 1f, 24f));
                message = "UNKNOWN HOSTILES — HOLD THE ROAD";
            }
            else
            {
                Spawn(CovenantEnemy.Kind.Grunt, new Vector3(-8f, 1f, 31f));
                Spawn(CovenantEnemy.Kind.Grunt, new Vector3(8f, 1f, 31f));
                Spawn(CovenantEnemy.Kind.Grunt, new Vector3(-3f, 1f, 34f));
                Spawn(CovenantEnemy.Kind.Grunt, new Vector3(3f, 1f, 34f));
                Spawn(CovenantEnemy.Kind.Jackal, new Vector3(-6f, 1f, 36f));
                Spawn(CovenantEnemy.Kind.Brute, new Vector3(0f, 1.6f, 38f));
                message = "THE LINE IS BROKEN — CLEAR A PATH";
            }
            transitioning = false;
        }

        void Spawn(CovenantEnemy.Kind kind, Vector3 position)
        {
            bool brute = kind == CovenantEnemy.Kind.Brute;
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = brute ? "Brute" : kind == CovenantEnemy.Kind.Jackal ? "Jackal" : "Grunt";
            body.transform.position = position;
            body.transform.localScale = brute ? new Vector3(1.4f, 1.6f, 1.4f) : new Vector3(0.9f, 0.9f, 0.9f);
            body.GetComponent<Renderer>().sharedMaterial = brute ? BruteMaterial : kind == CovenantEnemy.Kind.Jackal ? JackalMaterial : GruntMaterial;
            Destroy(body.GetComponent<Collider>());
            CharacterController cc = body.AddComponent<CharacterController>();
            cc.height = brute ? 3.2f : 2f;
            cc.radius = brute ? 0.62f : 0.42f;
            CovenantEnemy enemy = body.AddComponent<CovenantEnemy>();
            enemy.Type = kind;
            enemy.Health = brute ? 280 : kind == CovenantEnemy.Kind.Jackal ? 75 : 48;
            enemy.Shield = kind == CovenantEnemy.Kind.Jackal ? 85 : 0;
            enemy.MoveSpeed = brute ? 4.2f : 2.6f;
            enemy.FireInterval = brute ? 1.2f : 1.8f;
            enemy.BoltMaterial = PlasmaMaterial;
            alive.Add(enemy);
        }

        public void EnemyKilled(CovenantEnemy enemy)
        {
            alive.Remove(enemy);
            if (alive.Count != 0 || transitioning || IsFinished) return;
            if (wave == 1) StartCoroutine(BeginWave(2, 4f));
            else
            {
                evacuating = true;
                message = "REACH THE EVACUATION PAD";
            }
        }

        public void MarineDied()
        {
            if (!IsFinished) StartCoroutine(TransferMarine());
        }

        IEnumerator TransferMarine()
        {
            message = $"{names[marineIndex]} — KIA";
            yield return new WaitForSeconds(2.5f);
            marineIndex++;
            if (marineIndex >= names.Length)
            {
                IsFinished = true;
                message = "THE ROAD HAS FALLEN — PRESS PLAY TO RETRY";
                yield break;
            }
            Marine.ResetMarine(names[marineIndex], MarineSpawn);
            message = evacuating ? "REACH THE EVACUATION PAD" : "HOLD THE ROAD";
        }

        void Update()
        {
            if (!evacuating || IsFinished || !Marine.IsAlive) return;
            if (Vector3.Distance(Marine.transform.position, EvacuationPad.position) < 3.5f)
            {
                IsFinished = true;
                message = "TRANSPORT AWAY — SURVIVORS ABOARD";
            }
        }

        void OnGUI()
        {
            GUI.color = Color.white;
            GUI.Box(new Rect(16, 16, 390, 86), $"{message}\n{Marine.CallSign}   |   MARINES LEFT: {names.Length - marineIndex}\nHOSTILES: {alive.Count}");
            GUI.Box(new Rect(16, Screen.height - 94, 220, 72), $"HEALTH  {Marine.Health} / {Marine.MaxHealth}\nRIFLE  {Marine.AmmoText}");
            if (!IsFinished && Marine.IsAlive)
            {
                float x = Screen.width * 0.5f;
                float y = Screen.height * 0.5f;
                GUI.color = Time.time < Marine.HitMarkerUntil ? Color.red : Color.white;
                GUI.Label(new Rect(x - 8f, y - 12f, 30f, 30f), "+");
            }
            GUI.color = Color.white;
            if (evacuating && !IsFinished)
                GUI.Box(new Rect(Screen.width - 260, 16, 244, 45), "EVAC PAD: GREEN BEACON BEHIND LINE");
        }
    }
}
