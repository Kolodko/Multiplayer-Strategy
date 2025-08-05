using Cysharp.Threading.Tasks;
using TurnBasedStrategy.Core;
using UnityEngine;

namespace TurnBasedStrategy.Combat
{
    public class CombatSystem : MonoBehaviour, ICombatSystem
    {
        [Header("Combat Settings")]
        [SerializeField] private LayerMask _obstacleLayer = 1 << 10;
        [SerializeField] private bool _checkLineOfSight = true;
        [SerializeField] private float _unitRadius = 0.5f;
        
        public bool CanAttack(IUnit attacker, IUnit target)
        {
            if (attacker == null || target == null || !target.IsAlive)
                return false;
                
            if (attacker.OwnerId == target.OwnerId)
                return false;
                
            float distance = Vector3.Distance(attacker.Position, target.Position);
            
            float effectiveDistance = distance - _unitRadius * 2;
            
            if (effectiveDistance > attacker.AttackRange)
                return false;

            if (_checkLineOfSight)
            {
                return HasLineOfSight(attacker.Position, target.Position);
            }
            
            return true;
        }
        
        public async UniTask<bool> PerformAttack(IUnit attacker, IUnit target)
        {
            if (!CanAttack(attacker, target))
                return false;
            
            await PlayAttackAnimation(attacker, target);
            
            target.TakeDamage();
            
            return true;
        }
        
        private bool HasLineOfSight(Vector3 from, Vector3 to)
        {
            Vector3 direction = to - from;
            float distance = direction.magnitude;
            
            from += Vector3.up * 0.5f;
            to += Vector3.up * 0.5f;
            
            return !Physics.Raycast(from, direction.normalized, distance, _obstacleLayer);
        }
        
        private async UniTask PlayAttackAnimation(IUnit attacker, IUnit target)
        {
            await UniTask.Delay(300);
        }
    }
}