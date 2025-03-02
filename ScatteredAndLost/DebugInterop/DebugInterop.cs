using DebugMod;
using HK8YPlando.IC;
using System;

namespace HK8YPlando.DebugInterop;

internal static class DebugInterop
{
    internal static void Setup() => DebugMod.DebugMod.AddToKeyBindList(typeof(DebugInterop));

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

        if (mod.Checkpoint != null) mod.Checkpoint = Data.CheckpointLevel.Entrance;
        mod.DefeatedBrettorLords = false;
    }
}
