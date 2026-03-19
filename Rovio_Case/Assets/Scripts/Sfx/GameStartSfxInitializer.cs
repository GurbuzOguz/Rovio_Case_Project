using Zenject;

public class GameStartSfxInitializer : IInitializable
{
    private readonly ISfxService _sfx;

    public GameStartSfxInitializer(ISfxService sfx)
    {
        _sfx = sfx;
    }

    public void Initialize()
    {
        _sfx?.Play(SfxId.GameStart);
        _sfx?.PlayLoop(SfxId.BgmLoop);
    }
}

