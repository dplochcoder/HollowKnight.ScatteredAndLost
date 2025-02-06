using HK8YPlando.Scripts.SharedLib;
using System.Collections.Generic;

namespace HK8YPlando.Util;

internal static class KnightUtil
{
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
}
