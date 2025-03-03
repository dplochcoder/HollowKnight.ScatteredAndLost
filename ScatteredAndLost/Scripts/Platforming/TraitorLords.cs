using HK8YPlando.IC;
using HK8YPlando.Scripts.Framework;
using HK8YPlando.Scripts.InternalLib;
using HK8YPlando.Scripts.Proxy;
using HK8YPlando.Scripts.SharedLib;
using HK8YPlando.Util;
using HutongGames.PlayMaker.Actions;
using ItemChanger;
using ItemChanger.Extensions;
using ItemChanger.FsmStateActions;
using SFCore.Utils;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HK8YPlando.Scripts.Platforming;

[Shim]
internal class TraitorLords : MonoBehaviour
{
    [ShimField] public HeroDetectorProxy? Trigger;

    [ShimField] public float StartDelay;
    [ShimField] public GameObject? Spawn1;
    [ShimField] public bool Spawn1FacingRight;
    [ShimField] public float SpawnDelay;
    [ShimField] public GameObject? Spawn2;
    [ShimField] public bool Spawn2FacingRight;
    [ShimField] public float LeftX;
    [ShimField] public float RightX;
    [ShimField] public float MainY;
    [ShimField] public float AttackCooldown;
    [ShimField] public float SickleCooldown;
    [ShimField] public float SlamCooldown;
    [ShimField] public float PostDeathWait;

    private void Awake() => this.StartLibCoroutine(Run());

    private PlayMakerFSM? traitor1;
    private bool traitor1Dead;
    private PlayMakerFSM? traitor2;
    private bool traitor2Dead;

    private IEnumerator<CoroutineElement> Run()
    {
        yield return Coroutines.SleepFrames(2);
        Destroy(GameObject.Find("/_Transition Gates/top1")!);

        yield return Coroutines.SleepUntil(() => Trigger!.Detected());

        var mod = BrettasHouse.Get();
        mod.UpdateCheckpoint(Data.CheckpointLevel.Boss);

        yield return Coroutines.SleepSeconds(StartDelay);

        traitor1 = SpawnTraitorLord(Spawn1!.transform.position, Spawn1FacingRight, () =>
        {
            traitor1Dead = true;
            if (!traitor2Dead && traitor2 != null) RageMode(traitor2);
        });

        yield return Coroutines.SleepSeconds(SpawnDelay);
        traitor2 = SpawnTraitorLord(Spawn2!.transform.position, Spawn2FacingRight, () =>
        {
            traitor2Dead = true;
            if (!traitor1Dead) RageMode(traitor1);
        });

        yield return Coroutines.SleepUntil(() => traitor1Dead && traitor2Dead);
        TempleMusicManager.Get()?.FadeOut(5f);

        mod.DefeatedBrettorLords = true;
        mod.UpdateCheckpoint(Data.CheckpointLevel.Bretta);

        yield return Coroutines.SleepSeconds(PostDeathWait);

        var ggBattleTransitions = GameObjectExtensions.FindChild(ScatteredAndLostPreloader.Instance.GorbStatue, "Inspect")
            .LocateMyFSM("GG Boss UI").GetFsmState("Transition").GetFirstActionOfType<CreateObject>().gameObject.Value;

        var transitions = Instantiate(ggBattleTransitions);
        transitions.SetActive(true);
        transitions.LocateMyFSM("Transitions").SendEvent("GG TRANSITION OUT");

        yield return Coroutines.SleepSeconds(1.5f);

        // Finish the transition in the next scene.
        Events.OnSceneChange += FinishGodhomeTransition;
        GameManager.instance.BeginSceneTransition(new GameManager.SceneLoadInfo
        {
            SceneName = "Room_Bretta",
            EntryGateName = "right1",
            EntryDelay = 0f,
            Visualization = GameManager.SceneLoadVisualizations.GodsAndGlory,
            PreventCameraFadeOut = true,
            WaitForSceneTransitionCameraFade = false,
            AlwaysUnloadUnusedAssets = false
        });
    }

    private PlayMakerFSM? lastAttacker;
    private float attackCooldown;
    private PlayMakerFSM? lastSickler;
    private float sickleCooldown;
    private PlayMakerFSM? lastSlammer;
    private float slamCooldown;

