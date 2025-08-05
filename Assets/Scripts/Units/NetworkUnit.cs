using System;
using System.Collections;
using Cysharp.Threading.Tasks;
using TurnBasedStrategy.Core;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

namespace TurnBasedStrategy.Units
{
    public class NetworkUnit : NetworkBehaviour, IUnit
    {
        [Header("Unit Settings")]
        [SerializeField] private UnitType _unitType;
        [SerializeField] private GameObject _selectionIndicator;
        [SerializeField] private GameObject _attackRangeIndicator;
        [SerializeField] private GameObject _previewSelectionIndicator;
        [SerializeField] private LineRenderer _pathPreview;
        
        private NetworkVariable<int> _ownerId = new NetworkVariable<int>();
        private NetworkVariable<bool> _isAlive = new NetworkVariable<bool>(true);
        private NetworkVariable<Vector3> _networkPosition = new NetworkVariable<Vector3>();
        private NetworkVariable<bool> _isMoving = new NetworkVariable<bool>(false);
        private NetworkVariable<Vector3> _moveDestination = new NetworkVariable<Vector3>();
        
        private IMovementController _movementController;
        private ISelectionHandler _selectionHandler;
        private AttackRangeVisualizer _attackRangeVisualizer;
        private UnitSettings _settings;
        
        public int OwnerId => _ownerId.Value;
        public UnitType Type => _unitType;
        public float MoveSpeed => _settings?.MoveSpeed ?? 0f;
        public float AttackRange => _settings?.AttackRange ?? 0f;
        public Vector3 Position => transform.position;
        public bool IsAlive => _isAlive.Value;
        
        public event Action<NetworkUnit> OnUnitDestroyed;
        public event Action<NetworkUnit> OnUnitSelected;
        
        private void Awake()
        {
            _movementController = GetComponent<IMovementController>();
            _selectionHandler = GetComponent<ISelectionHandler>();
            _attackRangeVisualizer = GetComponent<AttackRangeVisualizer>();
            
            if (_attackRangeVisualizer == null)
            {
                _attackRangeVisualizer = gameObject.AddComponent<AttackRangeVisualizer>();
            }
            
            CreatePreviewSelectionIndicator();
        }
        
        private void CreatePreviewSelectionIndicator()
        {
            if (_previewSelectionIndicator == null)
            {
                GameObject previewGO = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                previewGO.name = "PreviewSelectionIndicator";
                previewGO.transform.SetParent(transform);
                previewGO.transform.localPosition = new Vector3(0, -0.9f, 0);
                previewGO.transform.localScale = new Vector3(1.2f, 0.05f, 1.2f);
                
                Renderer renderer = previewGO.GetComponent<Renderer>();
                renderer.material = new Material(Shader.Find("Sprites/Default"));
                renderer.material.color = new Color(1, 1, 0, 0.5f);
                
                Destroy(previewGO.GetComponent<Collider>());
                
                _previewSelectionIndicator = previewGO;
                _previewSelectionIndicator.SetActive(false);
            }
        }
        
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            
            if (IsServer)
            {
                _networkPosition.Value = transform.position;
            }
            
            _networkPosition.OnValueChanged += OnPositionChanged;
            _isAlive.OnValueChanged += OnAliveStatusChanged;
            _isMoving.OnValueChanged += OnMovingStatusChanged;
            _moveDestination.OnValueChanged += OnDestinationChanged;
            
            if (IsOwner)
            {
                _selectionHandler?.Initialize(this);
            }
        }
        
        public override void OnNetworkDespawn()
        {
            _networkPosition.OnValueChanged -= OnPositionChanged;
            _isAlive.OnValueChanged -= OnAliveStatusChanged;
            _isMoving.OnValueChanged -= OnMovingStatusChanged;
            _moveDestination.OnValueChanged -= OnDestinationChanged;
            base.OnNetworkDespawn();
        }
        
        public void Initialize(int ownerId, UnitType type)
        {
            if (!IsServer) 
                return;
            
            _ownerId.Value = ownerId;
            _unitType = type;
            LoadUnitSettings();
        }
        
        public void TakeDamage()
        {
            if (!IsServer)
                return;
            
            _isAlive.Value = false;
            DespawnUnit();
        }
        
        [ServerRpc(RequireOwnership = false)]
        public void RequestMoveServerRpc(Vector3 targetPosition, ServerRpcParams rpcParams = default)
        {
            if (!ValidateOwnership(rpcParams.Receive.SenderClientId)) return;
            
            MoveToPosition(targetPosition).Forget();
        }
        
        [ServerRpc(RequireOwnership = false)]
        public void RequestAttackServerRpc(ulong targetUnitId, ServerRpcParams rpcParams = default)
        {
            if (!ValidateOwnership(rpcParams.Receive.SenderClientId)) return;
            
            var targetUnit = NetworkManager.Singleton.SpawnManager.SpawnedObjects[targetUnitId]
                ?.GetComponent<NetworkUnit>();
                
            if (targetUnit != null && targetUnit.IsAlive)
            {
                PerformAttack(targetUnit).Forget();
            }
        }
        
