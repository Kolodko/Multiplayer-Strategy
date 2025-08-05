using UnityEngine;

namespace TurnBasedStrategy.Units
{
    [RequireComponent(typeof(NetworkUnit))]
    public class AttackRangeVisualizer : MonoBehaviour
    {
        [Header("Visual Settings")]
        [SerializeField] private Color _rangeColor = new Color(1f, 0f, 0f, 0.3f);
        [SerializeField] private float _lineWidth = 0.1f;
        [SerializeField] private int _segments = 64;
        [SerializeField] private float _heightOffset = 0.1f;
        
        private NetworkUnit _unit;
        private LineRenderer _rangeLineRenderer;
        private GameObject _rangeIndicator;
        
        private void Awake()
        {
            _unit = GetComponent<NetworkUnit>();
            CreateRangeIndicator();
        }
        
        private void CreateRangeIndicator()
        {
            GameObject rangeObject = new GameObject("AttackRangeIndicator");
            rangeObject.transform.SetParent(transform);
            rangeObject.transform.localPosition = Vector3.zero;
            
            _rangeLineRenderer = rangeObject.AddComponent<LineRenderer>();
            _rangeLineRenderer.startWidth = _lineWidth;
            _rangeLineRenderer.endWidth = _lineWidth;
            _rangeLineRenderer.loop = true;
            _rangeLineRenderer.useWorldSpace = false;
            
            Material lineMaterial = new Material(Shader.Find("Sprites/Default"));
            lineMaterial.color = _rangeColor;
            _rangeLineRenderer.material = lineMaterial;
            
            _rangeLineRenderer.positionCount = _segments + 1;
            
            float angleStep = 360f / _segments;
            
            for (int i = 0; i <= _segments; i++)
            {
                float angle = i * angleStep * Mathf.Deg2Rad;
                float x = Mathf.Cos(angle);
                float z = Mathf.Sin(angle);
                Vector3 point = new Vector3(x, _heightOffset, z);
                _rangeLineRenderer.SetPosition(i, point);
            }
            
            _rangeIndicator = rangeObject;
            _rangeIndicator.SetActive(false);
        }
        
        private void Start()
        {
            if (_unit != null)
            {
                UpdateRangeScale();
            }
        }
        
        private void UpdateRangeScale()
        {
            if (_rangeLineRenderer != null && _unit != null)
            {
                float range = _unit.AttackRange;
                _rangeIndicator.transform.localScale = new Vector3(range, 1f, range);
            }
        }
        
        public void ShowRange(bool show)
        {
            if (_rangeIndicator != null)
            {
                _rangeIndicator.SetActive(show);
                
                if (show)
                {
                    UpdateRangeScale();
                }
            }
        }
        
        public void UpdateRangePosition(Vector3 worldPosition)
        {
            if (_rangeIndicator != null)
            {
                _rangeIndicator.transform.position = new Vector3(worldPosition.x, _heightOffset, worldPosition.z);
            }
        }
        
        public void ResetRangePosition()
        {
            if (_rangeIndicator != null)
            {
                _rangeIndicator.transform.localPosition = Vector3.zero;
            }
        }
    }
}
