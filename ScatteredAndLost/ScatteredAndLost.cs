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

        AddMinerDoor();
    }

    public static ScatteredAndLostSettings Settings = new();

    public bool ToggleButtonInsideMenu => throw new NotImplementedException();

    public void OnLoadGlobal(ScatteredAndLostSettings s) => Settings = s;

    public ScatteredAndLostSettings OnSaveGlobal() => Settings;

    private static void AddMinerDoor()
    {
        var crownDoor = DoorData.GetDoor("Crown")!;

        DoorData minersDoor = new()
        {
            CamelCaseName = "Miner",
            UpperCaseName = "MINER",
            UIName = "Miner",
            Door = new()
            {
                Sprite = crownDoor.Door!.Sprite,
                LeftLocation = new()
                {
                    SceneName = "Crossroads_45",
                    GateName = "left1",
                    NoKeyDesc = "A crystallized door echoing of light and song.",
                    KeyDesc = "The purgatorial chaos of transition rando beckons.<br>Insert the Miner's Key?",
                    Masks = [],
                    RequiresLantern = false,
                    X = 1.8f,
                    Y = 10.3f,
                    Decorators = [],
                },
                RightLocation = new()
                {
                    SceneName = "Crossroads_14",
                    GateName = "right2",
                    NoKeyDesc = "A crystallized door echoing of light and song.",
                    KeyDesc = "The call of the mines reigns out.<br>Insert the Miner's Key?",
                    Masks = [],
                    RequiresLantern = false,
                    X = 31.2f,
                    Y = 8.3f,
                    Decorators = [],
                },
                Mode = DoorData.DoorInfo.SplitMode.Normal,
                Deployers = [],
            },
            Key = new()
            {
                ItemName = "MoreDoors-Miner's_Key",
                UIItemName = "Miner's Key",
                ShopDesc = "A precious little soul. Such a shame...",
                InvDesc = "A delicate key filled with spirit. It gleams with a twinkle of orange light.",
                UsedInvDesc = "Damned are those who prey on innocence.",
                Sprite = new IC.EmbeddedSprite("miner_key"),
                Location = new DualLocation()
                {
                    Test = new SDBool("Mushroom Roller", "Fungus2_23")
                    {
                        semiPersistent = true,
                    },
                    falseLocation = new ShinyEnemyLocation()
                    {
                        HintShinyScale = 1.5f,
                        HintShinyX = 0,
                        HintShinyY = 0,
                        objectName = "Mushroom Roller",
                        removeGeo = false,
                        forceShiny = false,
                        name = "MoreDoors-Miner's_Key-Mushroom_Roller-Bretta",
                        sceneName = "Fungus2_23",
                        flingType = FlingType.Everywhere,
                    },
                    trueLocation = new CoordinateLocation()
                    {
                        x = 25,
                        y = 6,
                        elevation = 0,
                        managed = false,
                        forceShiny = false,
                        name = "MoreDoors-Miner's_Key-Mushroom_Roller-Bretta",
                        sceneName = "Fungus2_23",
                        flingType = FlingType.Everywhere,
                    },
                    name = "MoreDoors-Miner's_Key-Mushroom_Roller-Bretta",
                    sceneName = "Fungus2_23",
                    flingType = FlingType.Everywhere
                },
                Logic = "Fungus2_23[right1]"
            }
        };

        DoorData.AddExtensionDoor("Miner", minersDoor);
    }

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
        if (ModHooks.GetMod("Randomizer 4") is Mod) SetupRando();

        On.UIManager.StartNewGame += (orig, self, pd, br) =>
        {
            if (Settings.EnableInVanilla)
            {
                ItemChangerMod.CreateSettingsProfile(false);
                ItemChangerMod.Modules.Add<BinocularsModule>();
                ItemChangerMod.Modules.Add<BumperModule>();
                var mod = ItemChangerMod.Modules.Add<BrettasHouse>();

                mod.EnabledHeartDoors = false;
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
