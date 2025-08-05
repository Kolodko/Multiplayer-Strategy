using System;
using Cysharp.Threading.Tasks;

namespace TurnBasedStrategy.Core
{
    public interface ITurnManager
    {
        int CurrentPlayerId { get; }
        int TurnNumber { get; }
        float RemainingTime { get; }
        bool CanMove { get; }
        bool CanAttack { get; }
        
        event Action<int> OnTurnChanged;
        event Action<float> OnTimerUpdated;
        event Action OnGameEnded;
        
        void StartGame();
        UniTask EndTurn();
        void RegisterAction(ActionType type);
    }

    public enum ActionType
    {
        Move,
        Attack
    }
}
