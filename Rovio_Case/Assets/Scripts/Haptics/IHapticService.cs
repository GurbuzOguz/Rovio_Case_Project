public interface IHapticService
{
    bool Enabled { get; set; }

    void Selection();
    void LightImpact();
    void MediumImpact();
    void HeavyImpact();
    void Success();
    void Warning();
    void Failure();
}
