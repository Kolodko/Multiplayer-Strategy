using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

namespace TurnBasedStrategy.Units
{
    [RequireComponent(typeof(NetworkUnit))]
    [RequireComponent(typeof(NavMeshAgent))]
    public class NetworkMovementSync : NetworkBehaviour
    {
        [Header("Sync Settings")]
        [SerializeField] private float _positionSyncRate = 0.1f;
        [SerializeField] private float _interpolationSpeed = 10f;
        [SerializeField] private float _teleportThreshold = 5f;
        
        private NavMeshAgent _agent;
        private NetworkUnit _unit;
        
        private float _lastSyncTime;
        private Vector3 _targetPosition;
        private bool _isInterpolating;
        
        private struct MovementState : INetworkSerializable
        {
            public Vector3 Position;
            public Vector3 Destination;
            public bool IsMoving;
            public float Speed;
            
            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref Position);
                serializer.SerializeValue(ref Destination);
                serializer.SerializeValue(ref IsMoving);
                serializer.SerializeValue(ref Speed);
            }
        }
        
        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            _unit = GetComponent<NetworkUnit>();
        }
        
        private void Start()
        {
            if (!IsOwner && !IsServer)
            {
                _agent.enabled = false;
            }
        }
        
        private void Update()
        {
            if (IsServer)
            {
                UpdateServerMovement();
            }
            else if (!IsOwner)
            {
                UpdateClientMovement();
            }
        }
        
        private void UpdateServerMovement()
        {
            if (Time.time - _lastSyncTime >= _positionSyncRate)
            {
                if (_agent.enabled && (_agent.hasPath || _agent.pathPending))
                {
                    var state = new MovementState
                    {
                        Position = transform.position,
                        Destination = _agent.destination,
                        IsMoving = true,
                        Speed = _agent.speed
                    };
                    
                    SyncMovementClientRpc(state);
                }
                else if (_isInterpolating)
                {
                    var state = new MovementState
                    {
                        Position = transform.position,
                        Destination = transform.position,
                        IsMoving = false,
                        Speed = 0f
                    };
                    
                    SyncMovementClientRpc(state);
                    _isInterpolating = false;
                }
                
                _lastSyncTime = Time.time;
            }
        }
        
        [ClientRpc]
        private void SyncMovementClientRpc(MovementState state)
        {
            if (IsOwner || IsServer) return;
            
            _targetPosition = state.Position;
            
            float distance = Vector3.Distance(transform.position, _targetPosition);
            
            if (distance > _teleportThreshold)
            {
                transform.position = _targetPosition;
            }
            else if (state.IsMoving)
            {
                _isInterpolating = true;
                
                if (!_agent.enabled)
                {
                    SimulateMovement(state.Destination, state.Speed);
                }
            }
            else
            {
                _isInterpolating = false;
            }
        }
        
        private void UpdateClientMovement()
        {
            if (_isInterpolating && _targetPosition != Vector3.zero)
            {
                transform.position = Vector3.Lerp(
                    transform.position, 
                    _targetPosition, 
                    Time.deltaTime * _interpolationSpeed
                );
                
                Vector3 lookDirection = (_targetPosition - transform.position).normalized;
                
                if (lookDirection != Vector3.zero)
                {
                    transform.rotation = Quaternion.LookRotation(lookDirection);
                }
            }
        }
        
        private void SimulateMovement(Vector3 destination, float speed)
        {
            Vector3 direction = (destination - transform.position).normalized;
            Vector3 movement = direction * speed * Time.deltaTime;
            
            if (Vector3.Distance(transform.position, destination) > 0.1f)
            {
                transform.position += movement;
                transform.rotation = Quaternion.LookRotation(direction);
            }
        }
        
        public void OnMovementStarted()
        {
            if (IsServer)
            {
                _isInterpolating = true;
            }
        }
        
        public void OnMovementCompleted()
        {
            if (IsServer)
            {
                var state = new MovementState
                {
                    Position = transform.position,
                    Destination = transform.position,
                    IsMoving = false,
                    Speed = 0f
                };
                
                SyncMovementClientRpc(state);
                _isInterpolating = false;
            }
        }
    }
}
