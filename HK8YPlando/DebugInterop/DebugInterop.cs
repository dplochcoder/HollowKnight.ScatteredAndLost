using DebugMod;
using HK8YPlando.IC;
using System;

namespace HK8YPlando.DebugInterop;

internal static class DebugInterop
{
    internal static void Setup() => DebugMod.DebugMod.AddToKeyBindList(typeof(DebugInterop));

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
