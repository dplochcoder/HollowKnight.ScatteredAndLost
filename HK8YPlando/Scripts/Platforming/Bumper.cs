using HK8YPlando.IC;
using HK8YPlando.Scripts.SharedLib;
using HK8YPlando.Util;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace HK8YPlando.Scripts.Platforming;

[Shim]
internal class Bumper : MonoBehaviour, IHitResponder
{
    [ShimField] public float CooldownDuration;
    [ShimField] public float HorizontalVelocity;
    [ShimField] public float HorizontalDecel;
    [ShimField] public float VerticalScale;

    private BumperModule? module;
    private float cooldown;

    private void Awake() => module = BumperModule.Get();

    public void Hit(HitInstance damageInstance)
    {
        if (cooldown > 0) return;
        if (damageInstance.AttackType != AttackTypes.Nail) return;
        if (KnightUtil.IsNailArtActive()) return;

        cooldown = CooldownDuration;
        var cState = HeroController.instance.cState;
        if (cState.downAttacking) module?.BumpUp(VerticalScale);
        else if (cState.upAttacking) module?.BumpDown();
        else module?.BumpHorizontal(HorizontalVelocity, HorizontalDecel);
    }

    private void Update()
    {
        if (cooldown <= Time.deltaTime)
        {
            cooldown = 0;
            return;
        }

        cooldown -= Time.deltaTime;
    }
}
