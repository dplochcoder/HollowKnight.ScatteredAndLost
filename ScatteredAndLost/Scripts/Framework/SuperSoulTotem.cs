using DecorationMaster;
using DecorationMaster.Attr;
using DecorationMaster.MyBehaviour;
using HK8YPlando.IC;
using HK8YPlando.Scripts.SharedLib;
using HK8YPlando.Util;
using HutongGames.PlayMaker.Actions;
using ItemChanger.Extensions;
using ItemChanger.FsmStateActions;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;
using SFCore.Utils;
using System.Collections.Generic;
using UnityEngine;

namespace HK8YPlando.Scripts.Framework;

// Configure behavior changes through a MonoBehaviour to support prefab cloning.
internal class SuperSoulTotemStarter : MonoBehaviour
{
    private void Awake()
    {
        SuperSoulTotem.EnhanceVanillaTotem(gameObject);
        SuperSoulTotem.EnhanceTotem(gameObject);
    }
}

[Shim]
internal class SuperSoulTotem : MonoBehaviour
{
    private const float EMISSION_RATE = 30;
    private const float PARTICLE_LIFETIME = 0.65f;
    private const float PARTICLE_SIZE = 0.75f;
    private const int PARTICLE_CAP = 100;

    private void Awake()
    {
        var mod = BrettasHouse.Get();
        if (mod != null && mod.RandomizeSoulTotems)
        {
            // Let ItemChanger place an item here instead.
            Destroy(this);
            return;
        }

        var totem = SpawnTotemInactive();

        totem.transform.position = transform.position;
        totem.transform.localScale = transform.localScale;
        totem.SetActive(true);

        Destroy(gameObject);
    }

    internal static GameObject SpawnTotemInactive()
    {
        var totem = Instantiate(ScatteredAndLostPreloader.Instance.SoulTotem);
        totem.AddComponent<SuperSoulTotemStarter>();

        return totem;
    }

    internal static void EnhanceVanillaTotem(GameObject totem)
    {
        var data = totem.GetComponent<PersistentIntItem>().persistentIntData;
        data.value = 3;
        data.semiPersistent = false;
        data.id = "SuperSoulTotem";
        data.sceneName = "BrettasHouse";

        var fsm = totem.LocateMyFSM("soul_totem");
        fsm.GetFsmState("Close").AddFirstAction(new Lambda(() => fsm.FsmVariables.GetFsmInt("Value").Value = 3));
        var hit = fsm.GetFsmState("Hit");
        hit.AddFirstAction(new Lambda(() => fsm.FsmVariables.GetFsmInt("Value").Value = 3));
    }

    internal static void EnhanceTotem(GameObject totem)
    {
        var hit = totem.LocateMyFSM("soul_totem").GetFsmState("Hit");

        var flinger = hit.GetFirstActionOfType<FlingObjectsFromGlobalPool>();
        flinger.spawnMin.Value = 11;
        flinger.spawnMax.Value = 11;

        if (SuperSoulTotemHooks.RegisterFlinger(flinger)) totem.AddComponent<OnDestroyHook>().Action = () => SuperSoulTotemHooks.UnregisterFlinger(flinger);

        var particles = GameObjectExtensions.FindChild(totem, "Soul Particles").GetComponent<ParticleSystem>();
        particles.emissionRate = EMISSION_RATE;
        particles.startSize = PARTICLE_SIZE;
        particles.maxParticles = PARTICLE_CAP;
        particles.startLifetime = PARTICLE_LIFETIME;
    }
}

internal static class SuperSoulTotemHooks
{
    private static List<ILHook> ilHooks = [];

    public static void Hook()
    {
        ilHooks.Add(new(typeof(SoulOrb).GetMethod("Zoom", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetStateMachineTarget(), HookSoulOrbZoom));
        ilHooks.Add(new(typeof(FlingObjectsFromGlobalPool).GetMethod("OnEnter", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance), HookFlingObjectsFromGlobalPool));
    }

    private static void HookFlingObjectsFromGlobalPool(ILContext il)
    {
        ILCursor cursor = new(il);
        cursor.Goto(0);
        cursor.GotoNext(i => i.MatchCall<RigidBody2dActionBase>("CacheRigidBody2d"));
        cursor.Emit(OpCodes.Ldarg_0);
        cursor.Emit(OpCodes.Ldloc_S, (byte)4);
        cursor.EmitDelegate(MaybeBuffSoulOrb);
    }

    private static HashSet<FlingObjectsFromGlobalPool> superSoulOrbFlingers = [];
    internal static bool RegisterFlinger(FlingObjectsFromGlobalPool flinger) => superSoulOrbFlingers.Add(flinger);
    internal static void UnregisterFlinger(FlingObjectsFromGlobalPool flinger) => superSoulOrbFlingers.Remove(flinger);

    private static void MaybeBuffSoulOrb(FlingObjectsFromGlobalPool self, GameObject go)
    {
        if (superSoulOrbFlingers.Contains(self)) BuffSoulOrb(go);
    }

    internal static void BuffSoulOrb(GameObject go)
    {
        var orb = go.GetComponent<SoulOrb>();
        if (orb == null) return;

        buffedSoulOrbs.Add(orb);
        GameObjectExtensions.GetOrAddComponent<OnDestroyHook>(go).Action += () => buffedSoulOrbs.Remove(orb);
    }

    private static void HookSoulOrbZoom(ILContext il)
    {
        ILCursor cursor = new(il);
        cursor.Goto(0);
        cursor.GotoNext(i => i.MatchCallvirt<HeroController>("AddMPCharge"));
        cursor.Emit(OpCodes.Ldloc_1);
        cursor.EmitDelegate(MaybeSuperHeal);
    }

    private static HashSet<SoulOrb> buffedSoulOrbs = [];

    private static void MaybeSuperHeal(SoulOrb orb)
    {
        if (buffedSoulOrbs.Remove(orb))
        {
            HeroController.instance.AddMPCharge(16);  // (16 + 2) * 11 = 198 = max MP
            HeroController.instance.AddHealth(1);  // 1 * 11 = max health
        }
    }
}

[DecorationMaster.Attr.Description("Super soul totem which heals you to full", "en-us")]
[Decoration("scattered_and_lost_soul_totem")]
internal class SuperSoulTotemDecoration : CustomDecoration
{
    private static GameObject MakePrefab()
    {
        var obj = SuperSoulTotem.SpawnTotemInactive();
        Object.DontDestroyOnLoad(obj);
        return obj;
    }

    public static void Register() => DecorationMasterUtil.RegisterDecoration<SuperSoulTotemDecoration, ItemDef.DefatulResizeItem>(
        "scattered_and_lost_soul_totem", MakePrefab(), "super_soul_totem");

    private void Awake() => UnVisableBehaviour.AttackReact.Create(gameObject);
}