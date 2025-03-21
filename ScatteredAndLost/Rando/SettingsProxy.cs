using RandoSettingsManager.SettingsManagement;
using RandoSettingsManager.SettingsManagement.Versioning;

namespace HK8YPlando.Rando;

public class SettingsProxy : RandoSettingsProxy<RandomizerSettings, string>
{
    public override string ModKey => nameof(ScatteredAndLostMod);

    public override VersioningPolicy<string> VersioningPolicy => new StrictModVersioningPolicy(ScatteredAndLostMod.Instance!);

    public override bool TryProvideSettings(out RandomizerSettings? settings)
    {
        settings = ScatteredAndLostMod.Settings.RandomizerSettings;
        return settings.Enabled;
    }

    public override void ReceiveSettings(RandomizerSettings? settings) => ConnectionMenu.Instance!.ApplySettings(settings ?? new());
}
