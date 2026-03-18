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
    [Range(0, 10)]
    public int benchCapacity = 5;

    [Range(0, 9)]
    public int initialBoxCount = 9;

    [Header("Boxes")]
    [Tooltip("Level başında hangi BoxConfig'lerin kullanılacağını belirtir. Sıra, sahnedeki spawn point sırasına göre eşlenir.")]
    public List<BoxConfig> initialBoxConfigs = new List<BoxConfig>();
}


