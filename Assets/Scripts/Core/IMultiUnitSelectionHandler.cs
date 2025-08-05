using System.Collections;
using System.Collections.Generic;
using TurnBasedStrategy.Input;
using TurnBasedStrategy.Units;
using UnityEngine;

public interface IMultiUnitSelectionHandler : IGameInputHandler
{
    List<NetworkUnit> SelectedUnits { get; }
    void SelectMultipleUnits(List<NetworkUnit> units);
    void AddUnitsToSelection(List<NetworkUnit> units);
    void DeselectAllUnits();
}
