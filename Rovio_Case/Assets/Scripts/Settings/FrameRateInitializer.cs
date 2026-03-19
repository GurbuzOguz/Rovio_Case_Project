using UnityEngine;
using Zenject;

public class FrameRateInitializer : IInitializable
{
    private const int DefaultMobileTargetFps = 60;
    private const int HighRefreshTargetFps = 120;

    public void Initialize()
    {
        if (!Application.isMobilePlatform)
        {
            return;
        }

        QualitySettings.vSyncCount = 0;
        int refreshHz = Mathf.RoundToInt((float)Screen.currentResolution.refreshRateRatio.value);
        int targetFps = refreshHz >= HighRefreshTargetFps ? HighRefreshTargetFps : DefaultMobileTargetFps;
        Application.targetFrameRate = targetFps;
    }
}
