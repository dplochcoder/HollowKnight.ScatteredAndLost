using HK8YPlando.Data;
using HK8YPlando.IC;
using HK8YPlando.Scripts.SharedLib;
using ItemChanger;
using Modding;
using Newtonsoft.Json;
using RandomizerCore.Logic;
using RandomizerCore.StringItems;
using RandomizerMod.Logging;
using RandomizerMod.RandomizerData;
using RandomizerMod.RC;
using RandomizerMod.Settings;
using RandoSettingsManager;
using System.IO;
using System.Linq;

namespace HK8YPlando.Rando;

internal static class RandoInterop
{
    public static RandomizerSettings LS = new();

    public static bool IsEnabled => ScatteredAndLostMod.Settings.RandomizerSettings.Enabled;

    private static void SetupRSM() => RandoSettingsManagerMod.Instance.RegisterConnection(new SettingsProxy());

    public static void Setup()
    {
        CondensedSpoilerLogger.AddCategory("Bretta Hearts", _ => IsEnabled, new(BrettaHeart.All().Select(h => h.name)));
        ConnectionMenu.Setup();
        LogManager.AddLogger(new SettingsLogger());
        ProgressionInitializer.OnCreateProgressionInitializer += OnCreateProgressionInitializer;
        RandoController.OnBeginRun += CreateLocalSettings;
        RandoController.OnExportCompleted += ExportCompleted;
        RCData.RuntimeLogicOverride.Subscribe(-2000f, ModifyLogic);
        RequestBuilder.OnUpdate.Subscribe(-2000f, ModifyRequestBuilder);
        SettingsLog.AfterLogSettings += LogSettings;

        // Call Finder.
        RandomizerData.Locations.Values.ForEach(l => Finder.DefineCustomLocation(l.Location!));
        BrettaHeart.All().ForEach(Finder.DefineCustomItem);

        if (ModHooks.GetMod("RandoSettingsManager") is Mod) SetupRSM();
    }

    private static void CreateLocalSettings(RandoController rc)
    {
        if (!IsEnabled) return;
        LS = ScatteredAndLostMod.Settings.RandomizerSettings.Clone();
    }

    private static void ExportCompleted(RandoController rc)
    {
        if (!IsEnabled) return;

        ItemChangerMod.CreateSettingsProfile(false);
        ItemChangerMod.Modules.Add<BinocularsModule>();
        ItemChangerMod.Modules.Add<BumperModule>();
        var mod = ItemChangerMod.Modules.Add<BrettasHouse>();

        if (LS.EnableHeartDoors)
        {
            mod.EnabledHeartDoors = true;
            var (c1, c2) = LS.ComputeDoorCosts(rc.gs);
            mod.DoorData[0].Total = c1;
            mod.DoorData[1].Total = c2;
        }
        if (LS.RandomizeSoulTotems) mod.RandomizeSoulTotems = true;
        if (LS.EnableCheckpoints) mod.Checkpoint = LS.EnableHeartDoors ? CheckpointLevel.Entrance : CheckpointLevel.Zippers;
    }

    private static void LogSettings(LogArguments args, TextWriter tw)
    {
        if (!IsEnabled) return;

        tw.WriteLine("Logging ScatteredAndLost settings:");
        using JsonTextWriter jtw = new(tw) { CloseOutput = false };
        JsonUtil._js.Serialize(jtw, LS);
        tw.WriteLine();
    }

    private static void ModifyLogic(GenerationSettings gs, LogicManagerBuilder lmb)
    {
        lmb.GetOrAddTerm(SoulTotemsTerm);

        if (LS.EnableHeartDoors)
        {
            lmb.GetOrAddTerm(BrettaHeart.TermName);
            foreach (var h in BrettaHeart.All()) lmb.AddItem(new StringItemTemplate(h.name, $"{BrettaHeart.TermName}++"));

            var (cost1, cost2) = LS.ComputeDoorCosts(gs);
            lmb.AddWaypoint(new("BrettaHouseGate1", $"{BrettaHeart.TermName}>{cost1 - 1}", true));
            lmb.AddWaypoint(new("BrettaHouseGate2", $"{BrettaHeart.TermName}>{cost2 - 1}", true));
        }

        foreach (var e in RandomizerData.Transitions)
        {
            if (e.Value.Logic != null) lmb.AddTransition(new(e.Key, e.Value.Logic));
        }

        foreach (var e in RandomizerData.Logic) lmb.AddWaypoint(new(e.Key, e.Value, false));
        foreach (var e in RandomizerData.Waypoints) lmb.AddWaypoint(new(e.Key, e.Value, true));


        foreach (var loc in RandomizerData.Locations)
        {
            if (loc.Value.Logic != null) lmb.AddLogicDef(new(loc.Key, loc.Value.Logic));
        }
    }

