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

        // Unity tarafında zaten Solid Color seçili diyorsun ama güvenli yapmak için set ediyoruz.
        cam.clearFlags = CameraClearFlags.SolidColor;

        cam.backgroundColor = _levelLayout.backgroundColor;
    }
}

