using System.Collections.Generic;
using TurnBasedStrategy.Core;
using TurnBasedStrategy.Network;
using TurnBasedStrategy.Units;
using UnityEngine;
using UnityEngine.UI;

namespace TurnBasedStrategy.Input
{
    public class RTSSelectionBox : MonoBehaviour
    {
        [Header("Selection Box UI")]
        [SerializeField] private RectTransform _selectionBoxVisual;
        [SerializeField] private Canvas _canvas;
        
        [Header("Selection Settings")]
        [SerializeField] private LayerMask _unitLayer = 1 << 9;
        [SerializeField] private Color _selectionBoxColor = new Color(0, 1, 0, 0.25f);
        [SerializeField] private Color _selectionBoxBorderColor = new Color(0, 1, 0, 0.8f);
        
        private Vector2 _startPosition;
        private Vector2 _endPosition;
        private bool _isSelecting;
        private Camera _mainCamera;
        private IGameInputHandler _inputHandler;
        private INetworkGameManager _networkManager;
        
        private List<NetworkUnit> _selectableUnits = new List<NetworkUnit>();
        private Rect _selectionRect;
        
        private void Awake()
        {
            _mainCamera = Camera.main;
            _inputHandler = GetComponent<IGameInputHandler>() ?? FindObjectOfType<GameInputHandler>();
            _networkManager = FindObjectOfType<NetworkGameManager>();
            
            CreateSelectionBoxVisual();
        }
        
        private void CreateSelectionBoxVisual()
        {
            if (_selectionBoxVisual == null)
            {
                GameObject selectionBoxGO = new GameObject("SelectionBox");
                
                if (_canvas == null)
                {
                    _canvas = FindObjectOfType<Canvas>();
                }
                
                selectionBoxGO.transform.SetParent(_canvas.transform, false);
                
                _selectionBoxVisual = selectionBoxGO.AddComponent<RectTransform>();
                
                Image boxImage = selectionBoxGO.AddComponent<Image>();
                boxImage.color = _selectionBoxColor;
                boxImage.raycastTarget = false;
                
                GameObject borderGO = new GameObject("SelectionBoxBorder");
                borderGO.transform.SetParent(selectionBoxGO.transform, false);
                RectTransform borderRect = borderGO.AddComponent<RectTransform>();
                borderRect.anchorMin = Vector2.zero;
                borderRect.anchorMax = Vector2.one;
                borderRect.sizeDelta = Vector2.zero;
                borderRect.anchoredPosition = Vector2.zero;
                
                Outline outline = borderGO.AddComponent<Outline>();
                outline.effectColor = _selectionBoxBorderColor;
                outline.effectDistance = new Vector2(1, -1);
                
                Image borderImage = borderGO.AddComponent<Image>();
                borderImage.color = Color.clear;
                borderImage.raycastTarget = false;
            }
            
            _selectionBoxVisual.gameObject.SetActive(false);
        }
        
        private void Update()
        {
            HandleSelectionInput();
            
            if (_isSelecting)
            {
                UpdateSelectionBox();
                UpdateSelectedUnits();
            }
        }
        
        private void HandleSelectionInput()
        {
            if (UnityEngine.Input.GetMouseButtonDown(0))
            {
                _startPosition = UnityEngine.Input.mousePosition;
                _isSelecting = true;
                _selectionBoxVisual.gameObject.SetActive(true);
                
                RefreshSelectableUnits();
            }
            
            if (UnityEngine.Input.GetMouseButton(0) && _isSelecting)
            {
                _endPosition = UnityEngine.Input.mousePosition;
            }
            
            if (UnityEngine.Input.GetMouseButtonUp(0) && _isSelecting)
            {
                _endPosition = UnityEngine.Input.mousePosition;
                SelectUnitsInBox();
                
                _isSelecting = false;
                _selectionBoxVisual.gameObject.SetActive(false);
            }
        }
        
        private void UpdateSelectionBox()
        {
            Vector2 boxStart = _startPosition;
            Vector2 boxEnd = _endPosition;
            
            Vector2 boxCenter = (boxStart + boxEnd) / 2;
            _selectionBoxVisual.anchoredPosition = boxCenter;
            
            Vector2 boxSize = new Vector2(
                Mathf.Abs(boxStart.x - boxEnd.x),
                Mathf.Abs(boxStart.y - boxEnd.y)
            );
            
            _selectionBoxVisual.sizeDelta = boxSize;
            
            _selectionRect = new Rect(
                Mathf.Min(boxStart.x, boxEnd.x),
                Mathf.Min(boxStart.y, boxEnd.y),
                boxSize.x,
                boxSize.y
            );
        }
        
        private void UpdateSelectedUnits()
        {
            if (_selectionRect.width < 10 && _selectionRect.height < 10) return;
            
            foreach (var unit in _selectableUnits)
            {
                if (unit == null)
                    continue;
                
                Vector3 screenPos = _mainCamera.WorldToScreenPoint(unit.transform.position);
                
                if (_selectionRect.Contains(screenPos, true))
                {
                    unit.SetPreviewSelection(true);
                }
                else
                {
                    unit.SetPreviewSelection(false);
                }
            }
        }
        
        private void SelectUnitsInBox()
        {
            List<NetworkUnit> selectedUnits = new List<NetworkUnit>();
            
            if (_selectionRect.width < 10 && _selectionRect.height < 10)
            {
                Ray ray = _mainCamera.ScreenPointToRay(_startPosition);
                
                if (Physics.Raycast(ray, out RaycastHit hit, 100f, _unitLayer))
                {
                    NetworkUnit unit = hit.collider.GetComponent<NetworkUnit>();
                    
                    if (unit != null && unit.OwnerId == _networkManager.LocalPlayerId)
                    {
                        selectedUnits.Add(unit);
                    }
                }
            }
            else
            {
                foreach (var unit in _selectableUnits)
                {
                    if (unit == null) continue;
                    
                    Vector3 screenPos = _mainCamera.WorldToScreenPoint(unit.transform.position);
                    
                    if (_selectionRect.Contains(screenPos, true))
                    {
                        selectedUnits.Add(unit);
                    }
                    
                    unit.SetPreviewSelection(false);
                }
            }
            
            if (selectedUnits.Count > 0)
            {
                if (UnityEngine.Input.GetKey(KeyCode.LeftShift) || UnityEngine.Input.GetKey(KeyCode.LeftControl))
                {
                    (_inputHandler as MultiUnitSelectionHandler)?.AddUnitsToSelection(selectedUnits);
                }
                else
                {
                    (_inputHandler as MultiUnitSelectionHandler)?.SelectMultipleUnits(selectedUnits);
                }
            }
            else if (!UnityEngine.Input.GetKey(KeyCode.LeftShift) && !UnityEngine.Input.GetKey(KeyCode.LeftControl))
            {
                (_inputHandler as MultiUnitSelectionHandler)?.DeselectAllUnits();
            }
        }
        
        private void RefreshSelectableUnits()
        {
            _selectableUnits.Clear();
            
            NetworkUnit[] allUnits = FindObjectsOfType<NetworkUnit>();
            
            foreach (var unit in allUnits)
            {
                if (unit.OwnerId == _networkManager.LocalPlayerId)
                {
                    _selectableUnits.Add(unit);
                }
            }
        }
    }
}
