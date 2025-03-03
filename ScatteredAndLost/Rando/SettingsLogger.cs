using RandomizerMod.Logging;

namespace HK8YPlando.Rando;

internal class SettingsLogger : RandoLogger
{
    public override void Log(LogArguments args)
    {
        if (RandoInterop.IsEnabled) LogManager.Write(tw => PurenailCore.SystemUtil.JsonUtil<ScatteredAndLostMod>.Serialize(RandoInterop.LS, tw), "ScatteredAndLostSpoiler.json");
    }
}
