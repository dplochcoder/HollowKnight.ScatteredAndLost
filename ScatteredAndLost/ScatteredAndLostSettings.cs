using MenuChanger.Attributes;
using RandomizerMod.Settings;

namespace HK8YPlando;

public class ScatteredAndLostSettings
{
    public bool EnableInVanilla = false;
    public bool EnableCheckpoints = true;

    public RandomizerSettings RandomizerSettings = new();
}

public class RandomizerSettings
{
    public bool Enabled = false;
    public bool EnableCheckpoints = true;
    public bool RandomizeSoulTotems = false;
    public bool EnableHeartDoors = true;

    [DynamicBound(nameof(MaxHearts), true)]
    [MenuRange(1, 50)]
    public int MinHearts = 3;

    [DynamicBound(nameof(MinHearts), false)]
    [MenuRange(1, 50)]
    public int MaxHearts = 10;

    [MenuRange(0, 50)]
    public int HeartTolerance = 2;

    public (int, int) ComputeDoorCosts(GenerationSettings gs)
    {
        System.Random r = new(gs.Seed + 117);
        int range = MaxHearts - MinHearts + 1;
        int d = r.Next(range);
        int d2 = (d + 1 + r.Next(range - 1)) % range;

        int a = d + MinHearts;
        int b = d2 + MinHearts;
        return a < b ? (a, b) : (b, a);
    }

    public RandomizerSettings Clone() => (RandomizerSettings)MemberwiseClone();
}
