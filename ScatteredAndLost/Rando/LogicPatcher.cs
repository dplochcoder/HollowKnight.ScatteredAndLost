using HK8YPlando.Data;
using HK8YPlando.IC;
using RandomizerCore.Logic;
using RandomizerMod.RandomizerData;
using RandomizerMod.RC;
using RandomizerMod.Settings;

namespace HK8YPlando.Rando;

internal static class LogicPatcher
{
    public static void Setup()
    {
        RCData.RuntimeLogicOverride.Subscribe(101f, ModifyLogic);
        RequestBuilder.OnUpdate.Subscribe(-1000f, ModifyRequestBuilder);
        ProgressionInitializer.OnCreateProgressionInitializer += OnCreateProgressionInitializer;
    }

    private static void ModifyLogic(GenerationSettings gs, LogicManagerBuilder lmb)
    {
        var settings = ScatteredAndLostMod.Settings.RandomizerSettings;
        if (settings.EnableHeartDoors)
        {
            var (cost1, cost2) = settings.ComputeDoorCosts(gs);
            lmb.AddWaypoint(new("BrettaHouseGate1", $"{BrettaHeart.TermName}>{cost1 - 1}"));
            lmb.AddWaypoint(new("BrettaHouseGate2", $"{BrettaHeart.TermName}>{cost2 - 1}"));
        }

        foreach (var e in RandomizerData.Transitions)
        {
            if (e.Value.Logic != null) lmb.AddTransition(new(e.Key, e.Value.Logic));
        }

        foreach (var e in RandomizerData.Logic)
        {
            lmb.GetOrAddTerm(e.Key, TermType.State);
            lmb.AddLogicDef(new(e.Key, e.Value));
        }

        foreach (var e in RandomizerData.Waypoints) lmb.AddWaypoint(new(e.Key, e.Value));
    }

    private static void ModifyRequestBuilder(RequestBuilder rb)
    {
        var settings = ScatteredAndLostMod.Settings.RandomizerSettings;
        if (!settings.Enabled) return;

        RandomizerSettings.LocalSettings = settings.Clone();

        if (settings.EnableCheckpoints && rb.gs.TransitionSettings.Mode == TransitionSettings.TransitionMode.RoomRandomizer)
            throw new System.ArgumentException("Bretta House checkpoints are incompatible with room rando");
        if (settings.RandomizeSoulTotems && !rb.gs.PoolSettings.SoulTotems)
            throw new System.ArgumentException("Soul Totems must be randomized if randomizing Bretta House Soul Totems");

        rb.RemoveTransitionByName("Town[door_bretta]");
        rb.RemoveTransitionByName("Room_Bretta[right1]");

        foreach (var e in RandomizerData.Transitions)
        {
            rb.EditTransitionRequest(e.Key, info =>
            {
                info.getTransitionDef = () => e.Value.Def!;
            });
        }

        if (settings.EnableHeartDoors)
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

            int numHearts = settings.ComputeDoorCosts(rb.gs).Item2 + settings.HeartTolerance;

            System.Random r = new(rb.gs.Seed + 13);
            for (int i = 0; i < numHearts; i++) rb.AddItemByName(allHearts[r.Next(allHearts.Count)].name);
        }
        else
        {
            rb.EditTransitionRequest("Town[door_bretta]", info =>
            {
                info.AddGetTransitionDefModifier("Town[door_bretta]", def => def with { VanillaTarget = "BrettaHouseZippers[right1]" });
            });
            rb.EditTransitionRequest("BrettaHouseZippers[right1]", info =>
            {
                info.AddGetTransitionDefModifier("BrettaHouseZippers[right1]", def => def with { VanillaTarget = "Town[door_bretta]", IsTitledAreaTransition = true });
            });
        }

        foreach (var loc in RandomizerData.Locations)
        {
            if (settings.EnableHeartDoors || loc.Value.Checkpoint > CheckpointLevel.Entrance)
            {
                rb.EditLocationRequest(loc.Key, info =>
                {
                    info.getLocationDef = () => loc.Value.GetLocationDef();
                });
            }
        }
    }

    private static void OnCreateProgressionInitializer(LogicManager lm, GenerationSettings gs, ProgressionInitializer prog)
    {
        var settings = RandomizerSettings.LocalSettings;
        if (!settings.Enabled || !settings.EnableHeartDoors) return;

        prog.Setters.Add(new(lm.GetTermStrict(BrettaHeart.TermName), -settings.HeartTolerance));
    }
}
