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
    [ShimField] public float EmissionRate;
    [ShimField] public float ParticleLifetime;
    [ShimField] public float ParticleSize;
    [ShimField] public int ParticleCap;

    private void Awake()
    {
        var totem = Instantiate(ScatteredAndLostPreloader.Instance.SoulTotem);

        var data = totem.GetComponent<PersistentIntItem>().persistentIntData;
        data.value = 3;
        data.semiPersistent = false;
        data.id = "SuperSoulTotem";
        data.sceneName = "BrettasHouse";

        totem.transform.position = transform.position;
        totem.transform.localScale = transform.localScale;
        totem.SetActive(true);

        var fsm = totem.LocateMyFSM("soul_totem");
        fsm.GetFsmState("Close").AddFirstAction(new Lambda(() => fsm.FsmVariables.GetFsmInt("Value").Value = 3));
        var hit = fsm.GetFsmState("Hit");
        hit.AddFirstAction(new Lambda(() => fsm.FsmVariables.GetFsmInt("Value").Value = 3));
        var flinger = hit.GetFirstActionOfType<FlingObjectsFromGlobalPool>();
        flinger.spawnMin.Value = 11;
        flinger.spawnMax.Value = 11;

        var mod = BrettasHouse.Get();
        mod.RegisterSuperSoulOrbFlinger(flinger);
        totem.AddComponent<OnDestroyHook>().Action = () => mod.UnregisterSuperSoulOrbFlinger(flinger);

        var particles = GameObjectExtensions.FindChild(totem, "Soul Particles").GetComponent<ParticleSystem>();
        particles.emissionRate = EmissionRate;
        particles.startSize = ParticleSize;
        particles.maxParticles = ParticleCap;
        particles.startLifetime = ParticleLifetime;

        Destroy(gameObject);
    }
}
