using HK8YPlando.IC;
using HK8YPlando.Scripts.Proxy;
using HK8YPlando.Scripts.SharedLib;
using UnityEngine;

namespace HK8YPlando.Scripts.Framework;

[Shim]
[RequireComponent(typeof(HeroDetectorProxy))]
internal class ShadeSpawnTrigger : MonoBehaviour
{
    [ShimField] public ShadeMarker? ShadeMarker;

    private void Awake() => GetComponent<HeroDetectorProxy>().OnDetected(() => BrettasHouse.Get().SetShadeSpawnTrigger(this));

    private void OnDestroy() => BrettasHouse.Get().ForgetShadeSpawnTrigger(this);
}
