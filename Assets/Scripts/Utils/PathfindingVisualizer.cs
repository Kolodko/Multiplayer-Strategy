using System.Collections.Generic;
using UnityEngine;

namespace TurnBasedStrategy.Utils
{
    public class PathfindingVisualizer : MonoBehaviour
    {
        [Header("Visualization Settings")]
        [SerializeField] private LineRenderer _pathLineRenderer;
        [SerializeField] private Color _validPathColor = Color.green;
        [SerializeField] private Color _invalidPathColor = Color.red;
        [SerializeField] private float _pathLineWidth = 0.2f;
        [SerializeField] private float _pathLineHeight = 0.1f;
        
        private void Awake()
        {
            if (_pathLineRenderer == null)
            {
                _pathLineRenderer = GetComponent<LineRenderer>();
            }
            
            if (_pathLineRenderer != null)
            {
                _pathLineRenderer.startWidth = _pathLineWidth;
                _pathLineRenderer.endWidth = _pathLineWidth;
            }
        }
        
        public void ShowPath(List<Vector3> path, bool isValid)
        {
            if (_pathLineRenderer == null || path == null || path.Count < 2)
            {
                HidePath();
                return;
            }

            _pathLineRenderer.startColor = isValid ? _validPathColor : _invalidPathColor;
            _pathLineRenderer.endColor = isValid ? _validPathColor : _invalidPathColor;
            
            var positions = new Vector3[path.Count];
            for (int i = 0; i < path.Count; i++)
            {
                positions[i] = path[i] + Vector3.up * _pathLineHeight;
            }
            
            _pathLineRenderer.positionCount = positions.Length;
            _pathLineRenderer.SetPositions(positions);
            _pathLineRenderer.enabled = true;
        }
        
        public void HidePath()
        {
            if (_pathLineRenderer != null)
            {
                _pathLineRenderer.enabled = false;
            }
        }
    }
}
