using System;
using System.Collections.Generic;
using UnityEngine;

namespace TurnBasedStrategy.Core
{
    [CreateAssetMenu(fileName = "GameSettings", menuName = "TurnBasedStrategy/GameSettings")]
    public class GameSettings : ScriptableObject
    {
        [Header("Turn Settings")]
        public float TurnDuration = 60f;
        public int DrawTurnNumber = 15;

        [Header("Unit Settings")]
        public UnitSettings SlowRangedUnit;
        public UnitSettings FastMeleeUnit;

        [Header("Board Settings")]
        public Vector2Int BoardSize = new Vector2Int(20, 20);
        public List<ObstacleSettings> Obstacles;
        public SpawnZoneSettings[] PlayerSpawnZones = new SpawnZoneSettings[2];
    }

    [Serializable]
    public class UnitSettings
    {
        public GameObject Prefab;
        public float MoveSpeed;
        public float AttackRange;
        public int StartingCount;
    }

    [Serializable]
    public class ObstacleSettings
    {
        public GameObject Prefab;
        public int MinCount;
        public int MaxCount;
        public Vector2 SpawnAreaMin;
        public Vector2 SpawnAreaMax;
    }

    [Serializable]
    public class SpawnZoneSettings
    {
        public Vector2 Center;
        public Vector2 Size;
        public bool RandomizePositions;
    }

    [Serializable]
    public class BoardGenerationSettings
    {
        public int Seed;
        public Vector2Int Size;
        public List<ObstacleSettings> Obstacles;
        public SpawnZoneSettings[] SpawnZones;
    }
}
