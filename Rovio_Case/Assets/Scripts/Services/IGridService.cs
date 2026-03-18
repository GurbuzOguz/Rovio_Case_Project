using System.Collections.Generic;
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

    /// <summary>Grid'de kalan product sayıları (colorId -> count).</summary>
    IReadOnlyDictionary<int, int> GetRemainingCountsByColorId();

    /// <summary>
    /// Belirli bir hücreden product kaldırır ve boşluğu, direction yönünde shift ederek doldurur.
    /// Hem internal data güncellenir hem de hangi hücrelerin nereye kaydığı döndürülür.
    /// </summary>
    List<GridShiftMove> RemoveAndShift(Vector2Int removedCell, GridShiftDirection direction);

    /// <summary>
    /// Edge hücrelerinde boşluk varsa, ilgili yarı içinde ürünleri kaydırarak edge'i doldurur.
    /// (Remove yok; sadece kaydırma). Yapılan hareketler döndürülür.
    /// </summary>
    List<GridShiftMove> FillEdgeGaps();
}

