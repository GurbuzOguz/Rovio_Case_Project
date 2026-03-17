using UnityEngine;

[CreateAssetMenu(fileName = "GridConfig", menuName = "Game/Grid Config")]
public class GridConfig : ScriptableObject
{
    [Header("Grid Size")]
    [Min(1)]
    public int rows = 6;

    [Min(1)]
    public int columns = 6;

    [Header("Cell Settings")]
    public float cellSize = 1f;
    public Vector3 origin = Vector3.zero;
}

