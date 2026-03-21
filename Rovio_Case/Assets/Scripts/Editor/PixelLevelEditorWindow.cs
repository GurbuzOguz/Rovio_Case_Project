using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class PixelLevelEditorWindow : EditorWindow
{
    private enum PaletteMode
    {
        UseLevelPalette,
        CreatePaletteFromTexture
    }

    private LevelLayout _targetLevel;
    private Texture2D _sourceTexture;
    private PaletteMode _paletteMode = PaletteMode.CreatePaletteFromTexture;
    private ProductPalette _paletteOverride;
    private string _newPaletteName = "ProductPalette_FromTexture";
    private string _newPaletteFolder = "Assets/Scriptable Objects/Configs/Color_Palettes";
    private bool _replaceLevelPalette = true;
    private bool _flipY = true;
    private bool _transparentPixelsToBlack = true;
    private float _alphaThreshold = 0.1f;
    private int _maxPaletteColors = 16;
    private float _perceptualSimilarity = 2f;

    [MenuItem("Tools/Level/Pixel Level Editor")]
    public static void Open()
    {
        var window = GetWindow<PixelLevelEditorWindow>("Pixel Level Editor");
        window.minSize = new Vector2(460f, 520f);
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Texture -> Level Layout", EditorStyles.boldLabel);
        EditorGUILayout.Space(4f);

        _targetLevel = (LevelLayout)EditorGUILayout.ObjectField("Target Level Layout", _targetLevel, typeof(LevelLayout), false);
        _sourceTexture = (Texture2D)EditorGUILayout.ObjectField("Source Texture", _sourceTexture, typeof(Texture2D), false);

        using (new EditorGUILayout.VerticalScope("box"))
        {
            EditorGUILayout.LabelField("Palette", EditorStyles.boldLabel);
            _paletteMode = (PaletteMode)EditorGUILayout.EnumPopup("Palette Mode", _paletteMode);

            if (_paletteMode == PaletteMode.UseLevelPalette)
            {
                _paletteOverride = (ProductPalette)EditorGUILayout.ObjectField(
                    "Palette Override (Optional)",
                    _paletteOverride,
                    typeof(ProductPalette),
                    false);
            }
            else
            {
                _newPaletteName = EditorGUILayout.TextField("New Palette Name", _newPaletteName);
                _newPaletteFolder = EditorGUILayout.TextField("Palette Folder", _newPaletteFolder);
                _replaceLevelPalette = EditorGUILayout.Toggle("Assign New Palette To Level", _replaceLevelPalette);
                _maxPaletteColors = EditorGUILayout.IntSlider("Max Colors", _maxPaletteColors, 2, 64);
                _perceptualSimilarity = EditorGUILayout.Slider("Color Similarity (Perceptual)", _perceptualSimilarity, 0.05f, 3f);
            }
        }

        using (new EditorGUILayout.VerticalScope("box"))
        {
            EditorGUILayout.LabelField("Sampling", EditorStyles.boldLabel);
            _flipY = EditorGUILayout.Toggle("Flip Y (texture top -> grid top)", _flipY);
            _transparentPixelsToBlack = EditorGUILayout.Toggle("Transparent Pixels -> Black", _transparentPixelsToBlack);
            using (new EditorGUI.DisabledScope(!_transparentPixelsToBlack))
            {
                _alphaThreshold = EditorGUILayout.Slider("Alpha Threshold", _alphaThreshold, 0f, 1f);
            }
        }

        EditorGUILayout.Space(8f);
        using (new EditorGUI.DisabledScope(!CanGenerate()))
        {
            if (GUILayout.Button("Generate Level From Texture", GUILayout.Height(34f)))
            {
                Generate();
            }
        }

        EditorGUILayout.Space(8f);
        DrawHints();
    }

    private bool CanGenerate()
    {
        return _targetLevel != null && _targetLevel.gridConfig != null && _sourceTexture != null;
    }

    private void DrawHints()
    {
        EditorGUILayout.HelpBox(
            "Texture import settings must have Read/Write Enabled turned on. " +
            "For pixel art, Filter Mode: Point and low/no compression are recommended.",
            MessageType.Info);
    }

    private void Generate()
    {
        if (!TryGetReadablePixels(_sourceTexture, out Color[] pixels, out int texW, out int texH))
        {
            return;
        }

        int gridW = Mathf.Max(1, _targetLevel.gridConfig.columns);
        int gridH = Mathf.Max(1, _targetLevel.gridConfig.rows);

        ProductPalette workingPalette = ResolveWorkingPalette(pixels, texW, texH);
        if (workingPalette == null || workingPalette.entries == null || workingPalette.entries.Count == 0)
        {
            EditorUtility.DisplayDialog("Pixel Level Editor", "Palette could not be created or is empty.", "OK");
            return;
        }

        var cells = BuildCellsFromTexture(pixels, texW, texH, gridW, gridH, workingPalette);

        Undo.RecordObject(_targetLevel, "Generate Level From Texture");
        _targetLevel.products = cells;
        if (_paletteMode == PaletteMode.CreatePaletteFromTexture && _replaceLevelPalette)
        {
            _targetLevel.productPalette = workingPalette;
        }
        EditorUtility.SetDirty(_targetLevel);
        AssetDatabase.SaveAssets();

        EditorUtility.DisplayDialog(
            "Pixel Level Editor",
            $"Level updated.\nCells: {cells.Count}\nPalette Colors: {workingPalette.entries.Count}",
            "OK");
    }

    private ProductPalette ResolveWorkingPalette(Color[] pixels, int texW, int texH)
    {
        if (_paletteMode == PaletteMode.UseLevelPalette)
        {
            var selectedPalette = _paletteOverride != null ? _paletteOverride : _targetLevel.productPalette;
            if (selectedPalette == null)
            {
                EditorUtility.DisplayDialog("Pixel Level Editor", "UseLevelPalette is selected but no palette is assigned.", "OK");
            }
            return selectedPalette;
        }

        var colors = ExtractDistinctColors(pixels, texW, texH);
        if (colors.Count == 0)
        {
            EditorUtility.DisplayDialog("Pixel Level Editor", "No colors could be extracted from the texture.", "OK");
            return null;
        }

        string folder = string.IsNullOrWhiteSpace(_newPaletteFolder)
            ? "Assets"
            : _newPaletteFolder.Trim();

        if (!AssetDatabase.IsValidFolder(folder))
        {
            EditorUtility.DisplayDialog("Pixel Level Editor", $"Invalid folder: {folder}", "OK");
            return null;
        }

        var palette = ScriptableObject.CreateInstance<ProductPalette>();
        palette.entries = new List<ProductColorEntry>(colors.Count);
        for (int i = 0; i < colors.Count; i++)
        {
            palette.entries.Add(new ProductColorEntry
            {
                colorId = i,
                displayColor = colors[i]
            });
        }

        string baseName = string.IsNullOrWhiteSpace(_newPaletteName) ? "ProductPalette_FromTexture" : _newPaletteName.Trim();
        string path = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{baseName}.asset");
        AssetDatabase.CreateAsset(palette, path);
        EditorUtility.SetDirty(palette);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        return palette;
    }

    private List<ProductCellData> BuildCellsFromTexture(
        Color[] pixels,
        int texW,
        int texH,
        int gridW,
        int gridH,
        ProductPalette palette)
    {
        var cells = new List<ProductCellData>(gridW * gridH);
        var paletteEntries = palette.entries;
        if (paletteEntries == null || paletteEntries.Count == 0)
        {
            return cells;
        }

        float sx = texW / (float)gridW;
        float sy = texH / (float)gridH;

        for (int y = 0; y < gridH; y++)
        {
            for (int x = 0; x < gridW; x++)
            {
                int tx = Mathf.Clamp(Mathf.FloorToInt((x + 0.5f) * sx), 0, texW - 1);
                int tySample = Mathf.Clamp(Mathf.FloorToInt((y + 0.5f) * sy), 0, texH - 1);
                int ty = _flipY ? (texH - 1 - tySample) : tySample;
                Color c = pixels[ty * texW + tx];

                if (_transparentPixelsToBlack && c.a <= _alphaThreshold)
                {
                    c = Color.black;
                }

                int colorId = FindClosestPaletteColorId(c, paletteEntries);
                cells.Add(new ProductCellData
                {
                    x = x,
                    y = y,
                    colorId = colorId
                });
            }
        }

        return cells;
    }

    private int FindClosestPaletteColorId(Color c, List<ProductColorEntry> entries)
    {
        int bestId = entries[0].colorId;
        float bestDist = float.PositiveInfinity;

        for (int i = 0; i < entries.Count; i++)
        {
            var p = entries[i].displayColor;
            float dr = c.r - p.r;
            float dg = c.g - p.g;
            float db = c.b - p.b;
            float d = dr * dr + dg * dg + db * db;
            if (d < bestDist)
            {
                bestDist = d;
                bestId = entries[i].colorId;
            }
        }

        return bestId;
    }

    private List<Color> ExtractDistinctColors(Color[] pixels, int texW, int texH)
    {
        var clusters = new List<ColorCluster>();
        for (int y = 0; y < texH; y++)
        {
            for (int x = 0; x < texW; x++)
            {
                Color c = pixels[y * texW + x];
                if (_transparentPixelsToBlack && c.a <= _alphaThreshold)
                {
                    c = Color.black;
                }

                Color rgb = new Color(c.r, c.g, c.b, 1f);
                int bestIdx = -1;
                float bestDist = float.PositiveInfinity;

                for (int i = 0; i < clusters.Count; i++)
                {
                    float d = GetPerceptualDistance(clusters[i].MeanColor, rgb);
                    if (d < _perceptualSimilarity && d < bestDist)
                    {
                        bestDist = d;
                        bestIdx = i;
                    }
                }

                if (bestIdx >= 0)
                {
                    clusters[bestIdx] = clusters[bestIdx].Add(rgb);
                }
                else if (clusters.Count < _maxPaletteColors)
                {
                    clusters.Add(ColorCluster.From(rgb));
                }
            }
        }

        // If capped by max colors, merge closest clusters until within the limit.
        while (clusters.Count > _maxPaletteColors)
        {
            int a = -1;
            int b = -1;
            float best = float.PositiveInfinity;
            for (int i = 0; i < clusters.Count; i++)
            {
                for (int j = i + 1; j < clusters.Count; j++)
                {
                    float d = GetPerceptualDistance(clusters[i].MeanColor, clusters[j].MeanColor);
                    if (d < best)
                    {
                        best = d;
                        a = i;
                        b = j;
                    }
                }
            }

            if (a < 0 || b < 0)
            {
                break;
            }

            clusters[a] = clusters[a].Merge(clusters[b]);
            clusters.RemoveAt(b);
        }

        var colors = new List<Color>(clusters.Count);
        for (int i = 0; i < clusters.Count; i++)
        {
            colors.Add(clusters[i].MeanColor);
        }

        return colors;
    }

    private static float GetPerceptualDistance(Color a, Color b)
    {
        Color.RGBToHSV(a, out float ah, out float @as, out float av);
        Color.RGBToHSV(b, out float bh, out float bs, out float bv);

        float dh = Mathf.Abs(ah - bh);
        dh = Mathf.Min(dh, 1f - dh); // circular hue distance
        float ds = Mathf.Abs(@as - bs);
        float dv = Mathf.Abs(av - bv);

        // Weighted to favor hue differences visible to the eye.
        return (dh * 2.4f) + (ds * 1.0f) + (dv * 0.8f);
    }

    private struct ColorCluster
    {
        public Color Sum;
        public int Count;

        public Color MeanColor => Count > 0
            ? new Color(Sum.r / Count, Sum.g / Count, Sum.b / Count, 1f)
            : Color.black;

        public static ColorCluster From(Color c)
        {
            return new ColorCluster
            {
                Sum = new Color(c.r, c.g, c.b, 1f),
                Count = 1
            };
        }

        public ColorCluster Add(Color c)
        {
            return new ColorCluster
            {
                Sum = new Color(Sum.r + c.r, Sum.g + c.g, Sum.b + c.b, 1f),
                Count = Count + 1
            };
        }

        public ColorCluster Merge(ColorCluster other)
        {
            return new ColorCluster
            {
                Sum = new Color(Sum.r + other.Sum.r, Sum.g + other.Sum.g, Sum.b + other.Sum.b, 1f),
                Count = Count + other.Count
            };
        }
    }

    private bool TryGetReadablePixels(Texture2D texture, out Color[] pixels, out int w, out int h)
    {
        pixels = null;
        w = 0;
        h = 0;
        if (texture == null)
        {
            return false;
        }

        try
        {
            pixels = texture.GetPixels();
            w = texture.width;
            h = texture.height;
            return true;
        }
        catch
        {
            string path = AssetDatabase.GetAssetPath(texture);
            if (!string.IsNullOrEmpty(path))
            {
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer != null && !importer.isReadable)
                {
                    EditorUtility.DisplayDialog(
                        "Pixel Level Editor",
                        "Texture is not readable. Enable Read/Write in import settings.",
                        "OK");
                    Selection.activeObject = texture;
                    EditorGUIUtility.PingObject(texture);
                    return false;
                }
            }

            EditorUtility.DisplayDialog("Pixel Level Editor", "Texture could not be read.", "OK");
            return false;
        }
    }
}
