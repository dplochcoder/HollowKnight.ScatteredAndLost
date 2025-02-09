using GlobalEnums;
using HK8YPlando.Scripts.SharedLib;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace HK8YPlando.Util;

internal static class KnightUtil
{
    public static float WIDTH = 0.5f;
    public static float HEIGHT = 1.2813f;

    private static readonly List<string> nailArts = ["Cyclone", "Dash", "Great"];

    internal static bool IsNailArtActive()
    {
        var knight = HeroController.instance.gameObject;
        var attacks = knight.FindChild("Attacks");
        foreach (var art in nailArts)
        {
            var obj = attacks.FindChild($"{art} Slash");
            if (obj.activeSelf) return true;
        }

        return false;
    }

    private static FieldInfo doubleJumpedField = typeof(HeroController).GetField("doubleJumped", BindingFlags.NonPublic | BindingFlags.Instance);
    internal static void SetDoubleJumped(this HeroController self, bool value) => doubleJumpedField.SetValue(self, value);

    private static FieldInfo airDashedField = typeof(HeroController).GetField("airDashed", BindingFlags.NonPublic | BindingFlags.Instance);
    internal static void SetAirDashed(this HeroController self, bool value) => airDashedField.SetValue(self, value);
}
