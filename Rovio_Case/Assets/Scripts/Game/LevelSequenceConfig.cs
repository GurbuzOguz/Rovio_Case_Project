using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "LevelSequenceConfig", menuName = "Game/Level Sequence")]
public class LevelSequenceConfig : ScriptableObject
{
    public List<LevelLayout> levels = new List<LevelLayout>();
}

