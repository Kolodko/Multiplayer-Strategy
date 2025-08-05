using System.Collections.Generic;
using TurnBasedStrategy.Gameplay;
using TurnBasedStrategy.Units;
using Unity.Netcode;
using UnityEngine;

namespace TurnBasedStrategy.Advanced
{
    public class NetworkOptimizations : NetworkBehaviour
    {
        [Header("Optimization Settings")]
        [SerializeField] private float _visibilityRadius = 15f;
        [SerializeField] private float _updateInterval = 0.1f;
        
        private Dictionary<ulong, HashSet<NetworkObject>> _clientVisibleObjects = new();
        private SpatialHashGrid _spatialGrid;
        
        private void Start()
        {
            if (!IsServer)
                return;
            
            _spatialGrid = new SpatialHashGrid(2f);
            InvokeRepeating(nameof(UpdateClientVisibility), 0f, _updateInterval);
        }
        
        private void UpdateClientVisibility()
        {
            if (!IsServer) 
                return;
            
            foreach (var client in NetworkManager.ConnectedClientsList)
            {
                UpdateClientVisibleObjects(client.ClientId);
            }
        }
        
        private void UpdateClientVisibleObjects(ulong clientId)
        {
            if (!_clientVisibleObjects.ContainsKey(clientId))
            {
                _clientVisibleObjects[clientId] = new HashSet<NetworkObject>();
            }
            
            var visibleObjects = new HashSet<NetworkObject>();
            var playerUnits = GetPlayerUnits((int)clientId);
            
            foreach (var unit in playerUnits)
            {
                var nearbyObjects = _spatialGrid.GetObjectsInRadius(unit.Position, _visibilityRadius);
                
                foreach (var obj in nearbyObjects)
                {
                    visibleObjects.Add(obj);
                }
            }
            
            var toHide = new HashSet<NetworkObject>(_clientVisibleObjects[clientId]);
            toHide.ExceptWith(visibleObjects);
            
            foreach (var obj in toHide)
            {
                obj.NetworkHide(clientId);
            }
            
            var toShow = new HashSet<NetworkObject>(visibleObjects);
            toShow.ExceptWith(_clientVisibleObjects[clientId]);
            
            foreach (var obj in toShow)
            {
                obj.NetworkShow(clientId);
            }
            
            _clientVisibleObjects[clientId] = visibleObjects;
        }
        
        private List<NetworkUnit> GetPlayerUnits(int playerId)
        {
            var unitManager = FindObjectOfType<UnitManager>();
            return unitManager?.GetPlayerUnits(playerId) ?? new List<NetworkUnit>();
        }
    }
}
