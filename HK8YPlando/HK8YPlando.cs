using HK8YPlando.IC;
using ItemChanger;
using Modding;
using System;
using System.Collections;
using System.Collections.Generic;

namespace HK8YPlando;

public class HK8YPlandoMod : Mod
{
    private static HK8YPlandoMod? Instance;

    public override string GetVersion() => PurenailCore.ModUtil.VersionUtil.ComputeVersion<HK8YPlandoMod>();

    public HK8YPlandoMod() : base("HK8YPlando")
    {
        Instance = this;
    }

    public static new void Log(string msg) => ((ILogger)Instance!).Log(msg);

    public static void BUG(string msg) => Log($"BUG: {msg}");

    public static new void LogError(string msg) => ((ILogger)Instance!).LogError(msg);

    public override List<(string, string)> GetPreloadNames() => HK8YPlandoPreloader.Instance.GetPreloadNames();

    public override (string, Func<IEnumerator>)[] PreloadSceneHooks() => HK8YPlandoPreloader.Instance.PreloadSceneHooks();

    public override void Initialize(Dictionary<string, Dictionary<string, UnityEngine.GameObject>> preloadedObjects)
    {
        HK8YPlandoPreloader.Instance.Initialize(preloadedObjects);
        HK8YPlandoSceneManagerAPI.Load();

        // FIXME: Disable, attach to save file.
        On.UIManager.StartNewGame += (orig, self, pd, br) =>
        {
            ItemChangerMod.CreateSettingsProfile(false);
            ItemChangerMod.Modules.Add<Balladrius>();
            ItemChangerMod.Modules.Add<BrettasHouseCSide>();

            orig(self, pd, br);
        };
    }
}
