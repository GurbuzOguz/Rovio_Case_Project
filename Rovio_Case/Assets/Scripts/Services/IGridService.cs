using UnityEngine;

public interface IGridService
{
    int Rows { get; }
    int Columns { get; }

    bool IsInside(int x, int y);

    Vector3 GridToWorld(int x, int y);
    Vector2Int WorldToGrid(Vector3 worldPosition);

    int GetProductAt(int x, int y);
    bool HasProductAt(int x, int y);
    void AddProductAt(int x, int y, int colorId);
    void RemoveProductAt(int x, int y);
    bool AreAllProductsCollected();
}

