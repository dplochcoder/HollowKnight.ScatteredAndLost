using Architect.Content.Elements;
using Architect.Content.Groups;
using DecorationMaster;
using DecorationMaster.Attr;
using DecorationMaster.MyBehaviour;
using HK8YPlando.Scripts.Framework;
using HK8YPlando.Scripts.SharedLib;
using HK8YPlando.Util;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace HK8YPlando.Scripts.Platforming;

[Shim]
internal class Bumper : MonoBehaviour, IHitResponder
{
    [ShimField] public Animator? SpriteAnimator;

    [ShimField] public float CooldownDuration;
    [ShimField] public float HorizontalBumpX;
    [ShimField] public float HorizontalBumpY;
    [ShimField] public float HorizontalBumpYMax;
    [ShimField] public float YForgiveness;
    [ShimField] public float HorizontalDecel;
    [ShimField] public float VerticalScale;

    [ShimField] public List<AudioClip> HitClips = [];

    [ShimField] public float OscillateRadius;
    [ShimField] public float OscillatePeriod;

    private Vector3 origPos;

    internal void SetOrigPos(Vector2 pos) => origPos = pos;

    private float oscillateTimer;
    private float cooldown;

    private void Awake() => origPos = transform.position;

    public void Hit(HitInstance damageInstance)
    {
        if (cooldown > 0) return;
        if (damageInstance.AttackType != AttackTypes.Nail) return;

        gameObject.PlaySound(HitClips.Random(), 0.9f);
        cooldown = CooldownDuration;
        SpriteAnimator?.SetTrigger("Burst");

        if (damageInstance.AttackType != AttackTypes.Nail) return;
        if (KnightUtil.IsNailArtActive()) return;

        var cState = HeroController.instance.cState;
        if (cState.downAttacking) BumperHooks.BumpUp(VerticalScale);
        else if (cState.upAttacking) BumperHooks.BumpDown();
        else
        {
            var kPos = HeroController.instance.gameObject.transform.position;
            BumperHooks.BumpHorizontal(HorizontalBumpX, HorizontalDecel, (transform.position.y - kPos.y > YForgiveness) ? 0 : HorizontalBumpY, HorizontalBumpYMax);
        }
    }

    private void Update()
    {
        if (cooldown > 0)
        {
            cooldown -= Time.deltaTime;
            if (cooldown <= 0) cooldown = 0;

            return;
        }

        oscillateTimer += Time.deltaTime;
        if (oscillateTimer > OscillatePeriod) oscillateTimer -= OscillatePeriod;

        Vector3 delta = new(OscillateRadius * Mathf.Sin(2 * oscillateTimer * Mathf.PI / OscillatePeriod), 0, 0);
        transform.position = origPos + delta;
    }
}

[Serializable]
public class BumperDecorationItem : Item { }

[Description("Celeste Bumper\nEnjoy Bouncy Knight", "en-us")]
[Decoration("scattered_and_lost_bumper")]
public class BumperDecoration : CustomDecoration
{
    public static void Register() => DecorationMasterUtil.RegisterDecoration<BumperDecoration, BumperDecorationItem>(
        "scattered_and_lost_bumper",
        ScatteredAndLostSceneManagerAPI.LoadPrefab<GameObject>("Bumper"),
        "bumper");

    private void Awake() => UnVisableBehaviour.AttackReact.Create(gameObject);

    public override void HandlePos(Vector2 val) => gameObject.GetComponent<Bumper>().SetOrigPos(val);
}

public static class BumperArchitectObject
{
    public static AbstractPackElement Create() => ArchitectUtil.MakeArchitectObject("Bumper", "Bumper", "bumper", ConfigGroup.Generic);
}
