using UnityEngine;

[CreateAssetMenu(fileName = "BoxConfig", menuName = "Game/Box Config")]
public class BoxConfig : ScriptableObject
{
    [Header("Identity")]
    public int colorId;

    [Header("Capacity")]
    [Min(1)]
    public int capacity = 3;

    [Header("Movement")]
    [Min(0.1f)]
    public float moveSpeed = 3f;
}

