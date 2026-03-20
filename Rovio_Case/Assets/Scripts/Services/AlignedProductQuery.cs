using UnityEngine;

public readonly struct AlignedProductQuery
{
    public readonly Vector3 WorldPosition;
    public readonly float AlignTolerance;
    public readonly int ColorId;

    public AlignedProductQuery(Vector3 worldPosition, float alignTolerance, int colorId)
    {
        WorldPosition = worldPosition;
        AlignTolerance = alignTolerance;
        ColorId = colorId;
    }
}
