using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "LevelLayout", menuName = "Game/Level Layout")]
public class LevelLayout : ScriptableObject
{
    [Header("Grid Reference")]
    public GridConfig gridConfig;

    [Header("Background")]
    [Tooltip("Background color for this level. Independent from product palette.")]
    public Color backgroundColor = new Color(0.12f, 0.12f, 0.12f, 1f);

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
    [Tooltip("BoxConfigs used at level start. Mapped by scene spawn point order.")]
    public List<BoxConfig> initialBoxConfigs = new List<BoxConfig>();

    [Tooltip("Optional box color spawn order (colorId sequence). If empty, default spawn logic is used.")]
    public List<int> customBoxSpawnColorOrder = new List<int>();
}


