using HK8YPlando.IC;
using HK8YPlando.Scripts.Proxy;
using HK8YPlando.Scripts.SharedLib;
using HK8YPlando.Util;
using System.Collections.Generic;
using UnityEngine;

namespace HK8YPlando.Scripts.Platforming;

[Shim]
internal class BrettorLordsDoor : MonoBehaviour
{
    [ShimField] public CoinDoor? Door;
    [ShimField] public HeroDetectorProxy? Detector;
    [ShimField] public AudioClip? OpenClip;

    private void Awake() => this.StartLibCoroutine(Run());

    private IEnumerator<CoroutineElement> Run()
    {
        var mod = BrettasHouse.Get();
        yield return Coroutines.SleepUntil(() => mod.DefeatedBrettorLords && Detector!.Detected());

        gameObject.PlayOneShot(OpenClip!);
        Door!.Open();
    }
}
