using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using TurnBasedStrategy.Combat;
using TurnBasedStrategy.Core;
using TurnBasedStrategy.Gameplay;
using TurnBasedStrategy.Units;
using UnityEngine;

namespace TurnBasedStrategy.Advanced
{
    public class AIOpponent : MonoBehaviour
    {
        private enum AIState
        {
            Idle,
            Analyzing,
            ExecutingMove,
            ExecutingAttack
        }
        
        public enum AIDifficulty
        {
            Easy,
            Medium,
            Hard
        }
        
        [Header("AI Settings")]
        [SerializeField] private float _thinkingDelay = 1f;
        [SerializeField] private float _actionDelay = 0.5f;
        [SerializeField] private AIDifficulty _difficulty = AIDifficulty.Medium;
        
        private AIState _currentState = AIState.Idle;
        private ITurnManager _turnManager;
        private IGameBoard _gameBoard;
        private ICombatSystem _combatSystem;
        private int _aiPlayerId = 1;
        
        private void Start()
        {
            _turnManager = FindObjectOfType<NetworkTurnManager>();
            _gameBoard = FindObjectOfType<GameBoard>();
            _combatSystem = FindObjectOfType<CombatSystem>();
            
            if (_turnManager != null)
            {
                _turnManager.OnTurnChanged += OnTurnChanged;
            }
        }
        
        private void OnTurnChanged(int currentPlayerId)
        {
            if (currentPlayerId == _aiPlayerId && _currentState == AIState.Idle)
            {
                ExecuteAITurn().Forget();
            }
        }
        
        private async UniTaskVoid ExecuteAITurn()
        {
            _currentState = AIState.Analyzing;
            
            await UniTask.Delay((int)(_thinkingDelay * 1000));
            
            var aiUnits = GetAIUnits();
            var enemyUnits = GetEnemyUnits();
            
            switch (_difficulty)
            {
                case AIDifficulty.Easy:
                    await ExecuteEasyStrategy(aiUnits, enemyUnits);
                    break;
                case AIDifficulty.Medium:
                    await ExecuteMediumStrategy(aiUnits, enemyUnits);
                    break;
                case AIDifficulty.Hard:
                    await ExecuteHardStrategy(aiUnits, enemyUnits);
                    break;
            }
            
            await _turnManager.EndTurn();
            
            _currentState = AIState.Idle;
        }
        
        private async UniTask ExecuteMediumStrategy(List<NetworkUnit> aiUnits, List<NetworkUnit> enemyUnits)
        {
            if (_turnManager.CanAttack)
            {
                var attackAction = FindBestAttackAction(aiUnits, enemyUnits);
                
                if (attackAction.HasValue)
                {
                    await ExecuteAttack(attackAction.Value.attacker, attackAction.Value.target);
                }
            }
            
            if (_turnManager.CanMove)
            {
                var moveAction = FindBestMoveAction(aiUnits, enemyUnits);
                
                if (moveAction.HasValue)
                {
                    await ExecuteMove(moveAction.Value.unit, moveAction.Value.position);
                }
            }
        }
        
        private (NetworkUnit attacker, NetworkUnit target)? FindBestAttackAction(
            List<NetworkUnit> aiUnits, List<NetworkUnit> enemyUnits)
        {
            var possibleAttacks = new List<(NetworkUnit attacker, NetworkUnit target, float score)>();
            
            foreach (var attacker in aiUnits)
            {
                foreach (var target in enemyUnits)
                {
                    if (_combatSystem.CanAttack(attacker, target))
                    {
                        float score = CalculateAttackScore(attacker, target);
                        possibleAttacks.Add((attacker, target, score));
                    }
                }
            }
            
            if (possibleAttacks.Count == 0) return null;
            
            var bestAttack = possibleAttacks.OrderByDescending(a => a.score).First();
            
            return (bestAttack.attacker, bestAttack.target);
        }
        
        private float CalculateAttackScore(NetworkUnit attacker, NetworkUnit target)
        {
            float score = 100f;
            
            if (target.Type == UnitType.SlowRanged)
                score += 50f;
            
            var enemiesNearTarget = _gameBoard.GetUnitsInRange(target.Position, 5f)
                .Count(u => u.OwnerId != _aiPlayerId);
            score -= enemiesNearTarget * 10f;
            
            return score;
        }
        