    private void Update()
    {
        if (attackCooldown > 0)
        {
            attackCooldown -= Time.deltaTime;
            if (attackCooldown < 0) attackCooldown = 0;
        }
        if (sickleCooldown > 0)
        {
            sickleCooldown -= Time.deltaTime;
            if (sickleCooldown < 0) sickleCooldown = 0;
        }
        if (slamCooldown > 0)
        {
            slamCooldown -= Time.deltaTime;
            if (slamCooldown < 0) slamCooldown = 0;
        }
    }

    private PlayMakerFSM SpawnTraitorLord(Vector3 position, bool facingRight, Action onDeath)
    {
        var prefab = ScatteredAndLostPreloader.Instance.TraitorLord;
        var pos = position;
        pos.z = prefab.transform.position.z;
        var obj = Instantiate(prefab, pos, Quaternion.identity);

        if (!facingRight) obj.transform.localScale = new(-1, 1, 1);
        var mid = (LeftX + RightX) / 2;

        var fsm = obj.LocateMyFSM("Mantis");
        fsm.FsmVariables.GetFsmGameObject("Self").Value = obj;

        fsm.GetFsmState("Check L").GetFirstActionOfType<FloatCompare>().float2.Value = mid + 1.5f;
        fsm.GetFsmState("Check R").GetFirstActionOfType<FloatCompare>().float2.Value = mid - 1.5f;
        fsm.GetFsmState("DSlash").GetFirstActionOfType<FloatCompare>().float2.Value = MainY;
        fsm.GetFsmState("Fall").GetFirstActionOfType<FloatCompare>().float2.Value = MainY;
        fsm.GetFsmState("Intro Land").GetFirstActionOfType<SetPosition>().y.Value = MainY;
        fsm.GetFsmState("Land").GetFirstActionOfType<SetPosition>().y.Value = MainY;
        fsm.GetFsmState("Roar").GetFirstActionOfType<SetFsmString>().setValue = "BRETTOR_LORD2";

        fsm.GetFsmState("Sickle Antic").AddFirstAction(new Lambda(() =>
        {
            if (sickleCooldown > 0 && lastSickler != null && lastSickler != this)
            {
                fsm.ForceSetState("Cooldown");
                return;
            }

            lastSickler = fsm;
            sickleCooldown = SickleCooldown;
        }));

        fsm.GetFsmState("Too Close?").AddFirstAction(new Lambda(() =>
        {
            if (slamCooldown > 0 && lastSlammer != null && lastSlammer != this)
            {
                fsm.ForceSetState("Idle");
                return;
            }

            lastSlammer = fsm;
            slamCooldown = SlamCooldown;
        }));

        List<string> attackStates = ["Feint?", "Jump Antic"];
        foreach (var attackState in attackStates)
        {
            fsm.GetFsmState(attackState).AddFirstAction(new Lambda(() =>
            {
                if (attackCooldown > 0 && lastAttacker != null && lastAttacker != this)
                {
                    fsm.ForceSetState("Cooldown");
                    return;
                }

                lastAttacker = fsm;
                attackCooldown = AttackCooldown;
            }));
        }

        obj.SetActive(true);
        fsm.ForceSetState("Fall");

        // Shorten death anim.
        this.StartLibCoroutine(ModifyCorpseFsm(fsm));

        fsm.gameObject.GetComponent<HealthManager>().OnDeath += () => onDeath();
        return fsm;
    }

    private IEnumerator<CoroutineElement> ModifyCorpseFsm(PlayMakerFSM fsm)
    {
        while (true)
        {
            var blow = GameObjectExtensions.FindChild(fsm.gameObject, "Corpse Traitor Lord(Clone)")?.LocateMyFSM("FSM");
            if (blow == null)
            {
                yield return Coroutines.SleepFrames(1);
                continue;
            }

            blow.GetFsmState("Init").AddFirstAction(new Lambda(() =>
            {
                if (enragedFsm == fsm) return;

                blow.GetFsmState("Init").GetFirstActionOfType<Wait>().time = 0.15f;
                blow.GetFsmState("Steam").GetFirstActionOfType<Wait>().time = 0.45f;
                blow.GetFsmState("Ready").GetFirstActionOfType<Wait>().time = 0.2f;
            }));
            break;
        }
    }

    [ShimField] public int RageHPBoost;

    [ShimField] public float RageRoarTime;
    [ShimField] public float RageRoarSpeedup;

