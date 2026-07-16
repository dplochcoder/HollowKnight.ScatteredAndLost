using System.Collections.Generic;
using HK8YPlando.Scripts;
using HK8YPlando.Scripts.InternalLib;
using HK8YPlando.Util;
using HutongGames.PlayMaker.Actions;
using ItemChanger;
using ItemChanger.Extensions;
using ItemChanger.FsmStateActions;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;
using UnityEngine;

namespace HK8YPlando.IC;

internal class OnDestroyHook : MonoBehaviour
{
    internal System.Action? Action;

    private void OnDestroy() => Action?.Invoke();
}

internal class Balladrius : ItemChanger.Modules.Module
{
    private static readonly FsmID blockerId = new("Blocker Control");

    private ILHook? bulletHook;

    private readonly HashSet<HealthManager> baldurs = [];
    private readonly HashSet<EnemyBullet> explodeOnImpact = [];

    public override void Initialize()
    {
        Events.AddFsmEdit(blockerId, BuffBaldur);
        On.HealthManager.IsBlockingByDirection += OverrideIsBlockingByDirection;
        bulletHook = new(
            typeof(EnemyBullet)
                .GetMethod(
                    "Collision",
                    System.Reflection.BindingFlags.NonPublic
                        | System.Reflection.BindingFlags.Instance
                )
                .GetStateMachineTarget(),
            OverrideEnemyBulletCollision
        );
    }

    public override void Unload()
    {
        Events.RemoveFsmEdit(blockerId, BuffBaldur);
        On.HealthManager.IsBlockingByDirection -= OverrideIsBlockingByDirection;
        bulletHook?.Dispose();
    }

    private void BuffBaldur(PlayMakerFSM fsm)
    {
        var obj = fsm.gameObject;

        List<int> numFires = [0];
        fsm.GetState("Fire")
            .AddFirstAction(
                new Lambda(() =>
                {
                    if (++numFires[0] == 2)
                        ReallyBuffBaldur(fsm);
                })
            );

        obj.AddComponent<InfiniteHealth>();

        var healthManager = obj.GetComponent<HealthManager>();
        baldurs.Add(healthManager);
        obj.AddComponent<OnDestroyHook>().Action = () => baldurs.Remove(healthManager);
    }

    private void ReallyBuffBaldur(PlayMakerFSM fsm)
    {
        fsm.FsmVariables.GetFsmFloat("X Speed Min").Value = 1.25f;

        var accel = fsm.gameObject.AddComponent<AnimationAccelerator>();

        SFCore.Utils.FsmUtil.RemoveGlobalTransition(fsm, "TOOK DAMAGE");

        fsm.GetState("Close").AccelerateAnimation(accel, 3f);
        fsm.GetState("Close2").AccelerateAnimation(accel, 3f);

        var fire = fsm.GetState("Fire");
        fire.AccelerateAnimation(accel, 3.5f);

        Wrapped<int> bullets = new(0);
        fire.AddLastAction(
            new Lambda(() =>
            {
                var shot = fsm.FsmVariables.GetFsmGameObject("Shot Instance").Value;
                var bullet = shot.GetComponent<EnemyBullet>();
                if (bullet != null && ++bullets.Value == 3)
                {
                    bullets.Value = 0;

                    explodeOnImpact.Add(bullet);
                    bullet.gameObject.GetOrAddComponent<OnDestroyHook>().Action ??= () =>
                        explodeOnImpact.Remove(bullet);
                }
            })
        );

        fsm.GetState("Hit").AccelerateAnimation(accel, 3f);

        var idle = fsm.GetState("Idle");
        idle.RemoveTransitionsOn("CLOSE");
        idle.AccelerateAnimation(accel, 3f);
        idle.GetFirstActionOfType<WaitRandom>().SetMinMax(0.075f, 0.1f);

        fsm.GetState("Open").AccelerateAnimation(accel, 3.5f);

        fsm.GetState("Shot Anim End").RemoveTransitionsOn("CLOSE");

        fsm.GetState("Shot Antic").AccelerateAnimation(accel, 3.5f);

        fsm.GetState("Sleep 1").AccelerateAnimation(accel, 3.5f);
        fsm.GetState("Sleep 2").AccelerateAnimation(accel, 3.5f);
    }

    private bool OverrideIsBlockingByDirection(
        On.HealthManager.orig_IsBlockingByDirection orig,
        HealthManager self,
        int cardinalDirection,
        AttackTypes attackTypes
    )
    {
        if (baldurs.Contains(self))
            return true;
        return orig(self, cardinalDirection, attackTypes);
    }

    private void OverrideEnemyBulletCollision(ILContext il)
    {
        ILCursor cursor = new(il);

        cursor.Goto(0).GotoNext(i => i.MatchCallOrCallvirt<AudioEvent>("SpawnAndPlayOneShot"));
        cursor.GotoNext();
        cursor.Emit(OpCodes.Ldloc_1);
        cursor.EmitDelegate(MaybeExplode);
    }

    private void MaybeExplode(EnemyBullet bullet)
    {
        if (!explodeOnImpact.Contains(bullet))
            return;

        explodeOnImpact.Remove(bullet);
        Object
            .Instantiate(
                ScatteredAndLostPreloader.Instance.BelflyExplosion,
                bullet.gameObject.transform.position,
                Quaternion.identity
            )
            .SetActive(true);
    }
}
