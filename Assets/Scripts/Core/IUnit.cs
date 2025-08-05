using UnityEngine;
using Unity.Netcode;

namespace TurnBasedStrategy.Core
{
    public interface IUnit
    {
        int OwnerId { get; }
        UnitType Type { get; }
        float MoveSpeed { get; }
        float AttackRange { get; }
        Vector3 Position { get; }
        bool IsAlive { get; }
        void Initialize(int ownerId, UnitType type);
        void TakeDamage();
    }

    public enum UnitType
    {
        SlowRanged,
        FastMelee
    }
}

