using HK8YPlando.Data;
using HK8YPlando.IC;
using HK8YPlando.Scripts.SharedLib;
using HutongGames.PlayMaker.Actions;
using ItemChanger;
using Modding;
using Newtonsoft.Json;
using RandomizerCore.Extensions;
using RandomizerCore.Logic;
using RandomizerCore.LogicItems;
using RandomizerCore.StringItems;
using RandomizerMod.Logging;
using RandomizerMod.RandomizerData;
using RandomizerMod.RC;
using RandomizerMod.Settings;
using RandoSettingsManager;
using System.Collections.Generic;
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
        RequestBuilder.OnUpdate.Subscribe(1000f, ModifyRequestBuilder);
        SettingsLog.AfterLogSettings += LogSettings;

        // Register items.
        RandomizerData.Locations.Values.ForEach(l => Finder.DefineCustomLocation(l.Location!));
        BrettaHeart.All().ForEach(Finder.DefineCustomItem);
        Finder.DefineCustomItem(new SuperSoulTotemItem());
        Container.DefineContainer<SuperSoulTotemContainer>();

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
            mod.EnableHeartDoors = true;
            mod.EnablePreviews = LS.EnablePreviews;
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
        lmb.AddItem(new EmptyItem(SuperSoulTotemItem.NAME));

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

    private static bool RandomizeTransition(TransitionDef def, TransitionSettings.TransitionMode mode)
    {
        if (def.SceneName == "BrettaHouseEntry" && !LS.EnableHeartDoors) return false;

        return mode switch
        {
            TransitionSettings.TransitionMode.None => false,
            TransitionSettings.TransitionMode.MapAreaRandomizer => def.IsMapAreaTransition || def.IsTitledAreaTransition,
            TransitionSettings.TransitionMode.FullAreaRandomizer => def.IsTitledAreaTransition,
            TransitionSettings.TransitionMode.RoomRandomizer => true,
            _ => throw new System.ArgumentException($"Unsupported mode: {mode}")
        };
    }

    private static void ModifyRequestBuilder(RequestBuilder rb)
    {
        if (!IsEnabled) return;

        if (LS.EnableCheckpoints && rb.gs.TransitionSettings.Mode != TransitionSettings.TransitionMode.None)
            throw new System.ArgumentException("Bretta House checkpoints are incompatible with transition rando");
        if (LS.RandomizeSoulTotems && !rb.gs.PoolSettings.SoulTotems)
            throw new System.ArgumentException("Soul Totems must be randomized if randomizing Bretta House Soul Totems");

        rb.RemoveTransitionByName(BrettaDoorIn);
        rb.RemoveTransitionByName(BrettaDoorOut);
        rb.TransitionRequests.Remove(BrettaDoorIn);
        rb.TransitionRequests.Remove(BrettaDoorOut);
        rb.RemoveFromVanilla(BrettaDoorIn);
        rb.RemoveFromVanilla(BrettaDoorOut);

        bool matching = rb.gs.TransitionSettings.TransitionMatching == TransitionSettings.TransitionMatchingSetting.MatchingDirections
            || rb.gs.TransitionSettings.TransitionMatching == TransitionSettings.TransitionMatchingSetting.MatchingDirectionsAndNoDoorToDoor;
        var dualBuilder = rb.EnumerateTransitionGroups().FirstOrDefault(x => x.label == RBConsts.TwoWayGroup) as SelfDualTransitionGroupBuilder;
        var horizontalBuilder = rb.EnumerateTransitionGroups().FirstOrDefault(x => x.label == RBConsts.InLeftOutRightGroup) as SymmetricTransitionGroupBuilder;
        var verticalBuilder = rb.EnumerateTransitionGroups().FirstOrDefault(x => x.label == RBConsts.InTopOutBotGroup) as SymmetricTransitionGroupBuilder;

        List<string> doors = [];
        foreach (var e in RandomizerData.Transitions)
        {
            var def = e.Value.Def!;
            rb.EditTransitionRequest(e.Key, info =>
            {
                info.getTransitionDef = () => def;
            });

            if (RandomizeTransition(def, rb.gs.TransitionSettings.Mode))
            {
                if (!matching) dualBuilder!.Transitions.Add(e.Key);
                else if (def.Direction == TransitionDirection.Door) doors.Add(e.Key);
                else if (def.Direction == TransitionDirection.Right) horizontalBuilder!.Group1.Add(e.Key);
                else if (def.Direction == TransitionDirection.Left) horizontalBuilder!.Group2.Add(e.Key);
                else if (def.Direction == TransitionDirection.Bot) verticalBuilder!.Group1.Add(e.Key);
                else verticalBuilder!.Group2.Add(e.Key);
            }
            else
            {
                rb.AddToVanilla(new(def.VanillaTarget, e.Key));
                rb.EnsureVanillaSourceTransition(e.Key);
            }
        }

        if (matching)
        {
            if (doors.Count > 0)
            {
                rb.rng.PermuteInPlace(doors);
                foreach (var door in doors)
                {
                    switch (horizontalBuilder!.Group2.GetTotal() - horizontalBuilder.Group1.GetTotal())
                    {
                        case > 0:
                            horizontalBuilder.Group1.Add(door);
                            break;
                        case < 0:
                            horizontalBuilder.Group2.Add(door);
                            break;
                        case 0:
                            switch (verticalBuilder!.Group2.GetTotal() - verticalBuilder.Group1.GetTotal())
                            {
                                case > 0:
                                    verticalBuilder.Group1.Add(door);
                                    break;
                                case < 0:
                                    verticalBuilder.Group2.Add(door);
                                    break;
                                case 0:
                                    if (rb.rng.NextBool()) horizontalBuilder.Group1.Add(door);
                                    else horizontalBuilder.Group2.Add(door);
                                    break;
                            }
                            break;
                    }
                }
            }
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
            rb.RemoveTransitionByName("BrettaHouseEntry[left1]");
            rb.RemoveTransitionByName("BrettaHouseEntry[right1]");
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

        rb.EditItemRequest(SuperSoulTotemItem.NAME, info =>
        {
            info.getItemDef = () => new()
            {
                Name = SuperSoulTotemItem.NAME,
                Pool = PoolNames.Soul,
                MajorItem = false,
                PriceCap = 1,
            };
        });

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

            if (LS.RandomizeSoulTotems && loc.Value.Location is SuperSoulTotemLocation) rb.AddItemByName(SuperSoulTotemItem.NAME);
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
