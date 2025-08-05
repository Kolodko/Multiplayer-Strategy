using Unity.Netcode;
using UnityEngine;

namespace TurnBasedStrategy.Units
{
    public class PlayerColorSystem : NetworkBehaviour
    {
        [Header("Player Colors")]
        [SerializeField] private Color _player1Color = new Color(0.2f, 0.4f, 0.8f);
        [SerializeField] private Color _player2Color = new Color(0.8f, 0.2f, 0.2f);
        [SerializeField] private bool _applyToAllRenderers = true;
        
        private NetworkUnit _unit;
        private Renderer[] _renderers;
        private Material[] _originalMaterials;
        
        private void Awake()
        {
            _unit = GetComponent<NetworkUnit>();
            _renderers = _applyToAllRenderers ? 
                GetComponentsInChildren<Renderer>() : 
                new Renderer[] { GetComponent<Renderer>() };
                
            _originalMaterials = new Material[_renderers.Length];
            
            for (int i = 0; i < _renderers.Length; i++)
            {
                if (_renderers[i] != null)
                {
                    _originalMaterials[i] = _renderers[i].material;
                }
            }
        }
        
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            
            if (_unit != null)
            {
                ApplyPlayerColor(_unit.OwnerId);
            }
        }
        
        private void ApplyPlayerColor(int playerId)
        {
            Color targetColor = playerId == 0 ? _player1Color : _player2Color;
            
            foreach (var renderer in _renderers)
            {
                if (renderer != null && !IsIndicatorRenderer(renderer))
                {
                    Material newMaterial = new Material(renderer.material);
                    newMaterial.color = targetColor;
                    renderer.material = newMaterial;
                }
            }
        }
        
        private bool IsIndicatorRenderer(Renderer renderer)
        {
            string[] indicatorNames = { "SelectionIndicator", "AttackRangeIndicator", "PreviewSelectionIndicator" };
            
            foreach (var name in indicatorNames)
            {
                if (renderer.gameObject.name.Contains(name))
                {
                    return true;
                }
            }
            
            return false;
        }
    }
}
