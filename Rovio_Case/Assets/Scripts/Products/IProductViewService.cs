using UnityEngine;

public interface IProductViewService
{
    /// <summary>Grid hücresindeki product view'i kaydeder.</summary>
    void Register(Vector2Int cell, ProductView view);

    /// <summary>Grid hücresindeki kaydı siler.</summary>
    void Unregister(Vector2Int cell);

    /// <summary>
    /// Hücredeki product view'i tüketir (kayıttan çıkarır) ve varsa box'a çekme animasyonu başlatır.
    /// View yoksa false döner.
    /// </summary>
    bool TryConsumeAndPullToBox(Vector2Int cell, Transform boxTransform);
}

