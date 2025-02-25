using DebugMod;
using HK8YPlando.IC;
using System;

namespace HK8YPlando.DebugInterop;

internal static class DebugInterop
{
    internal static void Setup() => DebugMod.DebugMod.AddToKeyBindList(typeof(DebugInterop));

    [BindableMethod(name = "Gear Up", category ="Scattered and Lost")]
    public static void GearUp()
    {
        BindableFunctions.GiveAllSkills();
        BindableFunctions.GiveAllCharms();
        BindableFunctions.ToggleInfiniteHP();
        BindableFunctions.ToggleInfiniteSoul();
        PlayerData.instance.SetBool("brettaRescued", true);
        BrettasHouse.Get().Hearts = 23;
    }

    [BindableMethod(name = "Give Heart", category = "Scattered and Lost")]
    public static void GiveHeart() => ++BrettasHouse.Get().Hearts;

    [BindableMethod(name = "Give All Hearts", category = "Scattered and Lost")]
    public static void GiveAllHearts()
    {
        var mod = BrettasHouse.Get();
        mod.Hearts = Math.Max(mod.Hearts, 23);
    }

    [BindableMethod(name = "Take All Hearts", category = "Scattered and Lost")]
    public static void TakeAllHearts() => BrettasHouse.Get().Hearts = 0;

    [BindableMethod(name = "Reset Doors", category = "Scattered and Lost")]
    public static void ResetDoors() => BrettasHouse.Get().DoorData.Clear();

    [BindableMethod(name = "Reset Checkpoint", category = "Scattered and Lost")]
    public static void ResetCheckpoint()
    {
        var mod = BrettasHouse.Get();
        mod.CheckpointScene = "BrettaHouseEntry";
        mod.CheckpointGate = "right1";
        mod.CheckpointPriority = 0;
    }

    [BindableMethod(name = "Skip to End", category = "Scattered and Lost")]
    public static void SkipToEnd()
    {
        var mod = BrettasHouse.Get();
        mod.CheckpointScene = "BrettaHouseBubbles";
        mod.CheckpointGate = "bot1";
        mod.CheckpointPriority = 4;
    }
}
