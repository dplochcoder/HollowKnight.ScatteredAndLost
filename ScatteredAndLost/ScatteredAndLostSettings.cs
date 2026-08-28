using System;
using MenuChanger.Attributes;
using RandomizerMod.Settings;

namespace HK8YPlando;

public class ScatteredAndLostSettings
{
    public bool EnableInVanilla = false;
    public bool EnableCheckpoints = true;
    public string CelesteInstallation = "";

    public RandomizerSettings RandomizerSettings = new();
}

file class CSRIgnoreAttribute : Attribute { }

public class RandomizerSettings
{
    public bool Enabled = false;
    public bool EnableCheckpoints = true;
    public bool RandomizeSoulTotems = false;
    public bool EnablePreviews = true;
    public bool EnableHeartDoors = true;

    internal const int MAX_HEART_REQUIREMENT = 50;

    [DynamicBound(nameof(MaxHearts), true)]
    [MenuRange(1, MAX_HEART_REQUIREMENT)]
    [CSRIgnore]
    public int MinHearts = 3;

    [DynamicBound(nameof(MinHearts), false)]
    [MenuRange(1, MAX_HEART_REQUIREMENT)]
    [CSRIgnore]
    public int MaxHearts = 10;

    [MenuRange(0, 10)]
    public int HeartTolerance = 2;

    public (int, int) ComputeDoorCosts(GenerationSettings gs)
    {
        if (MaxHearts - MinHearts <= 1)
            return (MinHearts, MaxHearts);

        System.Random r = new(gs.Seed + 117);
        int range = MaxHearts - MinHearts + 1;
        int d = r.Next(range);
        int d2 = (d + 1 + r.Next(range - 1)) % range;

        int a = d + MinHearts;
        int b = d2 + MinHearts;
        return a < b ? (a, b) : (b, a);
    }
}