    [ShimField] public float RageAttackSpeed;
    [ShimField] public float RageSickleSpeed;
    [ShimField] public float RageWalkSpeed;
    [ShimField] public float RageWaveSpeed;

    [ShimField] public float RageAttack1Speedup;
    [ShimField] public float RageAttackAnticSpeedup;
    [ShimField] public float RageAttackRecoverSpeedup;
    [ShimField] public float RageAttackSwipeSpeedup;
    [ShimField] public float RageCooldownSpeedup;
    [ShimField] public float RageDSlashSpeedup;
    [ShimField] public float RageDSlashAnticSpeedup;
    [ShimField] public float RageFeintSpeedup;
    [ShimField] public float RageFeint2Speedup;
    [ShimField] public float RageJumpSpeedup;
    [ShimField] public float RageJumpAnticSpeedup;
    [ShimField] public float RageLandSpeedup;
    [ShimField] public float RageSickleAnticSpeedup;
    [ShimField] public float RageSickleThrowSpeedup;
    [ShimField] public float RageSickleThrowCooldownSpeedup;
    [ShimField] public float RageSickleThrowRecoverSpeedup;
    [ShimField] public float RageSlamAnticSpeedup;
    [ShimField] public float RageSlammingSpeedup;
    [ShimField] public float RageSlamEndSpeedup;
    [ShimField] public float RageTurnSpeedup;
    [ShimField] public float RageWalkSpeedup;
    [ShimField] public float RageWavesSpeedup;

    private PlayMakerFSM? enragedFsm;

    private void RageMode(PlayMakerFSM fsm)
    {
        enragedFsm = fsm;
        var accel = GameObjectExtensions.GetOrAddComponent<AnimationAccelerator>(fsm.gameObject);

        fsm.gameObject.GetComponent<HealthManager>().hp += RageHPBoost;

        var roar = fsm.GetFsmState("Roar");
        var audio = roar.GetFirstActionOfType<AudioPlayerOneShot>();
        audio.pitchMin.Value = 1.25f;
        audio.pitchMax.Value = 1.25f;
        audio.audioClips[0] = ScatteredAndLostPreloader.Instance.OblobbleRoar;

        roar.RemoveActionsOfType<SendEventByName>();
        roar.GetFirstActionOfType<Wait>().time.Value = RageRoarTime;
        roar.RemoveActionsOfType<ActivateGameObject>();

        fsm.GetFsmState("Roar End").AccelerateAnimation(accel, RageRoarSpeedup);
        fsm.GetFsmState("Roar Recover").AccelerateAnimation(accel, RageRoarSpeedup);

        Wrapped<bool> raged = new(false);
        fsm.GetFsmState("Idle").AddFirstAction(new Lambda(() =>
        {
            if (!raged.Value)
            {
                raged.Value = true;
                fsm.ForceSetState("Roar");

                this.StartLibCoroutine(DelayedRoarAnim(fsm));
                ActualRageMode(fsm);
            }
        }));
    }

    private IEnumerator<CoroutineElement> DelayedRoarAnim(PlayMakerFSM fsm)
    {
        yield return Coroutines.SleepFrames(1);
        fsm.GetComponent<tk2dSpriteAnimator>().Play("Roar");
    }

