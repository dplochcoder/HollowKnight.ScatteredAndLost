using Architect.Content;
using HK8YPlando.IC;
using HK8YPlando.Rando;
using HK8YPlando.Scripts.Framework;
using HK8YPlando.Scripts.InternalLib;
using HK8YPlando.Scripts.Platforming;
using HK8YPlando.Scripts.SharedLib;
using HK8YPlando.Util;
using ItemChanger;
using ItemChanger.Internal.Menu;
using MenuChanger;
using Modding;
using Modding.Menu;
using Modding.Menu.Config;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using UnityEngine.UI;

namespace HK8YPlando;

public class ScatteredAndLostMod : Mod, IGlobalSettings<ScatteredAndLostSettings>, ICustomMenuMod
{
    public static ScatteredAndLostMod? Instance;

    internal static readonly string Version = PurenailCore.ModUtil.VersionUtil.ComputeVersion<ScatteredAndLostMod>();

    public override string GetVersion() => Version;

    public ScatteredAndLostMod() : base("ScatteredAndLost") { Instance = this; }

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

    public override int LoadPriority() => -1;

    private static void SetupArchitect() => ContentPacks.RegisterPack(new("Scattered & Lost", "Platforming assets borrowed from Celeste")
    {
        BubbleArchitectObject.Create(),
        BumperArchitectObject.Create(),
        CoinArchitectObject.Create(),
        CoinDoorArchitectObject.Create(),
        SuperSoulTotemArchitectObject.Create(),
        ZipperArchitectObject.Create()
    });

    private static bool IsRandoSave() => RandomizerMod.RandomizerMod.RS?.GenerationSettings != null;

    public override void Initialize(Dictionary<string, Dictionary<string, UnityEngine.GameObject>> preloadedObjects)
    {
        ScatteredAndLostPreloader.Instance.Initialize(preloadedObjects);
        ScatteredAndLostSceneManagerAPI.Load();
        SuperSoulTotemHooks.Load();
        BumperHooks.Load();

        if (ModHooks.GetMod("Architect") is Mod) SetupArchitect();
        if (ModHooks.GetMod("DebugMod") is Mod) SetupDebug();
        if (ModHooks.GetMod("Randomizer 4") is Mod) SetupRando();

        On.UIManager.StartNewGame += (orig, self, pd, br) =>
        {
            if (Settings.EnableInVanilla && (ModHooks.GetMod("Randomizer 4") == null || !IsRandoSave()))
            {
                ItemChangerMod.CreateSettingsProfile(false);
                ItemChangerMod.Modules.GetOrAdd<BinocularsModule>();
                var mod = ItemChangerMod.Modules.GetOrAdd<BrettasHouse>();

                mod.EnableHeartDoors = false;
                if (Settings.EnableCheckpoints) mod.Checkpoint = Data.CheckpointLevel.Zippers;
            }

            orig(self, pd, br);
        };
    }

    internal enum ExtractState
    {
        Working,
        Done,
    };

    private void ClickExtractButton(MenuButton button, Wrapped<ExtractState> state)
    {
        if (state.Value == ExtractState.Working) return;
        state.Value = ExtractState.Working;

        var text = button.gameObject.FindChild("Label").GetComponent<Text>();
        text.text = "Extracting...";

        Thread thread = new(() =>
        {
            string msg = "";
            try
            {
                string outputDir = Path.Combine(Path.GetDirectoryName(typeof(ScatteredAndLostMod).Assembly.Location), "Music");
                Directory.CreateDirectory(outputDir);

                var task = FmodRipper.ExtractMusic(FmodRipper.CelesteFmodPath(), FmodRipper.CelesteFmodMapping(), outputDir);
                task.Wait();
                msg = task.Result;
            }
            catch (Exception e)
            {
                BUG(e.ToString());
                msg = e.Message;
            }

            ThreadSupport.BeginInvoke(() =>
            {
                text.text = msg;
                state.Value = ExtractState.Done;
            });
        });
        thread.Start();
    }

    private void BuildExtractButton(ContentArea contentArea, MenuScreen returnScreen)
    {
        Wrapped<ExtractState> state = new(ExtractState.Done);
        MenuButtonConfig config = new()
        {
            Label = "Extract Celeste BGM",
            Description = new()
            {
                Text = "Extract OST from a Celeste installation on this machine",
            },
            Proceed = false,
            SubmitAction = menuButton => ClickExtractButton(menuButton, state),
            CancelAction = _ => UIManager.instance.UIGoToDynamicMenu(returnScreen),
        };

        contentArea.AddMenuButton(config.Label, config);
    }

    public MenuScreen GetMenuScreen(MenuScreen modListMenu, ModToggleDelegates? toggleDelegates)
    {
        ModMenuScreenBuilder builder = new("Scattered and Lost", modListMenu);
        builder.buildActions.Add(c => BuildExtractButton(c, modListMenu));
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

public class ScatteredAndLostDecorationMasterIntegration : Mod
{
    public override string GetVersion() => ScatteredAndLostMod.Version;

    private static void SetupDecorationMaster()
    {
        SuperSoulTotemDecoration.Register();
        ZipperDecoration.Register();
        CoinDecoration.Register();
        CoinDoorDecoration.Register();
        BumperDecoration.Register();
        BubbleDecoration.Register();

        DecorationMasterUtil.RefreshItemManager();
    }

    public override void Initialize()
    {
        if (ModHooks.GetMod("DecorationMaster") is Mod) SetupDecorationMaster();
    }
}