        private (NetworkUnit unit, Vector3 position)? FindBestMoveAction(
            List<NetworkUnit> aiUnits, List<NetworkUnit> enemyUnits)
        {
            var moveOptions = new List<(NetworkUnit unit, Vector3 position, float score)>();
            
            foreach (var unit in aiUnits)
            {
                var bestPosition = CalculateBestPosition(unit, enemyUnits);
                
                if (bestPosition.HasValue)
                {
                    float score = CalculatePositionScore(unit, bestPosition.Value, enemyUnits);
                    moveOptions.Add((unit, bestPosition.Value, score));
                }
            }
            
            if (moveOptions.Count == 0) return null;
            
            var bestMove = moveOptions.OrderByDescending(m => m.score).First();
            
            return (bestMove.unit, bestMove.position);
        }
        
        private Vector3? CalculateBestPosition(NetworkUnit unit, List<NetworkUnit> enemyUnits)
        {
            var positions = GeneratePossiblePositions(unit);
            Vector3? bestPosition = null;
            float bestScore = float.MinValue;
            
            foreach (var pos in positions)
            {
                float score = CalculatePositionScore(unit, pos, enemyUnits);
                
                if (score > bestScore)
                {
                    bestScore = score;
                    bestPosition = pos;
                }
            }
            
            return bestPosition;
        }
        
        private List<Vector3> GeneratePossiblePositions(NetworkUnit unit)
        {
            var positions = new List<Vector3>();
            int samples = 8;
            float maxDistance = unit.MoveSpeed;
            
            for (int i = 0; i < samples; i++)
            {
                float angle = (i / (float)samples) * Mathf.PI * 2f;
                float distance = maxDistance * 0.8f;
                
                Vector3 offset = new Vector3(
                    Mathf.Cos(angle) * distance,
                    0,
                    Mathf.Sin(angle) * distance
                );
                
                Vector3 position = unit.Position + offset;
                
                if (_gameBoard.IsPositionValid(position))
                {
                    positions.Add(position);
                }
            }
            
            return positions;
        }
        
        private float CalculatePositionScore(NetworkUnit unit, Vector3 position, List<NetworkUnit> enemyUnits)
        {
            float score = 0f;
            
            var enemiesInRange = enemyUnits.Where(e => 
                Vector3.Distance(position, e.Position) <= unit.AttackRange).ToList();
            score += enemiesInRange.Count * 30f;
            
            var threateningEnemies = enemyUnits.Where(e => 
                Vector3.Distance(position, e.Position) <= e.AttackRange).ToList();
            score -= threateningEnemies.Count * 20f;
            
            return score;
        }
        
        private async UniTask ExecuteMove(NetworkUnit unit, Vector3 position)
        {
            _currentState = AIState.ExecutingMove;
            await UniTask.Delay((int)(_actionDelay * 1000));
            
            unit.RequestMoveServerRpc(position);
            _turnManager.RegisterAction(ActionType.Move);
            
            await UniTask.Delay(1000);
        }
        
        private async UniTask ExecuteAttack(NetworkUnit attacker, NetworkUnit target)
        {
            _currentState = AIState.ExecutingAttack;
            await UniTask.Delay((int)(_actionDelay * 1000));
            
            attacker.RequestAttackServerRpc(target.NetworkObjectId);
            _turnManager.RegisterAction(ActionType.Attack);
            
            await UniTask.Delay(500);
        }
        
        private async UniTask ExecuteEasyStrategy(List<NetworkUnit> aiUnits, List<NetworkUnit> enemyUnits)
        {
            if (aiUnits.Count > 0 && Random.value > 0.5f && _turnManager.CanMove)
            {
                var unit = aiUnits[Random.Range(0, aiUnits.Count)];
                var positions = GeneratePossiblePositions(unit);
                
                if (positions.Count > 0)
                {
                    await ExecuteMove(unit, positions[Random.Range(0, positions.Count)]);
                }
            }
        }
        
        private async UniTask ExecuteHardStrategy(List<NetworkUnit> aiUnits, List<NetworkUnit> enemyUnits)
        {
            await ExecuteMediumStrategy(aiUnits, enemyUnits);
        }
        
        private List<NetworkUnit> GetAIUnits()
        {
            var unitManager = FindObjectOfType<UnitManager>();
            return unitManager?.GetPlayerUnits(_aiPlayerId) ?? new List<NetworkUnit>();
        }
        
        private List<NetworkUnit> GetEnemyUnits()
        {
            var unitManager = FindObjectOfType<UnitManager>();
            return unitManager?.GetPlayerUnits(1 - _aiPlayerId) ?? new List<NetworkUnit>();
        }
    }
}
