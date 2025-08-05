using Cysharp.Threading.Tasks;

namespace TurnBasedStrategy.Core
{
    public interface ICombatSystem
    {
        bool CanAttack(IUnit attacker, IUnit target);
        UniTask<bool> PerformAttack(IUnit attacker, IUnit target);
    }
}
