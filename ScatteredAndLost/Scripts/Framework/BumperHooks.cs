using HK8YPlando.Scripts.SharedLib;
using HK8YPlando.Util;
using Modding;
using PurenailCore.ModUtil;
using UnityEngine;

namespace HK8YPlando.Scripts.Framework;

internal class BumperSpeedControl
{
    internal static readonly BumperSpeedControl Instance = new();

    private float horzVelocity = 0;
    private float decel = 0;

    private void Update(On.HeroController.orig_Update orig, HeroController self)
    {
        orig(self);

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

        var cState = HeroController.instance.cState;
        horzVelocity = velocity * (cState.facingRight ? -1 : 1);
        this.decel = decel;
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

    internal void CancelHorizontal() => horzVelocity = 0;

    internal static Vector2 ApplyBumperVelocity(Vector2 velocity)
    {
        velocity.x += Instance.horzVelocity;
        return velocity;
    }

    private static int OnTakeDamage(ref int hazardType, int damage)
    {
        Instance.CancelHorizontal();
        return damage;
    }

    static BumperSpeedControl()
    {
        HeroVelocityModifier.AddModifier(0, ApplyBumperVelocity);
        ModHooks.TakeDamageHook += OnTakeDamage;
    }
}
