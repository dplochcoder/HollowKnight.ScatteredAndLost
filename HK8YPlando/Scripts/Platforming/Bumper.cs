using HK8YPlando.IC;
using HK8YPlando.Scripts.SharedLib;
using HK8YPlando.Util;
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

    private BumperModule? module;
    private Vector3 origPos;

    private float oscillateTimer;
    private float cooldown;

    private void Awake()
    {
        module = BumperModule.Get();
        origPos = transform.position;
    }

    public void Hit(HitInstance damageInstance)
    {
        if (cooldown > 0) return;
        if (damageInstance.AttackType != AttackTypes.Nail) return;

        gameObject.PlaySound(HitClips.Random());
        cooldown = CooldownDuration;
        SpriteAnimator?.SetTrigger("Burst");

        if (damageInstance.AttackType != AttackTypes.Nail) return;
        if (KnightUtil.IsNailArtActive()) return;

        var cState = HeroController.instance.cState;
        if (cState.downAttacking) module?.BumpUp(VerticalScale);
        else if (cState.upAttacking) module?.BumpDown();
        else
        {
            var kPos = HeroController.instance.gameObject.transform.position;
            module?.BumpHorizontal(HorizontalBumpX, HorizontalDecel, (transform.position.y - kPos.y > YForgiveness) ? 0 : HorizontalBumpY, HorizontalBumpYMax);
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
