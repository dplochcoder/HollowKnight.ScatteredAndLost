using GlobalEnums;
using HK8YPlando.Scripts.Proxy;
using HK8YPlando.Scripts.SharedLib;
using HK8YPlando.Util;
using System.Collections.Generic;
using UnityEngine;
using static FSMActionReplacements;

namespace HK8YPlando.Scripts.Platforming;

[Shim]
internal class Bubble : MonoBehaviour
{
    [ShimField] public Animator? BubbleAnimator;
    [ShimField] public RuntimeAnimatorController? IdleController;
    [ShimField] public RuntimeAnimatorController? ActiveController;
    [ShimField] public RuntimeAnimatorController? DissolveController;

    [ShimField] public HeroDetectorProxy? HeroDetector;
    [ShimField] public BubblePlayerDetector? PlayerDetector;

    [ShimField] public float StallTime;
    [ShimField] public float Speed;

    private void Awake() => this.StartLibCoroutine(Run());

    private IEnumerator<CoroutineElement> Run()
    {
        var origPos = transform.position;

        // FIXME
        yield break;
    }
}

[Shim]
[RequireComponent(typeof(Collider2D))]
internal class BubblePlayerDetector : MonoBehaviour
{
    [ShimField] public Bubble? Bubble;

    private Vector3 prevPrevPos;
    private Vector3 prevPos;

    private void Update()
    {
        prevPrevPos = prevPos;
        prevPos = transform.position;
    }

    private void OnTriggerEnter2D(Collider2D collider)
    {
        var hc = HeroController.instance;
        var knight = hc.gameObject;

        if (collider.gameObject.layer == (int)PhysLayers.TERRAIN)
        {
            knight.transform.position = prevPrevPos;
            knight.SetActive(true);
            // FIXME
        }
    }
}
