using HK8YPlando.Scripts.SharedLib;
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

    private static FieldInfo airDashedField = typeof(HeroController).GetField("airDashed", BindingFlags.NonPublic | BindingFlags.Instance);
    internal static void SetAirDashed(this HeroController self, bool value) => airDashedField.SetValue(self, value);

    private static FieldInfo doubleJumpedField = typeof(HeroController).GetField("doubleJumped", BindingFlags.NonPublic | BindingFlags.Instance);
    internal static void SetDoubleJumped(this HeroController self, bool value) => doubleJumpedField.SetValue(self, value);

    private static FieldInfo nailChargeTimerField = typeof(HeroController).GetField("nailChargeTimer", BindingFlags.NonPublic | BindingFlags.Instance);
    internal static void SetNailChargeTimer(this HeroController self, float value) => nailChargeTimerField.SetValue(self, value);

    private static MethodInfo cancelAttackMethod = typeof(HeroController).GetMethod("CancelAttack", BindingFlags.NonPublic | BindingFlags.Instance);
    internal static void CancelAttack(this HeroController self) => cancelAttackMethod.Invoke(self, []);

    private static MethodInfo cancelBounceMethod = typeof(HeroController).GetMethod("CancelBounce", BindingFlags.NonPublic | BindingFlags.Instance);
    internal static void CancelBounce(this HeroController self) => cancelBounceMethod.Invoke(self, []);

    private static MethodInfo cancelFallEffectsMethod = typeof(HeroController).GetMethod("CancelFallEffects", BindingFlags.NonPublic | BindingFlags.Instance);
    internal static void CancelFallEffects(this HeroController self) => cancelFallEffectsMethod.Invoke(self, []);

    private static MethodInfo cancelRecoilHorizontalMethod = typeof(HeroController).GetMethod("CancelRecoilHorizontal", BindingFlags.NonPublic | BindingFlags.Instance);
    internal static void CancelRecoilHorizontal(this HeroController self) => cancelRecoilHorizontalMethod.Invoke(self, []);
}
