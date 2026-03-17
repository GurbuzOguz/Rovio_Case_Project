using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ProductColorEntry
{
    public int colorId;
    public Color displayColor;
}

[CreateAssetMenu(fileName = "ProductPalette", menuName = "Game/Product Palette")]
public class ProductPalette : ScriptableObject
{
    public List<ProductColorEntry> entries = new List<ProductColorEntry>();
}

