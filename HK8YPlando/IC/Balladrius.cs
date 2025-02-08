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
using SFCore.Utils;
using System.Collections.Generic;
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
        bulletHook = new(typeof(EnemyBullet).GetMethod("Collision", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetStateMachineTarget(), OverrideEnemyBulletCollision);
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
        fsm.GetFsmState("Fire").AddFirstAction(new Lambda(() =>
        {
            if (++numFires[0] == 3) ReallyBuffBaldur(fsm);
        }));

        obj.AddComponent<InfiniteHealth>();
    }

    private void ReallyBuffBaldur(PlayMakerFSM fsm)
    {
        var healthManager = fsm.gameObject.GetComponent<HealthManager>();
        baldurs.Add(healthManager);
        fsm.gameObject.AddComponent<OnDestroyHook>().Action = () => baldurs.Remove(healthManager);

        fsm.FsmVariables.GetFsmFloat("X Speed Min").Value = 1.25f;

        var accel = fsm.gameObject.AddComponent<AnimationAccelerator>();

        fsm.RemoveGlobalTransition("TOOK DAMAGE");

        fsm.GetFsmState("Close").AccelerateAnimation(accel, 3f);
        fsm.GetFsmState("Close2").AccelerateAnimation(accel, 3f);

        var fire = fsm.GetFsmState("Fire");
        fire.AccelerateAnimation(accel, 3.5f);

        Wrapped<int> bullets = new(0);
        fire.AddLastAction(new Lambda(() =>
        {
            var shot = fsm.FsmVariables.GetFsmGameObject("Shot Instance").Value;
            var bullet = shot.GetComponent<EnemyBullet>();
            if (bullet != null && ++bullets.Value == 3)
            {
                bullets.Value = 0;

                explodeOnImpact.Add(bullet);
                bullet.gameObject.GetOrAddComponent<OnDestroyHook>().Action ??= () => explodeOnImpact.Remove(bullet);
            }
        }));

        fsm.GetFsmState("Hit").AccelerateAnimation(accel, 3f);

        var idle = fsm.GetFsmState("Idle");
        idle.RemoveTransitionsOn("CLOSE");
        idle.AccelerateAnimation(accel, 3f);
        idle.GetFirstActionOfType<WaitRandom>().SetMinMax(0.075f, 0.1f);

        fsm.GetFsmState("Open").AccelerateAnimation(accel, 3.5f);

        fsm.GetFsmState("Shot Anim End").RemoveTransitionsOn("CLOSE");

        fsm.GetFsmState("Shot Antic").AccelerateAnimation(accel, 3.5f);

        fsm.GetFsmState("Sleep 1").AccelerateAnimation(accel, 3.5f);
        fsm.GetFsmState("Sleep 2").AccelerateAnimation(accel, 3.5f);
    }

    private bool OverrideIsBlockingByDirection(On.HealthManager.orig_IsBlockingByDirection orig, HealthManager self, int cardinalDirection, AttackTypes attackTypes)
    {
        if (baldurs.Contains(self)) return true;
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
        if (!explodeOnImpact.Contains(bullet)) return;

        explodeOnImpact.Remove(bullet);
        Object.Instantiate(HK8YPlandoPreloader.Instance.BelflyExplosion, bullet.gameObject.transform.position, Quaternion.identity).SetActive(true);
    }
}
