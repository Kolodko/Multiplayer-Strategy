using TurnBasedStrategy.Core;
using TurnBasedStrategy.Gameplay;
using TurnBasedStrategy.Network;
using TurnBasedStrategy.Units;
using UnityEngine;
using UnityEngine.EventSystems;

namespace TurnBasedStrategy.Input
{
    public interface IGameInputHandler
    {
        void SelectUnit(NetworkUnit unit);
        bool IsUnitSelected(NetworkUnit unit);
        NetworkUnit SelectedUnit { get; }
    }
    
    public class GameInputHandler : MonoBehaviour, IGameInputHandler
    {
        [Header("Input Settings")]
        [SerializeField] private LayerMask _groundLayer = 1 << 8;
        [SerializeField] private LayerMask _unitLayer = 1 << 9;
        
        [Header("Debug")]
        [SerializeField] private NetworkUnit _selectedUnit;
        
        private ITurnManager _turnManager;
        private INetworkGameManager _networkManager;
        private Camera _mainCamera;
        
        public NetworkUnit SelectedUnit => _selectedUnit;
        
        private void Awake()
        {
            _mainCamera = Camera.main;
            if (_mainCamera == null)
            {
               Debug.LogError("Main Camera not found!");
            }
        }
        
        private void Start()
        {
            _turnManager = FindObjectOfType<NetworkTurnManager>();
            _networkManager = FindObjectOfType<NetworkGameManager>();
            
            Debug.Log($"GameInputHandler initialized. Ground Layer: {_groundLayer.value}, Unit Layer: {_unitLayer.value}");
        }
        
        private void Update()
        {
            if (_networkManager == null || _turnManager == null) return;
            
            if (_networkManager.LocalPlayerId != _turnManager.CurrentPlayerId)
            {
                Debug.Log("Not your turn!");
                return;
            }
            
            HandleMouseInput();
        }
        
        
        private void HandleMouseInput()
        {
            if (UnityEngine.Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())
            {
                HandleSelection();
            }
        }
        
        private void HandleSelection()
        {
            var ray = _mainCamera.ScreenPointToRay(UnityEngine.Input.mousePosition);
            
            if (Physics.Raycast(ray, out RaycastHit hit, 100f, _unitLayer))
            {
                Debug.Log($"Hit object: {hit.collider.gameObject.name} on layer {hit.collider.gameObject.layer}");
                
                var unit = hit.collider.GetComponent<NetworkUnit>();
                
                if (unit != null)
                {
                    
                    if (unit.OwnerId == _networkManager.LocalPlayerId)
                    {
                        SelectUnit(unit);
                    }
                }
            }
            else
            {
                DeselectUnit();
            }
        }
        
        public void SelectUnit(NetworkUnit unit)
        {
            if (_selectedUnit != null && _selectedUnit != unit)
            {
                _selectedUnit.SetSelected(false);
                var prevHandler = _selectedUnit.GetComponent<ISelectionHandler>();
                prevHandler?.OnDeselect();
            }
            
            _selectedUnit = unit;
            
            if (_selectedUnit != null)
            {
                _selectedUnit.SetSelected(true);
            }
        }
        
        public bool IsUnitSelected(NetworkUnit unit)
        {
            return _selectedUnit != null && _selectedUnit == unit;
        }
        
        private void DeselectUnit()
        {
            if (_selectedUnit != null)
            {
                _selectedUnit.SetSelected(false);
                var handler = _selectedUnit.GetComponent<ISelectionHandler>();
                handler?.OnDeselect();
                
                _selectedUnit = null;
            }
        }
        
        
        private void OnDrawGizmos()
        {
            if (_selectedUnit != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(_selectedUnit.transform.position, 0.5f);
            }
        }
    }
}
