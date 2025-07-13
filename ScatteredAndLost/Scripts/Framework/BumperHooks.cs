using HK8YPlando.Scripts.SharedLib;
using HK8YPlando.Util;
using HutongGames.PlayMaker.Actions;
using Modding;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace HK8YPlando.Scripts.Framework;

internal class BumperSpeedBehaviour : MonoBehaviour
{
    private float horzVelocity = 0;
    private float decel = 0;

    private void Update()
    {
        if (horzVelocity == 0) return;

        // TODO: Spells, recoil
        var hc = HeroController.instance;
        var cState = hc.cState;
        var hState = hc.hero_state;
        if (hState == GlobalEnums.ActorStates.hard_landing ||
            cState.onGround || cState.dashing || cState.casting ||
            cState.castRecoiling || cState.recoilingLeft || cState.recoilingRight || cState.wallSliding ||
            KnightUtil.IsNailArtActive())
        {
            horzVelocity = 0;
            return;
        }

        if (Mathf.Abs(horzVelocity) < decel * Time.deltaTime) horzVelocity = 0;
        else horzVelocity = Mathf.Sign(horzVelocity) * (Mathf.Abs(horzVelocity) - decel * Time.deltaTime);
    }

    public void BumpHorizontal(float velocity, float decel)
    {
        var cState = HeroController.instance.cState;
        horzVelocity = velocity * (cState.facingRight ? -1 : 1);
        this.decel = decel;

        BumperHooks.UpdateVelocity(gameObject.GetComponent<Rigidbody2D>());
    }

    internal void Reset() => horzVelocity = 0;

    internal Vector2 VelocityDelta() => new(horzVelocity, 0);
}

// Module for transparently modifying the Knight's velocity while HeroController code only sees the normal velocity.
internal static class BumperHooks
{
    private static BumperSpeedBehaviour? behaviour;

    public static void CancelBump() => behaviour?.Reset();

    private static List<ILHook>? hooks;

    public static void Load()
    {
        hooks = ILHookUtils.HookType<HeroController>(OverrideMethod);
        On.HutongGames.PlayMaker.Actions.GetVelocity2d.DoGetVelocity += OverrideGetVelocity2d;
        On.HutongGames.PlayMaker.Actions.SetVelocity2d.DoSetVelocity += OverrideSetVelocity2d;
        ModHooks.TakeDamageHook += OnTakeDamage;

        On.PlayMakerFSM.OnEnable += ModifyVelocity;
    }

    private static void ModifyVelocity(On.PlayMakerFSM.orig_OnEnable orig, PlayMakerFSM fsm)
    {
        orig(fsm);
        if (fsm.gameObject.name == "Knight" && fsm.FsmName == "ProxyFSM") behaviour = fsm.gameObject.GetOrAddComponent<BumperSpeedBehaviour>();
    }

    private static void OverrideMethod(ILContext il)
    {
        ILCursor cursor = new(il);

        cursor.Goto(0);
        do
        {
            if (cursor.Next.MatchCallvirt<Rigidbody2D>("get_velocity")) cursor.Remove().EmitDelegate(OverrideGetVelocity);
            else if (cursor.Next.MatchCallvirt<Rigidbody2D>("set_velocity")) cursor.Remove().EmitDelegate(OverrideSetVelocity);
        } while (cursor.TryGotoNext(i => true));
    }

    internal static void BumpUp(float scale)
    {
        var hc = HeroController.instance;
        hc.ShroomBounce();

        var rb2d = hc.gameObject.GetComponent<Rigidbody2D>();
        var v = rb2d.velocity;
        rb2d.SetVelocityY(v.y * scale);
    }

    internal static void BumpDown()
    {
        var hc = HeroController.instance;
        if (hc.cState.onGround) return;

        var rb2d = hc.gameObject.GetComponent<Rigidbody2D>();
        rb2d.SetVelocityY(-hc.MAX_FALL_VELOCITY);
    }

    internal static void BumpHorizontal(float velocity, float decel, float yBump, float yMax)
    {
        var hc = HeroController.instance;
        hc.SetDoubleJumped(false);
        hc.SetAirDashed(false);

        if (yBump > 0)
        {
            var rb2d = hc.gameObject.GetComponent<Rigidbody2D>();
            var origY = rb2d.velocity.y;

            hc.ShroomBounce();
            rb2d.SetVelocityY(Mathf.Max(origY, yBump, Mathf.Min(origY + yBump, yMax)));
        }
        behaviour?.BumpHorizontal(velocity, decel);
    }

    // Last recorded delta between dictated velocity and actual velocity.
    // Dictated + delta = actual
    private static Vector2 previousDelta = Vector2.zero;

    private static Vector2 CalculateDelta() => behaviour?.VelocityDelta() ?? Vector2.zero;

    internal static void UpdateVelocity(Rigidbody2D rb2d)
    {
        var newDelta = CalculateDelta();
        var deltaDelta = newDelta - previousDelta;
        previousDelta = newDelta;
        rb2d.velocity += deltaDelta;
    }

    private static Vector2 OverrideGetVelocity(Rigidbody2D rb2d) => rb2d.velocity - previousDelta;

    private static void OverrideSetVelocity(Rigidbody2D rb2d, Vector2 velocity)
    {
        previousDelta = CalculateDelta();
        rb2d.velocity = velocity + previousDelta;
    }

    private static void OverrideGetVelocity2d(On.HutongGames.PlayMaker.Actions.GetVelocity2d.orig_DoGetVelocity orig, GetVelocity2d self)
    {
        orig(self);
        if (self.space == Space.World && self.Owner.name == "Knight")
        {
            var newVel = self.vector.Value - previousDelta;

            self.vector.Value = newVel;
            self.x.Value = newVel.x;
            self.y.Value = newVel.y;
        }
    }

    private static readonly FieldInfo componentField = typeof(ComponentAction<Rigidbody2D>).GetField("component", BindingFlags.NonPublic | BindingFlags.Instance);

    private static void OverrideSetVelocity2d(On.HutongGames.PlayMaker.Actions.SetVelocity2d.orig_DoSetVelocity orig, SetVelocity2d self)
    {
        orig(self);
        if (self.Owner.name == "Knight")
        {
            var rb2d = (Rigidbody2D)componentField.GetValue(self);
            rb2d.velocity += previousDelta = CalculateDelta();
        }
    }

    private static int OnTakeDamage(ref int hazardType, int damage)
    {
        behaviour?.Reset();
        return damage;
    }
}
