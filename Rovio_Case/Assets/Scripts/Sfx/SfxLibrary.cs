using System;
using UnityEngine;

[CreateAssetMenu(fileName = "SfxLibrary", menuName = "Game/SFX Library")]
public class SfxLibrary : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        public SfxId id;
        public AudioClip clip;

        [Range(0f, 1f)] public float volume = 1f;
    }

    public Entry[] entries;

    [NonSerialized] public bool IsReady;

    public Entry GetEntry(SfxId id)
    {
        if (entries == null)
        {
            return null;
        }

        for (int i = 0; i < entries.Length; i++)
        {
            if (entries[i] != null && entries[i].id.Equals(id))
            {
                return entries[i];
            }
        }

        return null;
    }
}

