using HK8YPlando.Scripts.SharedLib;
using HK8YPlando.Util;
using HutongGames.PlayMaker.Actions;
using ItemChanger;
using Modding;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace HK8YPlando.IC;

internal class BumperSpeedBehaviour : MonoBehaviour
{
    private float horzVelocity = 0;
    private float decel = 0;

    private BumperModule? module;

    private void Update()
    {
        if (horzVelocity == 0) return;

        module ??= BumperModule.Get();

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
        // TODO: grounded

        var cState = HeroController.instance.cState;
        horzVelocity = velocity * (cState.facingRight ? -1 : 1);
        this.decel = decel;
    }

    internal void Reset() => horzVelocity = 0;

    internal Vector2 VelocityDelta() => new(horzVelocity, 0);
}

// Module for transparently modifying the Knight's velocity while HeroController code only sees the normal velocity.
internal class BumperModule : ItemChanger.Modules.Module
{
    private static BumperSpeedBehaviour? behaviour;

    public static BumperModule Get() => ItemChangerMod.Modules.Get<BumperModule>()!;

    private static readonly FsmID KnightFsmID = new("Knight", "ProxyFSM");

    private List<ILHook>? hooks;

    public override void Initialize()
    {
        hooks = ILHookUtils.HookType<HeroController>(OverrideMethod);
        On.HutongGames.PlayMaker.Actions.GetVelocity2d.DoGetVelocity += OverrideGetVelocity2d;
        On.HutongGames.PlayMaker.Actions.SetVelocity2d.DoSetVelocity += OverrideSetVelocity2d;
        ModHooks.TakeDamageHook += OnTakeDamage;

        Events.AddFsmEdit(KnightFsmID, ModifyVelocity);
    }

    public override void Unload()
    {
        hooks?.ForEach(h => h.Dispose());
        On.HutongGames.PlayMaker.Actions.GetVelocity2d.DoGetVelocity -= OverrideGetVelocity2d;
        On.HutongGames.PlayMaker.Actions.SetVelocity2d.DoSetVelocity -= OverrideSetVelocity2d;
        ModHooks.TakeDamageHook -= OnTakeDamage;

        Events.RemoveFsmEdit(KnightFsmID, ModifyVelocity);
        Object.Destroy(HeroController.instance?.gameObject?.GetComponent<BumperSpeedBehaviour>());
        behaviour = null;
    }

    private void ModifyVelocity(PlayMakerFSM fsm) => behaviour = fsm.gameObject.GetOrAddComponent<BumperSpeedBehaviour>();

    private void OverrideMethod(ILContext il)
    {
        ILCursor cursor = new(il);

        cursor.Goto(0);
        do
        {
            if (cursor.Next.MatchCallvirt<Rigidbody2D>("get_velocity")) cursor.Remove().EmitDelegate(OverrideGetVelocity);
            else if (cursor.Next.MatchCallvirt<Rigidbody2D>("set_velocity")) cursor.Remove().EmitDelegate(OverrideSetVelocity);
        } while (cursor.TryGotoNext(i => true));
    }

    internal void BumpUp(float scale)
    {
        var hc = HeroController.instance;
        hc.ShroomBounce();

        var rb2d = hc.gameObject.GetComponent<Rigidbody2D>();
        var v = rb2d.velocity;
        rb2d.SetVelocityY(v.y * scale);
    }

    internal void BumpDown()
    {
        var hc = HeroController.instance;
        if (hc.cState.onGround) return;

        var rb2d = hc.gameObject.GetComponent<Rigidbody2D>();
        rb2d.SetVelocityY(-hc.MAX_FALL_VELOCITY);
    }

    internal void BumpHorizontal(float velocity, float decel, float yBump, float yMax)
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

    private Vector2 previousDelta = Vector2.zero;

    private Vector2 CalculateDelta() => behaviour?.VelocityDelta() ?? Vector2.zero;

    internal void UpdateVelocity(Rigidbody2D rb2d)
    {
        var newDelta = CalculateDelta();
        var deltaDelta = newDelta - previousDelta;
        previousDelta = newDelta;
        rb2d.velocity += deltaDelta;
    }

    private Vector2 OverrideGetVelocity(Rigidbody2D rb2d) => rb2d.velocity - previousDelta;

    private void OverrideSetVelocity(Rigidbody2D rb2d, Vector2 velocity)
    {
        previousDelta = CalculateDelta();
        rb2d.velocity = velocity + previousDelta;
    }

    private void OverrideGetVelocity2d(On.HutongGames.PlayMaker.Actions.GetVelocity2d.orig_DoGetVelocity orig, GetVelocity2d self)
    {
        orig(self);
        if (self.space == Space.World && self.Owner.name == "Knight")
        {
            var prevVel = self.vector.Value;
            var newVel = self.vector.Value - previousDelta;

            self.vector.Value = newVel;
            self.x.Value = newVel.x;
            self.y.Value = newVel.y;
        }
    }

    private static readonly FieldInfo componentField = typeof(ComponentAction<Rigidbody2D>).GetField("component", BindingFlags.NonPublic | BindingFlags.Instance);

    private void OverrideSetVelocity2d(On.HutongGames.PlayMaker.Actions.SetVelocity2d.orig_DoSetVelocity orig, SetVelocity2d self)
    {
        orig(self);
        if (self.Owner.name == "Knight")
        {
            var rb2d = (Rigidbody2D)componentField.GetValue(self);
            rb2d.velocity += (previousDelta = CalculateDelta());
        }
    }

    private int OnTakeDamage(ref int hazardType, int damage)
    {
        behaviour?.Reset();
        return damage;
    }
}
