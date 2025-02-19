using HK8YPlando.IC;
using HK8YPlando.Scripts.InternalLib;
using HK8YPlando.Scripts.Proxy;
using HK8YPlando.Scripts.SharedLib;
using HK8YPlando.Util;
using Modding;
using System.Collections.Generic;
using UnityEngine;

namespace HK8YPlando.Scripts.Platforming;

[Shim]
internal class BubbleController : MonoBehaviour
{
    [ShimField] public Animator? BubbleAnimator;
    [ShimField] public RuntimeAnimatorController? IdleController;
    [ShimField] public RuntimeAnimatorController? FillController;
    [ShimField] public RuntimeAnimatorController? ActiveController;
    [ShimField] public RuntimeAnimatorController? DissolveController;
    [ShimField] public RuntimeAnimatorController? RespawnController;

    [ShimField] public AudioClip? EntryClip;
    [ShimField] public AudioClip? LoopClip;
    [ShimField] public AudioClip? WallClip;
    [ShimField] public AudioClip? DashClip;
    [ShimField] public AudioClip? RespawnClip;

    [ShimField] public GameObject? Bubble;
    [ShimField] public Rigidbody2D? RigidBody;
    [ShimField] public HeroDetectorProxy? Trigger;

    [ShimField] public float StallTime;
    [ShimField] public float Speed;
    [ShimField] public float RespawnDelay;
    [ShimField] public float RespawnCooldown;
    [ShimField] public Vector3 KnightOffset;

    private void Awake()
    {
        ModHooks.TakeDamageHook += OnTakeDamage;

        Trigger!.Ignore(Bubble!.GetComponent<Collider2D>());
        this.StartLibCoroutine(Run());
    }

    private bool finishedMoving;
    private Vector3 targetPos;
    private bool wallCling;
    private bool damageHeroEvent;

    internal void FinishMoving(Vector3 targetPos, bool wallCling)
    {
        finishedMoving = true;
        this.targetPos = targetPos;
        this.wallCling = wallCling;
    }

    private Vector2 ComputeVelocity(bool facingRight)
    {
        var vec = InputHandler.Instance.inputActions.moveVector;
        Vector2 dir = new(vec.X, vec.Y);
        if (dir.sqrMagnitude <= 0.25f) return facingRight ? new(Speed, 0) : new(-Speed, 0);

        var angle = Mathf.Round(Mathf.Atan2(dir.y, dir.x) * 4 / Mathf.PI) * Mathf.PI / 4;
        return new(Mathf.Cos(angle) * Speed, Mathf.Sin(angle) * Speed);
    }

    private int OnTakeDamage(ref int hazardType, int damage)
    {
        if (hazardType > 1 && damage > 0 && owningBubbleController == this) damageHeroEvent = true;
        return damage;
    }

    private static BubbleController? owningBubbleController;

    private void OnDestroy()
    {
        ModHooks.TakeDamageHook -= OnTakeDamage;

        if (owningBubbleController == this)
        {
            owningBubbleController = null;

            var hc = HeroController.instance;
            var knight = hc.gameObject;
            var renderer = knight.GetComponent<MeshRenderer>();

            hc.AffectedByGravity(true);
            renderer.enabled = true;

            hc.RegainControl();
        }
    }
    
