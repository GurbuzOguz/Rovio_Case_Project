using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "LevelLayout", menuName = "Game/Level Layout")]
public class LevelLayout : ScriptableObject
{
    [Header("Grid Reference")]
    public GridConfig gridConfig;

    [Header("Products Palette")]
    public ProductPalette productPalette;

    [Header("Products on Grid")]
    public List<ProductCellData> products = new List<ProductCellData>();

    [Header("Bench / Level Settings")]
    public int benchCapacity = 3;
    public int initialBoxCount = 3;
}

