using HK8YPlando.IC;
using ItemChanger;
using Modding;

namespace HK8YPlando;

public class HK8YPlandoMod : Mod
{
    private static HK8YPlandoMod? Instance;

    public override string GetVersion() => PurenailCore.ModUtil.VersionUtil.ComputeVersion<HK8YPlandoMod>();

    public HK8YPlandoMod() : base("HK8YPlando")
    {
        Instance = this;
    }

    public override void Initialize()
    {
        On.UIManager.StartNewGame += (orig, self, pd, br) =>
        {
            ItemChangerMod.CreateSettingsProfile(false);
            ItemChangerMod.Modules.Add<Balladrius>();

            orig(self, pd, br);
        };
    }
}
