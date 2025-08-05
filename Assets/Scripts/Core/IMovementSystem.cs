using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;

namespace TurnBasedStrategy.Core
{
    public interface IMovementSystem
    {
        UniTask<bool> MoveUnit(IUnit unit, Vector3 targetPosition);
        List<Vector3> CalculatePath(Vector3 from, Vector3 to);
        float GetPathLength(List<Vector3> path);
        bool IsValidPath(IUnit unit, List<Vector3> path);
    }
}
