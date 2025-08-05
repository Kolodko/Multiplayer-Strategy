using UnityEngine;
using UnityEngine.AI;

namespace TurnBasedStrategy.Environment
{
    [RequireComponent(typeof(NavMeshObstacle))]
    public class ObstacleSetup : MonoBehaviour
    {
        [Header("NavMesh Obstacle Settings")]
        [SerializeField] private bool _carveNavMesh = true;
        [SerializeField] private bool _moveThreshold = false;
        [SerializeField] private float _carvingMoveThreshold = 0.1f;
        [SerializeField] private float _carvingTimeToStationary = 0.5f;
        
        private NavMeshObstacle _navMeshObstacle;
        
        private void Awake()
        {
            SetupNavMeshObstacle();
        }
        
        private void SetupNavMeshObstacle()
        {
            _navMeshObstacle = GetComponent<NavMeshObstacle>();
            
            if (_navMeshObstacle == null)
            {
                _navMeshObstacle = gameObject.AddComponent<NavMeshObstacle>();
            }
            
            _navMeshObstacle.carving = _carveNavMesh;
            _navMeshObstacle.carveOnlyStationary = !_moveThreshold;
            _navMeshObstacle.carvingMoveThreshold = _carvingMoveThreshold;
            _navMeshObstacle.carvingTimeToStationary = _carvingTimeToStationary;
            
            Collider collider = GetComponent<Collider>();
            if (collider != null)
            {
                if (collider is BoxCollider box)
                {
                    _navMeshObstacle.shape = NavMeshObstacleShape.Box;
                    _navMeshObstacle.size = box.size;
                    _navMeshObstacle.center = box.center;
                }
                else if (collider is CapsuleCollider capsule)
                {
                    _navMeshObstacle.shape = NavMeshObstacleShape.Capsule;
                    _navMeshObstacle.radius = capsule.radius;
                    _navMeshObstacle.height = capsule.height;
                    _navMeshObstacle.center = capsule.center;
                }
                else
                {
                    _navMeshObstacle.shape = NavMeshObstacleShape.Box;
                    _navMeshObstacle.size = collider.bounds.size;
                    _navMeshObstacle.center = collider.bounds.center - transform.position;
                }
            }
            
            gameObject.layer = LayerMask.NameToLayer("Obstacle");
            gameObject.isStatic = true;
        }
        
#if UNITY_EDITOR
        [ContextMenu("Setup All Obstacles in Scene")]
        private void SetupAllObstaclesInScene()
        {
            ObstacleSetup[] allObstacles = FindObjectsOfType<ObstacleSetup>();
            
            foreach (var obstacle in allObstacles)
            {
                obstacle.SetupNavMeshObstacle();
            }
        }
#endif
    }
}
