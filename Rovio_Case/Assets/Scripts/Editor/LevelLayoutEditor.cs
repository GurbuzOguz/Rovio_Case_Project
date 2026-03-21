using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(LevelLayout))]
public class LevelLayoutEditor : Editor
{
    private const float SwatchSize = 24f;
    private const float SequenceSwatchSize = 28f;
    private const float SwatchPadding = 4f;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var level = (LevelLayout)target;
        if (level == null)
        {
            return;
        }

        EditorGUILayout.Space(10f);
        using (new EditorGUILayout.VerticalScope("box"))
        {
            EditorGUILayout.LabelField("Box Spawn Order Visual Editor", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Click a palette color to append it to custom spawn order. " +
                "Use Remove Last / Clear to edit quickly. Leave empty to use default logic.",
                MessageType.Info);

            DrawPaletteSwatches(level);
            EditorGUILayout.Space(6f);
            DrawSpawnSequence(level);
            EditorGUILayout.Space(4f);
            DrawSequenceActions(level);
        }
    }

    private static void DrawPaletteSwatches(LevelLayout level)
    {
        var entries = level.productPalette != null ? level.productPalette.entries : null;
        if (entries == null || entries.Count == 0)
        {
            EditorGUILayout.HelpBox("Assign a ProductPalette to use visual color buttons.", MessageType.Warning);
            return;
        }

        EditorGUILayout.LabelField("Palette Colors (click to add)", EditorStyles.miniBoldLabel);

        int columns = Mathf.Max(1, Mathf.FloorToInt((EditorGUIUtility.currentViewWidth - 60f) / (SwatchSize + SwatchPadding)));
        int col = 0;

        EditorGUILayout.BeginHorizontal();
        for (int i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];
            Color prev = GUI.backgroundColor;
            GUI.backgroundColor = entry.displayColor;
            if (GUILayout.Button(new GUIContent(" ", $"Add colorId {entry.colorId}"), GUILayout.Width(SwatchSize), GUILayout.Height(SwatchSize)))
            {
                Undo.RecordObject(level, "Add Spawn Color");
                level.customBoxSpawnColorOrder.Add(entry.colorId);
                EditorUtility.SetDirty(level);
            }
            GUI.backgroundColor = prev;

            col++;
            if (col >= columns && i < entries.Count - 1)
            {
                col = 0;
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
            }
        }
        EditorGUILayout.EndHorizontal();
    }

    private static void DrawSpawnSequence(LevelLayout level)
    {
        var order = level.customBoxSpawnColorOrder;
        if (order == null || order.Count == 0)
        {
            EditorGUILayout.HelpBox("Custom order is empty. Default spawn behavior will be used.", MessageType.None);
            return;
        }

        EditorGUILayout.LabelField("Current Custom Sequence", EditorStyles.miniBoldLabel);
        var paletteMap = BuildPaletteMap(level);

        int columns = 3;
        int col = 0;

        EditorGUILayout.BeginHorizontal();
        for (int i = 0; i < order.Count; i++)
        {
            int colorId = order[i];
            Color swatch = paletteMap.TryGetValue(colorId, out var c) ? c : Color.gray;

            Rect rect = GUILayoutUtility.GetRect(SequenceSwatchSize, SequenceSwatchSize, GUILayout.Width(SequenceSwatchSize), GUILayout.Height(SequenceSwatchSize));
            EditorGUI.DrawRect(rect, swatch);
            Handles.color = Color.black;
            Handles.DrawAAPolyLine(2f, new Vector3(rect.xMin, rect.yMin), new Vector3(rect.xMax, rect.yMin), new Vector3(rect.xMax, rect.yMax), new Vector3(rect.xMin, rect.yMax), new Vector3(rect.xMin, rect.yMin));
            GUI.Label(rect, (i + 1).ToString(), EditorStyles.whiteMiniLabel);

            // Click swatch to remove that specific step.
            if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
            {
                Undo.RecordObject(level, "Remove Spawn Color Step");
                order.RemoveAt(i);
                EditorUtility.SetDirty(level);
                Event.current.Use();
                break;
            }

            col++;
            if (col >= columns && i < order.Count - 1)
            {
                col = 0;
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
            }
        }
        EditorGUILayout.EndHorizontal();
    }

    private static void DrawSequenceActions(LevelLayout level)
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Remove Last"))
            {
                if (level.customBoxSpawnColorOrder != null && level.customBoxSpawnColorOrder.Count > 0)
                {
                    Undo.RecordObject(level, "Remove Last Spawn Color");
                    level.customBoxSpawnColorOrder.RemoveAt(level.customBoxSpawnColorOrder.Count - 1);
                    EditorUtility.SetDirty(level);
                }
            }

            if (GUILayout.Button("Clear Custom Order"))
            {
                Undo.RecordObject(level, "Clear Spawn Color Order");
                level.customBoxSpawnColorOrder.Clear();
                EditorUtility.SetDirty(level);
            }
        }
    }

    private static Dictionary<int, Color> BuildPaletteMap(LevelLayout level)
    {
        var map = new Dictionary<int, Color>();
        var entries = level.productPalette != null ? level.productPalette.entries : null;
        if (entries == null)
        {
            return map;
        }

        for (int i = 0; i < entries.Count; i++)
        {
            map[entries[i].colorId] = entries[i].displayColor;
        }

        return map;
    }
}
