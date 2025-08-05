using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;

namespace TurnBasedStrategy.Units
{
    public class MovementController : MonoBehaviour, IMovementController
    {
        private NavMeshAgent _agent;
        private NetworkUnit _unit;
        private NetworkMovementSync _movementSync;
        
        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            _unit = GetComponent<NetworkUnit>();
            _movementSync = GetComponent<NetworkMovementSync>();
            
            if (_movementSync == null)
            {
                _movementSync = gameObject.AddComponent<NetworkMovementSync>();
            }
        }
        
        public async UniTask<bool> MoveTo(Vector3 destination)
        {
            if (_agent == null || !_agent.enabled) 
                return false;
            
            var path = GetPath(destination);
            if (path == null || path.Count == 0)
                return false;
            
            var totalDistance = CalculatePathDistance(path);
            if (totalDistance > _unit.MoveSpeed)
            {
                Debug.Log($"Path too long! Distance: {totalDistance}, Max: {_unit.MoveSpeed}");
                return false;
            }
            
            _agent.SetDestination(destination);
            _movementSync?.OnMovementStarted();
            
            while (_agent.pathPending || _agent.remainingDistance > 0.1f)
            {
                await UniTask.Yield();
                
                if (_agent == null || !_agent.enabled)
                    return false;
            }
            
            _movementSync?.OnMovementCompleted();
            
            return true;
        }
        
        public List<Vector3> GetPath(Vector3 destination)
        {
            var path = new NavMeshPath();
            
            if (!NavMesh.CalculatePath(transform.position, destination, NavMesh.AllAreas, path))
                return null;
                
            return path.corners.ToList();
        }
        
        private float CalculatePathDistance(List<Vector3> path)
        {
            float distance = 0f;
            
            for (int i = 0; i < path.Count - 1; i++)
            {
                distance += Vector3.Distance(path[i], path[i + 1]);
            }
            return distance;
        }
    }
}
