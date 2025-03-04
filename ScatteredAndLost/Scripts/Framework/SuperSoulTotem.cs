using HK8YPlando.IC;
using HK8YPlando.Scripts.SharedLib;
using HutongGames.PlayMaker.Actions;
using ItemChanger.Extensions;
using ItemChanger.FsmStateActions;
using SFCore.Utils;
using UnityEngine;

namespace HK8YPlando.Scripts.Framework;

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
        if (mod.RandomizeSoulTotems)
        {
            // Let ItemChanger place an item here instead.
            Destroy(this);
            return;
        }

        var totem = Instantiate(ScatteredAndLostPreloader.Instance.SoulTotem);

        totem.transform.position = transform.position;
        totem.transform.localScale = transform.localScale;
        totem.SetActive(true);

        EnhanceVanillaTotem(totem);
        EnhanceTotem(totem);

        Destroy(gameObject);
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

        var mod = BrettasHouse.Get();
        mod.RegisterSuperSoulOrbFlinger(flinger);
        totem.AddComponent<OnDestroyHook>().Action = () => mod.UnregisterSuperSoulOrbFlinger(flinger);

        var particles = GameObjectExtensions.FindChild(totem, "Soul Particles").GetComponent<ParticleSystem>();
        particles.emissionRate = EMISSION_RATE;
        particles.startSize = PARTICLE_SIZE;
        particles.maxParticles = PARTICLE_CAP;
        particles.startLifetime = PARTICLE_LIFETIME;
    }
}
