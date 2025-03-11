using DebugMod;
using HK8YPlando.Data;
using HK8YPlando.IC;

namespace HK8YPlando.DebugInterop;

internal static class DebugInterop
{
    internal static void Setup() => DebugMod.DebugMod.AddToKeyBindList(typeof(DebugInterop));

    private static void GiveNailUpgrade()
    {
        var pd = PlayerData.instance;
        pd.SetBool(nameof(pd.honedNail), true);
        pd.IntAdd(nameof(pd.nailDamage), 4);
        PlayMakerFSM.BroadcastEvent("UPDATE NAIL DAMAGE");
        pd.IncrementInt(nameof(pd.nailSmithUpgrades));
    }

    [BindableMethod(name = "Gear up for Content", category = "Scattered and Lost")]
    public static void GearUpForContent()
    {
        Console.AddLine("Rescuing Bretta and granting late game gear");
        var pd = PlayerData.instance;

        pd.SetBool(nameof(PlayerData.brettaRescued), true);
        BindableFunctions.GiveAllSkills();
        BindableFunctions.GiveAllCharms();
        while (pd.GetInt(nameof(pd.maxHealthBase)) < 8) BindableFunctions.GiveMask();
        while (pd.GetInt(nameof(pd.MPReserveMax)) < 66) BindableFunctions.GiveVessel();
        while (pd.GetInt(nameof(pd.nailSmithUpgrades)) < 3) GiveNailUpgrade();
        PlayerData.instance.charmSlots = 9;
    }

    [BindableMethod(name = "Give Heart", category = "Scattered and Lost")]
    public static void GiveHeart() => ++BrettasHouse.Get().Hearts;

    [BindableMethod(name = "Give 100 Hearts", category = "Scattered and Lost")]
    public static void Give100Hearts()
    {
        var mod = BrettasHouse.Get();
        mod.Hearts += 100;
    }

    [BindableMethod(name = "Take All Hearts", category = "Scattered and Lost")]
    public static void TakeAllHearts() => BrettasHouse.Get().Hearts = 0;

    [BindableMethod(name = "Reset Bretta House", category = "Scattered and Lost")]
    public static void ResetBrettaHouse()
    {
        var mod = BrettasHouse.Get();
        foreach (var data in mod.DoorData)
        {
            data.NumUnlocked = 0;
            data.Opened = false;
            data.Closed = false;
        }

        if (mod.Checkpoint != null)
        {
            mod.Checkpoint = mod.EnableHeartDoors ? Data.CheckpointLevel.Entrance : Data.CheckpointLevel.Zippers;
            mod.BrettaDoorRedirected(mod.Checkpoint.Value.SceneAndGate());
        }
        mod.DefeatedBrettorLords = false;
    }
}
