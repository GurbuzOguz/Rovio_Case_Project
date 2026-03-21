using UnityEditor;
using UnityEngine;

public class LevelQuickStartWindow : EditorWindow
{
    private const int DefaultVisibleButtonCount = 30;

    private LevelSequenceConfig _levelSequence;
    private bool _autoEnterPlayMode = true;
    private Vector2 _scroll;

    [MenuItem("Tools/Level/Level Quick Start")]
    public static void Open()
    {
        var window = GetWindow<LevelQuickStartWindow>("Level Quick Start");
        window.minSize = new Vector2(320f, 360f);
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Quick Level Start", EditorStyles.boldLabel);
        EditorGUILayout.Space(4f);

        _levelSequence = (LevelSequenceConfig)EditorGUILayout.ObjectField(
            "Level Sequence",
            _levelSequence,
            typeof(LevelSequenceConfig),
            false);

        _autoEnterPlayMode = EditorGUILayout.Toggle("Auto Enter Play Mode", _autoEnterPlayMode);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Set Level 1"))
            {
                SetLevelIndex(0);
            }

            if (GUILayout.Button("Reset To Level 1"))
            {
                SetLevelIndex(0);
            }
        }

        EditorGUILayout.Space(6f);

        int totalLevels = GetTotalLevelCount();
        EditorGUILayout.LabelField($"Detected Levels: {totalLevels}", EditorStyles.helpBox);

        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        int buttonCount = Mathf.Max(totalLevels, DefaultVisibleButtonCount);
        for (int i = 0; i < buttonCount; i++)
        {
            bool isRealLevel = i < totalLevels;
            using (new EditorGUI.DisabledScope(!isRealLevel && totalLevels > 0))
            {
                if (GUILayout.Button($"Start Level {i + 1}"))
                {
                    SetLevelIndex(i);
                }
            }
        }
        EditorGUILayout.EndScrollView();
    }

    private int GetTotalLevelCount()
    {
        if (_levelSequence == null || _levelSequence.levels == null)
        {
            return 0;
        }

        return Mathf.Max(0, _levelSequence.levels.Count);
    }

    private void SetLevelIndex(int index)
    {
        int safeIndex = Mathf.Max(0, index);
        PlayerPrefs.SetInt(LevelPrefsKeys.CurrentLevelIndex, safeIndex);
        PlayerPrefs.Save();

        Debug.Log($"[LevelQuickStart] Start index set to {safeIndex} (Level {safeIndex + 1}).");

        if (_autoEnterPlayMode && !EditorApplication.isPlaying)
        {
            EditorApplication.isPlaying = true;
        }
    }
}