    private const string BrettaDoorIn = "Town[door_bretta]";
    private const string BrettaDoorOut = "Room_Bretta[right1]";

    private static void ModifyRequestBuilder(RequestBuilder rb)
    {
        if (!IsEnabled) return;

        if (LS.EnableCheckpoints && rb.gs.TransitionSettings.Mode != TransitionSettings.TransitionMode.None)
            throw new System.ArgumentException("Bretta House checkpoints are incompatible with transition rando");
        if (LS.RandomizeSoulTotems && !rb.gs.PoolSettings.SoulTotems)
            throw new System.ArgumentException("Soul Totems must be randomized if randomizing Bretta House Soul Totems");

        rb.TransitionRequests.Remove(BrettaDoorIn);
        rb.TransitionRequests.Remove(BrettaDoorOut);
        rb.RemoveFromVanilla(BrettaDoorIn);
        rb.RemoveFromVanilla(BrettaDoorOut);

        foreach (var e in RandomizerData.Transitions)
        {
            var def = e.Value.Def!;
            rb.EditTransitionRequest(e.Key, info =>
            {
                info.getTransitionDef = () => def;
            });
            rb.AddToVanilla(new(def.VanillaTarget, e.Key));
        }

        if (LS.EnableHeartDoors)
        {
            if (!rb.gs.PoolSettings.Keys)
                throw new System.ArgumentException("Cannot enable heart doors without randomizing keys");

            var allHearts = BrettaHeart.All();
            foreach (var heart in allHearts)
            {
                rb.EditItemRequest(heart.name, info =>
                {
                    info.getItemDef = () => new()
                    {
                        Name = heart.name,
                        Pool = PoolNames.Key,
                        MajorItem = false,
                        PriceCap = 500,
                    };
                });
            }

            int numHearts = LS.ComputeDoorCosts(rb.gs).Item2 + LS.HeartTolerance;

            System.Random r = new(rb.gs.Seed + 13);
            for (int i = 0; i < numHearts; i++) rb.AddItemByName(allHearts[r.Next(allHearts.Count)].name);
        }
        else
        {
            rb.RemoveFromVanilla("BrettaHouseEntry[left1]");
            rb.RemoveFromVanilla("BrettaHouseEntry[right1]");

            rb.EditTransitionRequest(BrettaDoorIn, info =>
            {
                info.AddGetTransitionDefModifier(BrettaDoorIn, def => def with { VanillaTarget = "BrettaHouseZippers[right1]", IsTitledAreaTransition = true });
            });
            rb.RemoveFromVanilla(BrettaDoorIn);
            rb.AddToVanilla(new("BrettaHouseZippers[right1]", BrettaDoorIn));

            rb.EditTransitionRequest("BrettaHouseZippers[right1]", info =>
            {
                info.AddGetTransitionDefModifier("BrettaHouseZippers[right1]", def => def with { VanillaTarget = BrettaDoorIn, IsTitledAreaTransition = true });
            });
            rb.RemoveFromVanilla("BrettaHouseZippers[right1]");
            rb.AddToVanilla(new(BrettaDoorIn, "BrettaHouseZippers[right1]"));
        }

        foreach (var loc in RandomizerData.Locations)
        {
            if ((LS.EnableHeartDoors && loc.Value.Checkpoint == CheckpointLevel.Entrance) || LS.RandomizeSoulTotems)
            {
                rb.EditLocationRequest(loc.Key, info =>
                {
                    info.getLocationDef = () => loc.Value.GetLocationDef();
                });
                rb.AddLocationByName(loc.Key);
            }
        }
    }

    private const string SoulTotemsTerm = "BRETTAHOUSESOULTOTEMS";

    private static void OnCreateProgressionInitializer(LogicManager lm, GenerationSettings gs, ProgressionInitializer prog)
    {
        if (!LS.Enabled || !LS.EnableHeartDoors) return;

        prog.Setters.Add(new(lm.GetTermStrict(BrettaHeart.TermName), -LS.HeartTolerance));
        prog.Setters.Add(new(lm.GetTermStrict(SoulTotemsTerm), LS.RandomizeSoulTotems ? 0 : 1));
    }
}
