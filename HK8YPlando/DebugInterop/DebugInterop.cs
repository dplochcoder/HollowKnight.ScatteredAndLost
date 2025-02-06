using DebugMod;
using HK8YPlando.IC;
using System;
using System.Linq.Expressions;

namespace HK8YPlando.DebugInterop;

internal static class DebugInterop
{
    internal static void Setup() => DebugMod.DebugMod.AddToKeyBindList(typeof(DebugInterop));

    [BindableMethod(name = "Gear Up", category ="HK8YPlando")]
    public static void GearUp()
    {
        BindableFunctions.GiveAllSkills();
        BindableFunctions.GiveAllCharms();
        BindableFunctions.ToggleInfiniteHP();
        BindableFunctions.ToggleInfiniteSoul();
        PlayerData.instance.SetBool("brettaRescued", true);
        BrettasHouse.Get().Hearts = 23;
    }

    [BindableMethod(name = "Give Heart", category = "HK8YPlando")]
    public static void GiveHeart() => ++BrettasHouse.Get().Hearts;

    [BindableMethod(name = "Give All Hearts", category = "HK8YPlando")]
    public static void GiveAllHearts()
    {
        var mod = BrettasHouse.Get();
        mod.Hearts = Math.Max(mod.Hearts, 23);
    }

    [BindableMethod(name = "Take All Hearts", category = "HK8YPlando")]
    public static void TakeAllHearts() => BrettasHouse.Get().Hearts = 0;

    [BindableMethod(name = "Reset Doors", category = "HK8YPlando")]
    public static void ResetDoors() => BrettasHouse.Get().DoorData.Clear();
}
