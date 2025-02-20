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
    [ShimField] public int FlingMin;
    [ShimField] public int FlingMax;

    private void Awake()
    {
        var totem = Instantiate(HK8YPlandoPreloader.Instance.SoulTotem);
        totem.transform.position = transform.position;
        totem.transform.localScale = transform.localScale;
        totem.SetActive(true);

        var fsm = totem.LocateMyFSM("soul_totem");
        fsm.GetFsmState("Close").AddFirstAction(new Lambda(() => fsm.FsmVariables.GetFsmInt("Value").Value = 3));
        var hit = fsm.GetFsmState("Hit");
        hit.AddFirstAction(new Lambda(() => fsm.FsmVariables.GetFsmInt("Value").Value = 3));
        var flinger = hit.GetFirstActionOfType<FlingObjectsFromGlobalPool>();
        flinger.spawnMin.Value = FlingMin;
        flinger.spawnMax.Value = FlingMax;

        var particles = GameObjectExtensions.FindChild(totem, "Soul Particles").GetComponent<ParticleSystem>();
        particles.emissionRate = EmissionRate;
        particles.startSize = ParticleSize;
        particles.maxParticles = ParticleCap;
        particles.startLifetime = ParticleLifetime;

        Destroy(gameObject);
    }
}
