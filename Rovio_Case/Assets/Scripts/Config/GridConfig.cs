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

    [Header("Runtime Scaling")]
    [Tooltip("Keeps board world-size constant by scaling cell size against a reference grid.")]
    public bool keepWorldSizeFromReference = true;
    [Min(1)] public int referenceRows = 10;
    [Min(1)] public int referenceColumns = 10;

    [Header("Box Path")]
    [Tooltip("Shared path reference used by boxes. Assign a BoxPath component from the scene.")]
    public BoxPath boxPath;

    public float GetRuntimeCellSize()
    {
        float baseSize = Mathf.Max(0.0001f, cellSize);
        if (!keepWorldSizeFromReference)
        {
            return baseSize;
        }

        float refRows = Mathf.Max(1, referenceRows);
        float refCols = Mathf.Max(1, referenceColumns);
        float rowsSafe = Mathf.Max(1, rows);
        float colsSafe = Mathf.Max(1, columns);

        // Fit current grid into the same world footprint as reference grid.
        float sizeByRows = (baseSize * refRows) / rowsSafe;
        float sizeByCols = (baseSize * refCols) / colsSafe;
        return Mathf.Max(0.0001f, Mathf.Min(sizeByRows, sizeByCols));
    }
}

