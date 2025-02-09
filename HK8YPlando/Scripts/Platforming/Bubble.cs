using GlobalEnums;
using HK8YPlando.Scripts.InternalLib;
using HK8YPlando.Scripts.Proxy;
using HK8YPlando.Scripts.SharedLib;
using HK8YPlando.Util;
using System.Collections.Generic;
using System.Net.Http.Headers;
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

    [ShimField] public GameObject? Bubble;
    [ShimField] public Rigidbody2D? RigidBody;
    [ShimField] public HeroDetectorProxy? Trigger;

    [ShimField] public float StallTime;
    [ShimField] public float Speed;
    [ShimField] public float RespawnDelay;
    [ShimField] public float RespawnCooldown;
    [ShimField] public Vector3 KnightOffset;

    private void Awake() => this.StartLibCoroutine(Run());

    private bool finishedMoving;
    private Vector3 targetPos;
    private bool wallCling;
    private DamageHero? damageHero;

    internal void FinishMoving(Vector3 targetPos, bool wallCling, DamageHero? damageHero)
    {
        finishedMoving = true;
        this.targetPos = targetPos;
        this.wallCling = wallCling;
        this.damageHero = damageHero;
    }

    private Vector2 ComputeVelocity(bool facingRight)
    {
        var vec = InputHandler.Instance.inputActions.moveVector;
        Vector2 dir = new(vec.X, vec.Y);
        if (dir.sqrMagnitude <= 0.25f) return facingRight ? new(Speed, 0) : new(-Speed, 0);

        var angle = Mathf.Round(Mathf.Atan2(dir.y, dir.x) * 4 / Mathf.PI) * Mathf.PI / 4;
        return new(Mathf.Cos(angle) * Speed, Mathf.Sin(angle) * Speed);
    }

    private CollisionSide ComputeCollisionSide(Vector2 velocity)
    {
        if (velocity.x > 0.1f) return CollisionSide.right;
        else if (velocity.x < -0.1f) return CollisionSide.left;
        else if (velocity.y > 0.1f) return CollisionSide.top;
        else return CollisionSide.bottom;
    }

    private bool tookControl = false;

    private void OnDestroy()
    {
        if (tookControl)
        {
            var hc = HeroController.instance;
            var knight = hc.gameObject;
            var renderer = knight.GetComponent<MeshRenderer>();

            knight.SetActive(true);
            hc.RegainControl();
        }
    }
    
    private IEnumerator<CoroutineElement> Run()
    {
        var origPos = transform.position;
        Trigger!.gameObject.SetActive(true);

        var hc = HeroController.instance;
        var knight = hc.gameObject;
        var renderer = knight.GetComponent<MeshRenderer>();
        var input = InputHandler.Instance;

        while (true)
        {
            BubbleAnimator!.runtimeAnimatorController = IdleController;
            yield return Coroutines.SleepUntil(() => Trigger!.Detected());

            var facingRight = hc.cState.facingRight;
            hc.RelinquishControl();
            knight.SetActive(false);
            tookControl = true;

            BubbleAnimator!.runtimeAnimatorController = FillController;
            yield return Coroutines.SleepSeconds(StallTime);

            RigidBody!.velocity = ComputeVelocity(facingRight);

            Wrapped<bool> dashReleased = new(false);

            BubbleAnimator!.runtimeAnimatorController = ActiveController;
            yield return Coroutines.SleepUntil(() =>
            {
                if (!dashReleased.Value)
                {
                    if (!input.inputActions.dash) dashReleased.Value = true;
                }
                else if (input.inputActions.dash)
                {
                    knight.transform.position = Bubble!.transform.position + KnightOffset;
                    knight.SetActive(true);
                    hc.SetStartWithDash();
                    hc.RegainControl();
                    hc.SetAirDashed(true);
                    hc.SetDoubleJumped(false);
                    tookControl = false;

                    return true;
                }

                if (finishedMoving) return true;

                knight.transform.position = Bubble!.transform.position + KnightOffset;
                return false;
            });

            if (finishedMoving)
            {
                knight.transform.position = targetPos + KnightOffset;
                knight.SetActive(true);
                if (wallCling) hc.SetStartWithWallslide();
                hc.RegainControl();
                hc.SetAirDashed(false);
                hc.SetDoubleJumped(false);
                tookControl = false;

                if (damageHero != null) hc.TakeDamage(damageHero.gameObject, ComputeCollisionSide(RigidBody.velocity), damageHero.damageDealt, damageHero.hazardType);
            }

            finishedMoving = false;
            damageHero = null;
            RigidBody.velocity = Vector2.zero;
            BubbleAnimator!.runtimeAnimatorController = DissolveController;
            yield return Coroutines.SleepSeconds(RespawnDelay);

            Bubble!.transform.position = origPos;
            BubbleAnimator!.runtimeAnimatorController = RespawnController;
            yield return Coroutines.SleepSeconds(RespawnCooldown);
        }
    }

    private void FixedUpdate()
    {
        if (tookControl) HeroController.instance.gameObject.transform.position = Bubble!.transform.position;
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

        BubbleController!.FinishMoving(pos, wallCling, null);
    }
}

[Shim]
internal class BubbleHeroProxy : MonoBehaviour
{
    [ShimField] public BubbleController? BubbleController;

    private void OnTriggerEnter2D(Collider2D collider) => HandleTrigger(collider);

    private void OnTriggerStay2D(Collider2D collider) => HandleTrigger(collider);

    private void HandleTrigger(Collider2D collider)
    {
        var damageHero = collider.gameObject.GetComponent<DamageHero>();
        if (damageHero == null) return;

        BubbleController!.FinishMoving(transform.position, false, damageHero);
    }
}
