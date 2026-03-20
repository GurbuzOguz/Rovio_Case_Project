using UnityEngine;
using Zenject;

public class LevelBackgroundColorInitializer : IInitializable
{
    private readonly LevelLayout _levelLayout;

    public LevelBackgroundColorInitializer(LevelLayout levelLayout)
    {
        _levelLayout = levelLayout;
    }

    public void Initialize()
    {
        if (_levelLayout == null || _levelLayout.productPalette == null)
        {
            return;
        }

        var cam = Camera.main;
        if (cam == null)
        {
            return;
        }

        // Force a safe camera clear setup even if already configured as Solid Color.
        cam.clearFlags = CameraClearFlags.SolidColor;

        cam.backgroundColor = _levelLayout.backgroundColor;
    }
}