    private IEnumerator<CoroutineElement> Run()
    {
        var origPos = transform.position;

        var hc = HeroController.instance;
        var knight = hc.gameObject;
        var renderer = knight.GetComponent<MeshRenderer>();
        var heroRb2d = knight.GetComponent<Rigidbody2D>();
        var input = InputHandler.Instance;
        var particleSystem = Bubble!.GetComponent<ParticleSystem>();

        while (true)
        {
            BubbleAnimator!.runtimeAnimatorController = IdleController;
            yield return Coroutines.SleepUntil(() => Trigger!.Detected());
            Bubble!.gameObject.PlaySound(EntryClip!);

            var facingRight = hc.cState.facingRight;
            if (owningBubbleController == null)
            {
                hc.CancelAttack();
                hc.CancelRecoilHorizontal();
                hc.CancelBounce();
                hc.CancelFallEffects();
                hc.cState.nailCharging = false;
                hc.SetNailChargeTimer(0);

                heroRb2d.velocity = Vector2.zero;
                hc.AffectedByGravity(false);
                BumperModule.Get().CancelBump();

                renderer.enabled = false;

                hc.RelinquishControl();
            }
            owningBubbleController = this;

            BubbleAnimator!.runtimeAnimatorController = FillController;
            yield return Coroutines.SleepSeconds(StallTime);

            RigidBody!.simulated = true;
            RigidBody.velocity = ComputeVelocity(facingRight);
            particleSystem.Play();

            Wrapped<bool> dashReleased = new(false);

            BubbleAnimator!.runtimeAnimatorController = ActiveController;
            finishedMoving = false;
            damageHeroEvent = false;
            bool dashed = false;
            Bubble.gameObject.LoopSound(LoopClip!, 0.6f);
            yield return Coroutines.SleepUntil(() =>
            {
                if (damageHeroEvent || owningBubbleController != this || finishedMoving) return true;

                if (!dashReleased.Value)
                {
                    if (!input.inputActions.dash) dashReleased.Value = true;
                }
                else if (input.inputActions.dash)
                {
                    dashed = true;
                    return true;
                }

                knight.transform.position = Bubble!.transform.position + KnightOffset;
                return false;
            });

            if (owningBubbleController == this)
            {
                Bubble.gameObject.PlaySound((!damageHeroEvent && dashed) ? DashClip! : WallClip!);

                hc.AffectedByGravity(true);
                heroRb2d.velocity = Vector2.zero;

                if (!damageHeroEvent)
                {
                    var basePos = finishedMoving ? targetPos : Bubble!.transform.position;
                    knight.transform.position = basePos + KnightOffset;

                    renderer.enabled = true;
                    if (dashed) hc.SetStartWithDash();
                    else if (wallCling) hc.SetStartWithWallslide();
                    hc.RegainControl();
                    hc.SetAirDashed(dashed);
                    hc.SetDoubleJumped(false);
                }

                owningBubbleController = null;
            }

            finishedMoving = false;
            damageHeroEvent = false;
            RigidBody.velocity = Vector2.zero;
            RigidBody.simulated = false;
            particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            BubbleAnimator!.runtimeAnimatorController = DissolveController;
            yield return Coroutines.SleepSeconds(RespawnDelay);

            Bubble.gameObject.PlaySound(RespawnClip!, 1, false);
            Bubble!.transform.position = origPos;
            BubbleAnimator!.runtimeAnimatorController = RespawnController;
            yield return Coroutines.SleepSeconds(RespawnCooldown);
        }
    }

    private void Update()
    {
        if (owningBubbleController == this)
        {
            var hc = HeroController.instance;
            hc.ResetHardLandingTimer();
            hc.AffectedByGravity(false);
        }
    }

    private void FixedUpdate()
    {
        if (owningBubbleController == this) HeroController.instance.gameObject.transform.position = Bubble!.transform.position + KnightOffset;
    }
}

[Shim]
internal class Bubble : MonoBehaviour
{
    [ShimField] public BubbleController? BubbleController;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.gameObject.layer != 8) return;

        var box = collision.gameObject.GetComponent<BoxCollider2D>()?.bounds ?? collision.gameObject.GetComponent<EdgeCollider2D>().bounds;

        Vector3 pos;
        bool wallCling = false;

        var normal = collision.GetSafeContact().Normal;
        if (Mathf.Abs(normal.y) < 0.1f)
        {
            wallCling = true;
            if (normal.x > 0) pos = new(box.max.x + KnightUtil.WIDTH / 2, transform.position.y);
            else pos = new(box.min.x - KnightUtil.WIDTH / 2, transform.position.y);
        }
        else if (normal.y > 0) pos = new(transform.position.x, box.max.y + KnightUtil.HEIGHT / 2);
        else pos = new(transform.position.x, box.min.y - KnightUtil.HEIGHT / 2);

        BubbleController!.FinishMoving(pos, wallCling);
    }
}
