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

    /// <summary>
    /// World pozisyonu grid satır/sütun merkezlerinden birine hizalıysa,
    /// o satır/sütunda (aynı X veya aynı Y) verilen colorId'ye sahip bir product hücresi bulur.
    /// </summary>
    bool TryFindAlignedProductCell(Vector3 worldPosition, float alignTolerance, int colorId, out Vector2Int cell);
}

