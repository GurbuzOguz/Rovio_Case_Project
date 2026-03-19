using Lofelt.NiceVibrations;

public class HapticService : IHapticService
{
    public bool Enabled
    {
        get => HapticController.hapticsEnabled;
        set => HapticController.hapticsEnabled = value;
    }

    public void Selection()
    {
        Play(HapticPatterns.PresetType.Selection);
    }

    public void LightImpact()
    {
        Play(HapticPatterns.PresetType.LightImpact);
    }

    public void MediumImpact()
    {
        Play(HapticPatterns.PresetType.MediumImpact);
    }

    public void HeavyImpact()
    {
        Play(HapticPatterns.PresetType.HeavyImpact);
    }

    public void Success()
    {
        Play(HapticPatterns.PresetType.Success);
    }

    public void Warning()
    {
        Play(HapticPatterns.PresetType.Warning);
    }

    public void Failure()
    {
        Play(HapticPatterns.PresetType.Failure);
    }

    private static void Play(HapticPatterns.PresetType preset)
    {
        if (preset == HapticPatterns.PresetType.None)
        {
            return;
        }

        HapticPatterns.PlayPreset(preset);
    }
}
