using System.Collections.Generic;
using TurnBasedStrategy.Core;
using Unity.AI.Navigation;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using Random = System.Random;

namespace TurnBasedStrategy.Gameplay
{
    public class GameBoard : NetworkBehaviour, IGameBoard
    {
        [Header("Board Settings")]
        [SerializeField] private GameObject _groundPrefab;
        [SerializeField] private Transform _obstaclesParent;
        [SerializeField] private NavMeshSurface _navMeshSurface;
        
        private BoardGenerationSettings _currentSettings;
        private List<GameObject> _obstacles = new List<GameObject>();
        private Random _random;
        
        public Vector2Int BoardSize => _currentSettings?.Size ?? Vector2Int.zero;
        
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            
            if (IsServer)
            {
                var gameSettings = Resources.Load<GameSettings>("GameSettings");
                
                var settings = new BoardGenerationSettings
                {
                    Seed = UnityEngine.Random.Range(0, int.MaxValue),
                    Size = gameSettings.BoardSize,
                    Obstacles = gameSettings.Obstacles,
                    SpawnZones = gameSettings.PlayerSpawnZones
                };
                
                GenerateBoard(settings);
            }
        }
        
        public void GenerateBoard(BoardGenerationSettings settings)
        {
            if (!IsServer)
                return;
            
            _currentSettings = settings;
            _random = new Random(settings.Seed);
            
            GenerateGround();
            
            GenerateObstacles();
            
            if (_navMeshSurface != null)
            {
                _navMeshSurface.BuildNavMesh();
            }
            
            SendBoardDataToClients();
        }
        
        private void GenerateGround()
        {
            var networkObject = _groundPrefab.GetComponent<NetworkObject>();
            
            if (networkObject != null)
            {
                networkObject.Spawn();
            }
        }
        
        private void GenerateObstacles()
        {
            foreach (var obstacleSettings in _currentSettings.Obstacles)
            {
                int count = _random.Next(obstacleSettings.MinCount, obstacleSettings.MaxCount + 1);
                
                for (int i = 0; i < count; i++)
                {
                    var position = GetRandomPositionInArea(obstacleSettings.SpawnAreaMin, obstacleSettings.SpawnAreaMax);
                    var obstacle = Instantiate(obstacleSettings.Prefab, position, 
                        Quaternion.Euler(0, _random.Next(0, 360), 0), _obstaclesParent);
                    
                    _obstacles.Add(obstacle);
                    
                    var networkObject = obstacle.GetComponent<NetworkObject>();
                    
                    if (networkObject != null)
                    {
                        networkObject.Spawn();
                    }
                }
            }
            
            RebakeNavMesh();
        }
        
        private void RebakeNavMesh()
        {
            if (_navMeshSurface != null)
            {
                Debug.Log("Rebaking NavMesh after obstacles generation...");
                _navMeshSurface.BuildNavMesh();
            }
        }
        
        private Vector3 GetRandomPositionInArea(Vector2 min, Vector2 max)
        {
            float x = Mathf.Lerp(min.x, max.x, (float)_random.NextDouble());
            float z = Mathf.Lerp(min.y, max.y, (float)_random.NextDouble());
            
            return new Vector3(x, 1.40f, z);
        }
        
        public bool IsPositionValid(Vector3 position)
        {
            if (Mathf.Abs(position.x) > _currentSettings.Size.x / 2f ||
                Mathf.Abs(position.z) > _currentSettings.Size.y / 2f)
            {
                return false;
            }
            
            NavMeshHit hit;
            return NavMesh.SamplePosition(position, out hit, 1f, NavMesh.AllAreas);
        }
        
        public List<IUnit> GetUnitsInRange(Vector3 center, float range)
        {
            var units = new List<IUnit>();
            var colliders = Physics.OverlapSphere(center, range, LayerMask.GetMask("Unit"));
            
            foreach (var collider in colliders)
            {
                var unit = collider.GetComponent<IUnit>();
                
                if (unit != null && unit.IsAlive)
                {
                    units.Add(unit);
                }
            }
            
            return units;
        }
        
        private void SendBoardDataToClients()
        {
            var boardData = new BoardSyncData
            {
                Seed = _currentSettings.Seed,
                BoardSize = _currentSettings.Size
            };
            
            SyncBoardDataClientRpc(boardData);
        }
        
        [ClientRpc]
        private void SyncBoardDataClientRpc(BoardSyncData data)
        {
            if (IsServer)
                return;
            
            _currentSettings = new BoardGenerationSettings
            {
                Seed = data.Seed,
                Size = data.BoardSize
            };
        }
        
        private struct BoardSyncData : INetworkSerializable
        {
            public int Seed;
            public Vector2Int BoardSize;
            
            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref Seed);
                serializer.SerializeValue(ref BoardSize);
            }
        }
    }
}
