using Architect.Content.Elements;
using Architect.Content.Groups;
using DecorationMaster;
using DecorationMaster.Attr;
using DecorationMaster.MyBehaviour;
using HK8YPlando.IC;
using HK8YPlando.Scripts.SharedLib;
using HK8YPlando.Util;
using HutongGames.PlayMaker.Actions;
using ItemChanger.Extensions;
using ItemChanger.FsmStateActions;
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
        fsm.GetState("Close").AddFirstAction(new Lambda(() => fsm.FsmVariables.GetFsmInt("Value").Value = 3));
        var hit = fsm.GetState("Hit");
        hit.AddFirstAction(new Lambda(() => fsm.FsmVariables.GetFsmInt("Value").Value = 3));
    }

    internal static void EnhanceTotem(GameObject totem)
    {
        var hit = totem.LocateMyFSM("soul_totem").GetState("Hit");

        var flinger = hit.GetFirstActionOfType<FlingObjectsFromGlobalPool>();
        flinger.spawnMin.Value = 11;
        flinger.spawnMax.Value = 11;

        flingers.Add(flinger);
        totem.DoOnDestroy(() => flingers.Remove(flinger));

        var particles = GameObjectExtensions.FindChild(totem, "Soul Particles").GetComponent<ParticleSystem>();
        var emission = particles.emission;
        emission.rateOverTime = EMISSION_RATE;
        var main = particles.main;
        main.startSize = PARTICLE_SIZE;
        main.maxParticles = PARTICLE_CAP;
        main.startLifetime = PARTICLE_LIFETIME;
    }

    private static readonly HashSet<FlingObjectsFromGlobalPool> flingers = [];
    private static readonly HashSet<SoulOrb> orbs = [];

    internal static void BuffSoulOrb(SoulOrb orb)
    {
        if (orbs.Add(orb))
            orb.gameObject.DoOnDestroy(() => orbs.Remove(orb));
    }

    private static bool loaded = false;
    internal static void Load()
    {
        if (loaded) return;
        loaded = true;

        PurenailCore.ModUtil.SoulOrbModifier.OnFlingSoulOrb += (flinger, orb) =>
        {
            if (flingers.Contains(flinger)) BuffSoulOrb(orb);
        };
        PurenailCore.ModUtil.SoulOrbModifier.OnGiveSoul += orb =>
        {
            if (orbs.Remove(orb))
            {
                HeroController.instance.AddMPCharge(16);  // (16 + 2) * 11 = 198 = max MP
                HeroController.instance.AddHealth(1);  // 1 * 11 = max health
            }
        };
    }

    static SuperSoulTotem() => Load();
}

[Description("Super soul totem which heals you to full", "en-us")]
[Decoration("scattered_and_lost_soul_totem")]
internal class SuperSoulTotemDecoration : CustomDecoration
{
    private static GameObject MakePrefab()
    {
        var obj = SuperSoulTotem.SpawnTotemInactive();
        DontDestroyOnLoad(obj);
        return obj;
    }

    public static void Register() => DecorationMasterUtil.RegisterDecoration<SuperSoulTotemDecoration, ItemDef.DefatulResizeItem>(
        "scattered_and_lost_soul_totem", MakePrefab(), "super_soul_totem");

    private void Awake() => UnVisableBehaviour.AttackReact.Create(gameObject);
}

public static class SuperSoulTotemArchitectObject
{
    private static GameObject MakePrefab()
    {
        var obj = SuperSoulTotem.SpawnTotemInactive();
        Object.DontDestroyOnLoad(obj);
        return obj;
    }

    public static AbstractPackElement Create() => ArchitectUtil.MakeArchitectObject(MakePrefab(), "SuperSoulTotem", null, ConfigGroup.Generic);
}