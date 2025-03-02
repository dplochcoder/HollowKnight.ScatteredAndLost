using HK8YPlando.Scripts.SharedLib;
using ItemChanger;
using RandomizerMod.RandomizerData;
using System;
using System.Collections.Generic;
using JsonUtil = PurenailCore.SystemUtil.JsonUtil<HK8YPlando.ScatteredAndLostMod>;

namespace HK8YPlando.Data;

[Shim]
public enum CheckpointLevel
{
    Entrance,
    Zippers,
    Switches,
    Bumpers,
    Bubbles,
    Boss,
    Bretta
}

public static class CheckpointLevelExtensions
{
    public static (string, string) SceneAndGate(this CheckpointLevel self) => self switch
    {
        CheckpointLevel.Entrance => ("BrettaHouseEntry", "right1"),
        CheckpointLevel.Zippers => ("BrettaHouseZippers", "right1"),
        CheckpointLevel.Switches => ("BrettaHouseSwitches", "right1"),
        CheckpointLevel.Bumpers => ("BrettaHouseBumpers", "right1"),
        CheckpointLevel.Bubbles => ("BrettaHouseBubbles", "bot1"),
        CheckpointLevel.Boss => ("BrettaHouseBubbles", "top1"),
        CheckpointLevel.Bretta => ("Room_Bretta", "right1"),
        _ => throw new ArgumentException($"Unknown checkpoint level: {self}")
    };
}

public record LocationData
{
    public CheckpointLevel Checkpoint;
    public AbstractLocation? Location;
    public string? Logic;

    public LocationDef GetLocationDef() => new BrettaHouseLocationDef(Location!);
}

public record TransitionData
{
    public TransitionDef? Def;
    public string? Logic;
}

public record BrettaHouseLocationDef : LocationDef
{
    public BrettaHouseLocationDef(AbstractLocation location)
    {
        Name = location.name;
        SceneName = location.sceneName!;
        FlexibleCount = true;
        AdditionalProgressionPenalty = false;
    }

    public override string TitledArea { get => "Bretta's House"; }
    public override string MapArea { get => "Dirtmouth"; }
}

public static class RandomizerData
{
    private static T LoadEmbedded<T>(string name) where T : class => JsonUtil.DeserializeEmbedded<T>($"ScatteredAndLost.Resources.Data.{name}.json");

    public static SortedDictionary<string, TransitionData> Transitions = LoadEmbedded<SortedDictionary<string, TransitionData>>("transitions");

    public static SortedDictionary<string, LocationData> Locations = LoadEmbedded<SortedDictionary<string, LocationData>>("locations");

    public static SortedDictionary<string, string> Logic = LoadEmbedded<SortedDictionary<string, string>>("logic");

    public static SortedDictionary<string, string> Waypoints = LoadEmbedded<SortedDictionary<string, string>>("waypoints");

    static RandomizerData() => Locations.Values.ForEach(d => Finder.DefineCustomLocation(d.Location!));
}
