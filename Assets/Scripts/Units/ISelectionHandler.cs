using TurnBasedStrategy.Units;

public interface ISelectionHandler
{
    void Initialize(NetworkUnit unit);
    void OnSelect();
    void OnDeselect();
}
