using System;
using UnityEngine;

public enum GridShiftDirection
{
    Left,
    Right,
    Down,
    Up
}

[Serializable]
public struct GridShiftMove
{
    public Vector2Int from;
    public Vector2Int to;
    public int colorId;

    public GridShiftMove(Vector2Int from, Vector2Int to, int colorId)
    {
        this.from = from;
        this.to = to;
        this.colorId = colorId;
    }
}

