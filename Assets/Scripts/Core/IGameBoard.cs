using System.Collections.Generic;
using UnityEngine;

namespace TurnBasedStrategy.Core
{
    public interface IGameBoard
    {
        Vector2Int BoardSize { get; }
        void GenerateBoard(BoardGenerationSettings settings);
        bool IsPositionValid(Vector3 position);
        List<IUnit> GetUnitsInRange(Vector3 center, float range);
    }
}