    private void ActualRageMode(PlayMakerFSM fsm)
    {
        var accel = GameObjectExtensions.GetOrAddComponent<AnimationAccelerator>(fsm.gameObject);

        fsm.FsmVariables.GetFsmFloat("Attack Speed").Value = RageAttackSpeed;
        fsm.FsmVariables.GetFsmFloat("DSlash Speed").Value = RageAttackSpeed;
        fsm.FsmVariables.GetFsmFloat("Sickle Speed Base").Value = RageSickleSpeed;

        fsm.GetFsmState("Attack 1").AccelerateAnimation(accel, RageAttack1Speedup);
        fsm.GetFsmState("Attack Antic").AccelerateAnimation(accel, RageAttackAnticSpeedup);
        fsm.GetFsmState("Attack Recover").AccelerateAnimation(accel, RageAttackRecoverSpeedup);
        fsm.GetFsmState("Attack Swipe").AccelerateAnimation(accel, RageAttackSwipeSpeedup);
        fsm.GetFsmState("DSlash").AccelerateAnimation(accel, RageDSlashSpeedup);
        fsm.GetFsmState("DSlash Antic").AccelerateAnimation(accel, RageDSlashAnticSpeedup);
        fsm.GetFsmState("Jump").AccelerateAnimation(accel, RageJumpSpeedup);
        fsm.GetFsmState("Jump Antic").AccelerateAnimation(accel, RageJumpAnticSpeedup);
        fsm.GetFsmState("Land").AccelerateAnimation(accel, RageLandSpeedup);
        fsm.GetFsmState("Sickle Antic").AccelerateAnimation(accel, RageSickleAnticSpeedup);
        fsm.GetFsmState("Sickle Throw Recover").AccelerateAnimation(accel, RageSickleThrowRecoverSpeedup);
        fsm.GetFsmState("Slam Antic").AccelerateAnimation(accel, RageSlamAnticSpeedup);
        fsm.GetFsmState("Turn").AccelerateAnimation(accel, RageTurnSpeedup);

        var cooldown = fsm.GetFsmState("Cooldown");
        cooldown.GetFirstActionOfType<Wait>().time = 0.25f / RageCooldownSpeedup;
        cooldown.AccelerateAnimation(accel, RageLandSpeedup);

        var feint = fsm.GetFsmState("Feint");
        feint.GetFirstActionOfType<Wait>().time = 0.2f / RageFeintSpeedup;
        feint.AccelerateAnimation(accel, RageFeintSpeedup);

        var feint2 = fsm.GetFsmState("Feint 2");
        feint2.GetFirstActionOfType<Wait>().time = 0.25f / RageFeint2Speedup;
        feint2.AccelerateAnimation(accel, RageFeint2Speedup);

        var sickleThrow = fsm.GetFsmState("Sickle Throw");
        sickleThrow.AccelerateAnimation(accel, RageSickleThrowSpeedup);
        sickleThrow.InsertBefore<PlayParticleEmitter>(new Lambda(() =>
        {
            var spawner = sickleThrow.GetFirstActionOfType<SpawnObjectFromGlobalPool>();
            var spawned = spawner.gameObject.Value.Spawn(spawner.spawnPoint.Value.transform.position, Quaternion.identity);
            spawned.GetComponent<Rigidbody2D>().velocity = new(fsm.FsmVariables.GetFsmFloat("Sickle Speed Base").Value * 3.5f * fsm.gameObject.transform.localScale.x, 0);
        }));

        var sickleThrowCooldown = fsm.GetFsmState("Sick Throw CD");
        sickleThrowCooldown.GetFirstActionOfType<Wait>().time = 0.9f / RageSickleThrowCooldownSpeedup;
        sickleThrowCooldown.AccelerateAnimation(accel, RageSickleThrowRecoverSpeedup);

        var slamming = fsm.GetFsmState("Slamming");
        slamming.GetFirstActionOfType<Wait>().time = 0.3f / RageSlammingSpeedup;
        slamming.AccelerateAnimation(accel, RageSlammingSpeedup);

        var slamEnd = fsm.GetFsmState("Slam End");
        slamEnd.GetFirstActionOfType<Wait>().time = 1.2f / RageSlamEndSpeedup;
        slamEnd.AccelerateAnimation(accel, RageSlammingSpeedup);

        var walk = fsm.GetFsmState("Walk");
        walk.GetFirstActionOfType<ChaseObjectGround>().speedMax.Value = RageWalkSpeed;
        walk.AccelerateAnimation(accel, RageWalkSpeedup);

        var waves = fsm.GetFsmState("Waves");
        waves.GetFirstActionOfType<Wait>().time = 1f / RageWavesSpeedup;
        foreach (var action in waves.GetActionsOfType<SetVelocity2d>()) action.x.Value = Mathf.Sign(action.x.Value) * RageWaveSpeed;
    }

    private void FinishGodhomeTransition(Scene scene)
    {
        var ggBattleTransitions = GameObjectExtensions.FindChild(ScatteredAndLostPreloader.Instance.GorbStatue, "Inspect")
            .LocateMyFSM("GG Boss UI").GetFsmState("Transition").GetFirstActionOfType<CreateObject>().gameObject.Value;

        var transitions = Instantiate(ggBattleTransitions);
        transitions.AddComponent<OnDestroyHook>().Action = () => Events.OnSceneChange -= FinishGodhomeTransition;
        transitions.SetActive(true);
        transitions.LocateMyFSM("Transitions").SendEvent("GG TRANSITION IN");
    }
}