        private async UniTaskVoid MoveToPosition(Vector3 targetPosition)
        {
            _isMoving.Value = true;
            _moveDestination.Value = targetPosition;
            
            await _movementController.MoveTo(targetPosition);
            
            _networkPosition.Value = transform.position;
            _isMoving.Value = false;
        }
        
        private void Update()
        {
            if (IsServer && _isMoving.Value)
            {
                _networkPosition.Value = transform.position;
            }
            
            if (!IsServer && !IsOwner)
            {
                if (_isMoving.Value)
                {
                    float speed = MoveSpeed;
                    transform.position = Vector3.MoveTowards(transform.position, _networkPosition.Value, speed * Time.deltaTime);
                    
                    if (Vector3.Distance(transform.position, _moveDestination.Value) < 0.1f)
                    {
                        transform.position = _moveDestination.Value;
                    }
                }
                else
                {
                    transform.position = Vector3.Lerp(transform.position, _networkPosition.Value, Time.deltaTime * 10f);
                }
            }
        }
        
        private async UniTaskVoid PerformAttack(NetworkUnit target)
        {
            await UniTask.Delay(500);
            target.TakeDamage();
        }
        
        private void OnPositionChanged(Vector3 oldPos, Vector3 newPos)
        {
            if (!IsServer && !IsOwner && !_isMoving.Value)
            {
                transform.position = newPos;
            }
        }
        
        private void OnMovingStatusChanged(bool wasMoving, bool isMoving)
        {
            if (!IsServer && !IsOwner)
            {
                if (isMoving)
                {
                    var agent = GetComponent<NavMeshAgent>();
                    if (agent != null)
                    {
                        agent.enabled = false;
                    }
                }
            }
        }
        
        private void OnDestinationChanged(Vector3 oldDest, Vector3 newDest)
        {
            if (!IsServer && !IsOwner && _isMoving.Value)
            {
                var agent = GetComponent<NavMeshAgent>();
                if (agent != null)
                {
                    agent.enabled = false;
                }
            }
        }
        
        private void OnAliveStatusChanged(bool wasAlive, bool isAlive)
        {
            if (!isAlive)
            {
                OnUnitDestroyed?.Invoke(this);
                
                if (IsServer)
                {
                    StartCoroutine(DestroyUnitAfterDelay());
                }
                else
                {
                    gameObject.SetActive(false);
                }
            }
        }
        
        private IEnumerator DestroyUnitAfterDelay()
        {
            yield return new WaitForSeconds(0.5f);
            
            if (NetworkObject != null && NetworkObject.IsSpawned)
            {
                NetworkObject.Despawn(true);
            }
        }
        
        private bool ValidateOwnership(ulong senderId)
        {
            return IsServer && senderId == (ulong)_ownerId.Value;
        }
        
        private void LoadUnitSettings()
        {
            var gameSettings = Resources.Load<GameSettings>("GameSettings");
            _settings = _unitType == UnitType.SlowRanged 
                ? gameSettings.SlowRangedUnit 
                : gameSettings.FastMeleeUnit;
        }
        
        private void DespawnUnit()
        {
            if (IsServer)
            {
                if (NetworkObject != null && NetworkObject.IsSpawned)
                {
                    NetworkObject.Despawn(true);
                }
            }
        }
        
        public void SetSelected(bool selected)
        {
            _selectionIndicator?.SetActive(selected);
            
            if (_attackRangeVisualizer != null)
            {
                _attackRangeVisualizer.ShowRange(selected);
            }
            
            if (_attackRangeIndicator != null)
            {
                _attackRangeIndicator.SetActive(false);
            }
            
            if (selected)
            {
                OnUnitSelected?.Invoke(this);
            }
        }
        
        public void SetPreviewSelection(bool preview)
        {
            if (_previewSelectionIndicator != null)
            {
                _previewSelectionIndicator.SetActive(preview);
            }
        }
        
        public void ShowPathPreview(Vector3[] path)
        {
            if (_pathPreview == null) 
                return;
            
            _pathPreview.positionCount = path.Length;
            _pathPreview.SetPositions(path);
            _pathPreview.enabled = path.Length > 0;
            
            if (_attackRangeVisualizer != null && path.Length > 0)
            {
                _attackRangeVisualizer.UpdateRangePosition(path[path.Length - 1]);
            }
        }
        
        public void ResetAttackRangeIndicatorPosition()
        {
            if (_attackRangeVisualizer != null)
            {
                _attackRangeVisualizer.ResetRangePosition();
            }
            
            if (_attackRangeIndicator != null)
            {
                _attackRangeIndicator.transform.localPosition = Vector3.zero;
            }
        }
    }
}