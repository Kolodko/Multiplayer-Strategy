using TurnBasedStrategy.Input;
using UnityEngine;
using UnityEngine.EventSystems;

namespace TurnBasedStrategy.Units
{
    public class SelectionHandler : MonoBehaviour, ISelectionHandler, IPointerClickHandler
    {
        private NetworkUnit _unit;
        private IGameInputHandler _inputHandler;
        private Camera _mainCamera;
        
        private float _lastRightClickTime;
        private const float DOUBLE_CLICK_TIME = 0.3f;
        
        private void Awake()
        {
            var collider = GetComponent<Collider>();
            
            if (collider == null)
            {
               Debug.LogError($"Unit {gameObject.name} needs a Collider component for selection!");
            }
        }
        
        public void Initialize(NetworkUnit unit)
        {
            _unit = unit;
            _mainCamera = Camera.main;
            
            if (_inputHandler == null)
            {
                _inputHandler = FindObjectOfType<GameInputHandler>();
                
                if (_inputHandler == null)
                {
                    Debug.LogError("GameInputHandler not found in scene!");
                }
            }
        }
        
        private void Start()
        {
            if (_inputHandler == null)
            {
                _inputHandler = FindObjectOfType<GameInputHandler>();
            }
        }
        
        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                if (_unit != null && _unit.IsOwner)
                {
                    OnSelect();
                }
            }
        }
        
        public void OnSelect()
        {
            if (_inputHandler == null)
            {
                _inputHandler = FindObjectOfType<GameInputHandler>();
            }
            
            if (_inputHandler != null)
            {
                _inputHandler.SelectUnit(_unit);
                Debug.Log($"Unit {_unit.name} selected");
            }
            else
            {
                Debug.LogError("GameInputHandler is null when trying to select unit!");
            }
        }
        
        public void OnDeselect()
        {
            if (_unit != null)
            {
                _unit.SetSelected(false);
                _unit.ShowPathPreview(new Vector3[0]);
                _unit.ResetAttackRangeIndicatorPosition();
            }
        }
        
        private void Update()
        {
            if (_unit == null || !_unit.IsOwner)
                return;
            
            if (_inputHandler == null || !_inputHandler.IsUnitSelected(_unit))
                return;
            
            if (UnityEngine.Input.GetMouseButtonDown(1))
            {
                float timeSinceLastClick = Time.time - _lastRightClickTime;
                
                if (timeSinceLastClick <= DOUBLE_CLICK_TIME)
                {
                    HandleMovementRequest();
                }
                else
                {
                    HandlePathPreview();
                }
                
                _lastRightClickTime = Time.time;
            }
        }
        
        private void HandlePathPreview()
        {
            Ray ray = _mainCamera.ScreenPointToRay(UnityEngine.Input.mousePosition);
            
            if (Physics.Raycast(ray, out RaycastHit hit, 100f, LayerMask.GetMask("Ground")))
            {
                var movementController = _unit.GetComponent<IMovementController>();
                
                if (movementController != null)
                {
                    var path = movementController.GetPath(hit.point);
                    
                    if (path != null)
                    {
                        _unit.ShowPathPreview(path.ToArray());
                        Debug.Log($"Showing path preview for {_unit.name} to {hit.point}");
                    }
                }
                else
                {
                    Debug.LogError("MovementController not found on unit!");
                }
            }
        }
        
        private void HandleMovementRequest()
        {
            Ray ray = _mainCamera.ScreenPointToRay(UnityEngine.Input.mousePosition);
            
            if (Physics.Raycast(ray, out RaycastHit hit, 100f, LayerMask.GetMask("Ground")))
            {
                Debug.Log($"Requesting move for {_unit.name} to {hit.point}");
                _unit.RequestMoveServerRpc(hit.point);
                
                _inputHandler?.SelectUnit(null);
            }
        }
    }
}