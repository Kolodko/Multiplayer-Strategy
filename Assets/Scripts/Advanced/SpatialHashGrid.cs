using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class SpatialHashGrid
{
    private Dictionary<Vector2Int, List<NetworkObject>> _grid = new();
    private float _cellSize;
        
    public SpatialHashGrid(float cellSize)
    {
        _cellSize = cellSize;
    }
    
    public List<NetworkObject> GetObjectsInRadius(Vector3 center, float radius)
    {
        var results = new List<NetworkObject>();
        var cellRadius = Mathf.CeilToInt(radius / _cellSize);
        var centerCell = GetCell(center);
            
        for (int x = -cellRadius; x <= cellRadius; x++)
        {
            for (int y = -cellRadius; y <= cellRadius; y++)
            {
                var cell = new Vector2Int(centerCell.x + x, centerCell.y + y);
                
                if (_grid.ContainsKey(cell))
                {
                    results.AddRange(_grid[cell]);
                }
            }
        }
            
        return results;
    }
        
    private Vector2Int GetCell(Vector3 position)
    {
        return new Vector2Int(
            Mathf.FloorToInt(position.x / _cellSize),
            Mathf.FloorToInt(position.z / _cellSize)
        );
    }
}