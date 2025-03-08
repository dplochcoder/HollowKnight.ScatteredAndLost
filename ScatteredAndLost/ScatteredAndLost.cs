using HK8YPlando.IC;
using HK8YPlando.Rando;
using ItemChanger;
using ItemChanger.Internal.Menu;
using ItemChanger.Locations;
using Modding;
using MoreDoors.Data;
using MoreDoors.IC;
using System;
using System.Collections;
using System.Collections.Generic;

namespace HK8YPlando;

public class ScatteredAndLostMod : Mod, IGlobalSettings<ScatteredAndLostSettings>, ICustomMenuMod
{
    public static ScatteredAndLostMod? Instance;

    public override string GetVersion() => PurenailCore.ModUtil.VersionUtil.ComputeVersion<ScatteredAndLostMod>();

    public ScatteredAndLostMod() : base("ScatteredAndLost")
    {
        Instance = this;
    }

    public static ScatteredAndLostSettings Settings = new();

    public bool ToggleButtonInsideMenu => throw new NotImplementedException();

    public void OnLoadGlobal(ScatteredAndLostSettings s) => Settings = s;

    public ScatteredAndLostSettings OnSaveGlobal() => Settings;

    public static new void Log(string msg) => ((ILogger)Instance!).Log(msg);

    public static void BUG(string msg) => Log($"BUG: {msg}");

    public static new void LogError(string msg) => ((ILogger)Instance!).LogError(msg);

    public override List<(string, string)> GetPreloadNames() => ScatteredAndLostPreloader.Instance.GetPreloadNames();

    public override (string, Func<IEnumerator>)[] PreloadSceneHooks() => ScatteredAndLostPreloader.Instance.PreloadSceneHooks();

    private static void SetupDebug() => DebugInterop.DebugInterop.Setup();

    private static void SetupRando() => RandoInterop.Setup();

    public override void Initialize(Dictionary<string, Dictionary<string, UnityEngine.GameObject>> preloadedObjects)
    {
        ScatteredAndLostPreloader.Instance.Initialize(preloadedObjects);
        ScatteredAndLostSceneManagerAPI.Load();

        if (ModHooks.GetMod("DebugMod") is Mod) SetupDebug();
        if (ModHooks.GetMod("MoreDoors") is Mod) MoreDoorsInterop.MoreDoorsInterop.AddMinerDoor();
        if (ModHooks.GetMod("Randomizer 4") is Mod) SetupRando();

        On.UIManager.StartNewGame += (orig, self, pd, br) =>
        {
            if (Settings.EnableInVanilla)
            {
                ItemChangerMod.CreateSettingsProfile(false);
                ItemChangerMod.Modules.Add<BinocularsModule>();
                ItemChangerMod.Modules.Add<BumperModule>();
                var mod = ItemChangerMod.Modules.Add<BrettasHouse>();

                mod.EnableHeartDoors = false;
                if (Settings.EnableCheckpoints) mod.Checkpoint = Data.CheckpointLevel.Zippers;
            }

            orig(self, pd, br);
        };
    }

    public MenuScreen GetMenuScreen(MenuScreen modListMenu, ModToggleDelegates? toggleDelegates)
    {
        ModMenuScreenBuilder builder = new("Scattered and Lost", modListMenu);
        builder.AddHorizontalOption(new()
        {
            Name = "Enable in Vanilla",
            Description = "If yes, Bretta's House will be expanded in vanilla saves.",
            Values = ["No", "Yes"],
            Saver = i => Settings.EnableInVanilla = i == 1,
            Loader = () => Settings.EnableInVanilla ? 1 : 0,
        });
        builder.AddHorizontalOption(new()
        {
            Name = "Enable Checkpoints",
            Description = "If yes, re-entering Bretta's house will always skip to the furthest accessed room.",
            Values = ["No", "Yes"],
            Saver = i => Settings.EnableCheckpoints = i == 1,
            Loader = () => Settings.EnableCheckpoints ? 1 : 0,
        });
        return builder.CreateMenuScreen();
    }
}
