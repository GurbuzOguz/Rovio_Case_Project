using UnityEngine;

public class ProductView : MonoBehaviour
{
    public Vector2Int Cell { get; private set; }
    public int ColorId { get; private set; }

    public void Initialize(Vector2Int cell, int colorId)
    {
        Cell = cell;
        ColorId = colorId;
    }
}

