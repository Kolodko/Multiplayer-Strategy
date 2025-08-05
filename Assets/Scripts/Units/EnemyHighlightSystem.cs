using System.Collections.Generic;
using TurnBasedStrategy.Core;
using TurnBasedStrategy.Gameplay;
using UnityEngine;

namespace TurnBasedStrategy.Units
{
    public class EnemyHighlightSystem : MonoBehaviour
    {
        [Header("Highlight Settings")]
        [SerializeField] private Color _enemyInRangeColor = Color.red;
        [SerializeField] private float _highlightIntensity = 0.5f;
        
        private NetworkUnit _unit;
        private IGameBoard _gameBoard;
        private Dictionary<NetworkUnit, Color> _originalColors = new Dictionary<NetworkUnit, Color>();
        private List<NetworkUnit> _highlightedEnemies = new List<NetworkUnit>();
        private Vector3 _lastCheckedPosition;
        
        private void Awake()
        {
            _unit = GetComponent<NetworkUnit>();
            _gameBoard = FindObjectOfType<GameBoard>();
        }
        
        private void Update()
        {
            if (_unit == null || !_unit.IsOwner) 
                return;
            
            Vector3 checkPosition = _unit.Position;
            
            if (_unit.GetComponent<LineRenderer>() != null && _unit.GetComponent<LineRenderer>().enabled)
            {
                var pathPreview = _unit.GetComponent<LineRenderer>();
                
                if (pathPreview.positionCount > 0)
                {
                    checkPosition = pathPreview.GetPosition(pathPreview.positionCount - 1);
                }
            }
            
            if (Vector3.Distance(checkPosition, _lastCheckedPosition) > 0.1f)
            {
                UpdateEnemyHighlights(checkPosition);
                _lastCheckedPosition = checkPosition;
            }
        }
        
        private void UpdateEnemyHighlights(Vector3 centerPosition)
        {
            ClearHighlights();
            
            if (_gameBoard == null) return;
            
            var unitsInRange = _gameBoard.GetUnitsInRange(centerPosition, _unit.AttackRange);
            
            foreach (var potentialTarget in unitsInRange)
            {
                var targetUnit = potentialTarget as NetworkUnit;
                
                if (targetUnit != null && targetUnit.OwnerId != _unit.OwnerId && targetUnit.IsAlive)
                {
                    HighlightEnemy(targetUnit);
                }
            }
        }
        
        private void HighlightEnemy(NetworkUnit enemy)
        {
            var renderer = enemy.GetComponentInChildren<Renderer>();
            
            if (renderer != null)
            {
                if (!_originalColors.ContainsKey(enemy))
                {
                    _originalColors[enemy] = renderer.material.color;
                }
                
                Color highlightColor = Color.Lerp(_originalColors[enemy], _enemyInRangeColor, _highlightIntensity);
                renderer.material.color = highlightColor;
                
                _highlightedEnemies.Add(enemy);
            }
        }
        
        private void ClearHighlights()
        {
            foreach (var enemy in _highlightedEnemies)
            {
                if (enemy != null && _originalColors.ContainsKey(enemy))
                {
                    var renderer = enemy.GetComponentInChildren<Renderer>();
                    
                    if (renderer != null)
                    {
                        renderer.material.color = _originalColors[enemy];
                    }
                }
            }
            
            _highlightedEnemies.Clear();
            _originalColors.Clear();
        }
        
        private void OnDisable()
        {
            ClearHighlights();
        }
        
        private void OnDestroy()
        {
            ClearHighlights();
        }
    }
}
