using System.Collections.Generic;
using System.Linq;
using TurnBasedStrategy.Core;
using TurnBasedStrategy.Gameplay;
using TurnBasedStrategy.Network;
using TurnBasedStrategy.Units;
using UnityEngine;
using UnityEngine.EventSystems;

namespace TurnBasedStrategy.Input
{
    public class MultiUnitSelectionHandler : MonoBehaviour, IMultiUnitSelectionHandler
    {
        [Header("Selection Settings")]
        [SerializeField] private LayerMask _groundLayer = 1 << 8;
        [SerializeField] private LayerMask _unitLayer = 1 << 9;
        
        [Header("Debug")]
        [SerializeField] private List<NetworkUnit> _selectedUnits = new List<NetworkUnit>();
        
        private ITurnManager _turnManager;
        private INetworkGameManager _networkManager;
        private Camera _mainCamera;
        private RTSSelectionBox _selectionBox;
        
        private float _lastRightClickTime;
        private const float DOUBLE_CLICK_TIME = 0.3f;
        
        public NetworkUnit SelectedUnit => _selectedUnits.Count > 0 ? _selectedUnits[0] : null;
        public List<NetworkUnit> SelectedUnits => _selectedUnits;
        
        private void Awake()
        {
            _mainCamera = Camera.main;
            _selectionBox = GetComponent<RTSSelectionBox>() ?? gameObject.AddComponent<RTSSelectionBox>();
        }
        
        private void Start()
        {
            _turnManager = FindObjectOfType<NetworkTurnManager>();
            _networkManager = FindObjectOfType<NetworkGameManager>();
        }
        
        private void Update()
        {
            if (_networkManager == null || _turnManager == null) return;
            
            if (_networkManager.LocalPlayerId != _turnManager.CurrentPlayerId)
            {
                return;
            }
            
            HandleMouseInput();
            HandleKeyboardShortcuts();
        }
        
        private void HandleMouseInput()
        {
            if (UnityEngine.Input.GetMouseButtonDown(1) && !EventSystem.current.IsPointerOverGameObject())
            {
                float timeSinceLastClick = Time.time - _lastRightClickTime;
                
                if (timeSinceLastClick <= DOUBLE_CLICK_TIME)
                {
                    HandleDoubleRightClick();
                }
                else
                {
                    HandleSingleRightClick();
                }
                
                _lastRightClickTime = Time.time;
            }
        }
        
        private void HandleKeyboardShortcuts()
        {
            if (UnityEngine.Input.GetKeyDown(KeyCode.A) && UnityEngine.Input.GetKey(KeyCode.LeftControl))
            {
                SelectAllOwnedUnits();
            }
            
            if (UnityEngine.Input.GetKeyDown(KeyCode.Escape))
            {
                DeselectAllUnits();
            }
        }
        
        private void HandleSingleRightClick()
        {
            if (_selectedUnits.Count == 0) return;
            
            var ray = _mainCamera.ScreenPointToRay(UnityEngine.Input.mousePosition);
            
            if (Physics.Raycast(ray, out RaycastHit unitHit, 100f, _unitLayer))
            {
                var targetUnit = unitHit.collider.GetComponent<NetworkUnit>();
                
                if (targetUnit != null && targetUnit.OwnerId != _networkManager.LocalPlayerId && targetUnit.IsAlive)
                {
                    if (_turnManager.CanAttack && _selectedUnits.Count == 1)
                    {
                        var attacker = _selectedUnits[0];
                        var distance = Vector3.Distance(attacker.Position, targetUnit.Position);
                        
                        if (distance <= attacker.AttackRange)
                        {
                            RequestAttack(attacker, targetUnit);
                        }
                        else
                        {
                            Debug.Log("Target out of range!");
                        }
                    }
                    return;
                }
            }
            
            if (Physics.Raycast(ray, out RaycastHit groundHit, 100f, _groundLayer))
            {
                if (_turnManager.CanMove)
                {
                    ShowPathPreview(groundHit.point);
                }
            }
        }
        
        private void HandleDoubleRightClick()
        {
            if (_selectedUnits.Count != 1 || !_turnManager.CanMove) return;
            
            var ray = _mainCamera.ScreenPointToRay(UnityEngine.Input.mousePosition);
            
            if (Physics.Raycast(ray, out RaycastHit hit, 100f, _groundLayer))
            {
                RequestMovement(_selectedUnits[0], hit.point);
            }
        }
        
        public void SelectUnit(NetworkUnit unit)
        {
            DeselectAllUnits();
            
            if (unit != null)
            {
                _selectedUnits.Add(unit);
                unit.SetSelected(true);
            }
        }
        
        public void SelectMultipleUnits(List<NetworkUnit> units)
        {
            DeselectAllUnits();
            
            foreach (var unit in units)
            {
                if (unit != null && unit.OwnerId == _networkManager.LocalPlayerId)
                {
                    _selectedUnits.Add(unit);
                    unit.SetSelected(true);
                }
            }
        }
        
        public void AddUnitsToSelection(List<NetworkUnit> units)
        {
            foreach (var unit in units)
            {
                if (unit != null && unit.OwnerId == _networkManager.LocalPlayerId && !_selectedUnits.Contains(unit))
                {
                    _selectedUnits.Add(unit);
                    unit.SetSelected(true);
                }
            }
        }
        
        public bool IsUnitSelected(NetworkUnit unit)
        {
            return _selectedUnits.Contains(unit);
        }
        
        public void DeselectAllUnits()
        {
            foreach (var unit in _selectedUnits)
            {
                if (unit != null)
                {
                    unit.SetSelected(false);
                    var handler = unit.GetComponent<ISelectionHandler>();
                    handler?.OnDeselect();
                }
            }
            
            _selectedUnits.Clear();
        }
        
        private void SelectAllOwnedUnits()
        {
            var allUnits = FindObjectsOfType<NetworkUnit>();
            var ownedUnits = allUnits.Where(u => u.OwnerId == _networkManager.LocalPlayerId).ToList();
            SelectMultipleUnits(ownedUnits);
        }
        
        private void ShowPathPreview(Vector3 targetPosition)
        {
            if (_selectedUnits.Count != 1) return;
            
            var unit = _selectedUnits[0];
            var movementController = unit.GetComponent<IMovementController>();
            var path = movementController.GetPath(targetPosition);
            
            if (path != null && path.Count > 0)
            {
                unit.ShowPathPreview(path.ToArray());
            }
        }
        
        private void RequestMovement(NetworkUnit unit, Vector3 targetPosition)
        {
            unit.RequestMoveServerRpc(targetPosition);
            _turnManager.RegisterAction(ActionType.Move);
            DeselectAllUnits();
        }
        
        private void RequestAttack(NetworkUnit attacker, NetworkUnit target)
        {
            var distance = Vector3.Distance(attacker.Position, target.Position);
            
            if (distance <= attacker.AttackRange)
            {
                attacker.RequestAttackServerRpc(target.NetworkObjectId);
                _turnManager.RegisterAction(ActionType.Attack);
                DeselectAllUnits();
            }
        }
        
        private void OnDrawGizmos()
        {
            if (_selectedUnits != null)
            {
                Gizmos.color = Color.green;
                
                foreach (var unit in _selectedUnits)
                {
                    if (unit != null)
                    {
                        Gizmos.DrawWireSphere(unit.transform.position + Vector3.up * 2, 0.3f);
                    }
                }
            }
        }
    }
}