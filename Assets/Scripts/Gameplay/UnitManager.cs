using System.Collections.Generic;
using System.Linq;
using TurnBasedStrategy.Core;
using TurnBasedStrategy.Units;
using Unity.Netcode;
using UnityEngine;

namespace TurnBasedStrategy.Gameplay
{
    public class UnitManager : NetworkBehaviour
    {
        [Header("Unit Management")]
        [SerializeField] private GameSettings _gameSettings;
        
        private Dictionary<int, List<NetworkUnit>> _playerUnits = new Dictionary<int, List<NetworkUnit>>();
        
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            
            if (IsServer)
            {
                SpawnUnitsForPlayers();
            }
        }
        
        private void SpawnUnitsForPlayers()
        {
            _playerUnits[0] = new List<NetworkUnit>();
            _playerUnits[1] = new List<NetworkUnit>();
            
            SpawnPlayerUnits(0, _gameSettings.PlayerSpawnZones[0]);
            SpawnPlayerUnits(1, _gameSettings.PlayerSpawnZones[1]);
        }
        
        private void SpawnPlayerUnits(int playerId, SpawnZoneSettings spawnZone)
        {
            for (int i = 0; i < _gameSettings.SlowRangedUnit.StartingCount; i++)
            {
                var position = GetSpawnPosition(spawnZone, i, _gameSettings.SlowRangedUnit.StartingCount);
                var unit = SpawnUnit(_gameSettings.SlowRangedUnit.Prefab, position, playerId, UnitType.SlowRanged);
                _playerUnits[playerId].Add(unit);
            }
            
            for (int i = 0; i < _gameSettings.FastMeleeUnit.StartingCount; i++)
            {
                var position = GetSpawnPosition(spawnZone, i, _gameSettings.FastMeleeUnit.StartingCount);
                var unit = SpawnUnit(_gameSettings.FastMeleeUnit.Prefab, position, playerId, UnitType.FastMelee);
                _playerUnits[playerId].Add(unit);
            }
        }
        
        private Vector3 GetSpawnPosition(SpawnZoneSettings spawnZone, int index, int totalCount)
        {
            if (spawnZone.RandomizePositions)
            {
                float x = Random.Range(-spawnZone.Size.x / 2f, spawnZone.Size.x / 2f);
                float z = Random.Range(-spawnZone.Size.y / 2f, spawnZone.Size.y / 2f);
                
                return new Vector3(spawnZone.Center.x + x, 0, spawnZone.Center.y + z);
            }
            else
            {
                int cols = Mathf.CeilToInt(Mathf.Sqrt(totalCount));
                int row = index / cols;
                int col = index % cols;
                
                float spacing = 2f;
                float x = (col - cols / 2f) * spacing;
                float z = (row - totalCount / cols / 2f) * spacing;
                
                return new Vector3(spawnZone.Center.x + x, 0, spawnZone.Center.y + z);
            }
        }
        
        private NetworkUnit SpawnUnit(GameObject prefab, Vector3 position, int playerId, UnitType type)
        {
            var unitObject = Instantiate(prefab, position, Quaternion.identity);
            var networkObject = unitObject.GetComponent<NetworkObject>();
            networkObject.SpawnWithOwnership((ulong)playerId);
            
            var unit = unitObject.GetComponent<NetworkUnit>();
            unit.Initialize(playerId, type);
            
            return unit;
        }
        
        public List<NetworkUnit> GetPlayerUnits(int playerId)
        {
            if (!_playerUnits.ContainsKey(playerId))
                return new List<NetworkUnit>();
                
            return _playerUnits[playerId].Where(u => u != null && u.IsAlive).ToList();
        }
        
        public NetworkUnit GetUnit(ulong networkObjectId)
        {
            if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(networkObjectId, out var networkObject))
                return null;
                
            return networkObject.GetComponent<NetworkUnit>();
        }
    }
}
