using System;
using UnityEngine;

namespace Harvest
{
    [Serializable]
    public sealed class EnemySpawn
    {
        public CovenantEnemy Prefab;
        public Vector3 Position;
    }

    [Serializable]
    public sealed class EncounterWave
    {
        public string Callout;
        public float DelaySeconds = 2f;
        public EnemySpawn[] Enemies;
    }

    [CreateAssetMenu(menuName = "Harvest/Encounter")]
    public sealed class EncounterDefinition : ScriptableObject
    {
        public EncounterWave[] Waves;
        public string EvacuationCallout = "REACH THE EVACUATION PAD";
    }
}
