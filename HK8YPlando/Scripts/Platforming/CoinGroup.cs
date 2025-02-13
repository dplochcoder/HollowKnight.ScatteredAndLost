using GlobalEnums;
using HK8YPlando.Scripts.Proxy;
using HK8YPlando.Scripts.SharedLib;
using HK8YPlando.Util;
using Modding;
using PurenailCore.GOUtil;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HK8YPlando.Scripts.Platforming;

[Shim]
internal class CoinGroup : MonoBehaviour
{
    [ShimField] public AudioClip? FinishedClip;

    private List<CoinDoor> doors = [];
    private List<Coin> coins = [];

    private void Awake()
    {
        doors = gameObject.FindComponentsRecursive<CoinDoor>().ToList();
        coins = gameObject.FindComponentsRecursive<Coin>().ToList();

        ModHooks.TakeDamageHook += OnTakeDamage;
    }

    private void OnDestroy() => ModHooks.TakeDamageHook -= OnTakeDamage;

    private int OnTakeDamage(ref int hazardType, int damage)
    {
        if (damage > 0 && hazardType == (int)HazardType.SPIKES + 1)
        {
            coins.ForEach(c => c.Deactivate());
            doors.ForEach(d => d.Close());
            doorsOpened = false;
        }

        return damage;
    }

    private bool doorsOpened = false;

    private void Update()
    {
        if (doorsOpened) return;
        if (coins.Any(c => !c.IsActivated())) return;

        doors.ForEach(d => d.Open());
        doors.FirstOrDefault()?.gameObject.PlaySound(FinishedClip!, 0.7f);
        doorsOpened = true;
    }
}

[Shim]
internal class Coin : MonoBehaviour
{
    private const string SPEED_MULTIPLIER = "SpeedMultiplier";

    [ShimField] public List<SpriteRenderer> Renderers = [];
    [ShimField] public ParticleSystem? ParticleSystem;
    [ShimField] public Animator? Animator;
    [ShimField] public HeroDetectorProxy? HeroDetector;

    [ShimField] public AudioClip? ObtainedClip;

    [ShimField] public Color IdleColor;
    [ShimField] public Color FlashColor;
    [ShimField] public Color ActiveColor;
    [ShimField] public float CooldownTime;

    [ShimField] public float FlashTransitionTime;
    [ShimField] public float FlashHangTime;
    [ShimField] public float FlashAnimationSpeed;
    [ShimField] public float ActiveTransitionTime;

    private Color _currentColor;
    private Color currentColor
    {
        get { return _currentColor; }
        set
        {
            _currentColor = value;
            Renderers.ForEach(r => r.color = value);
        }
    }

    private bool activated = false;
    private bool onCooldown = false;

    private void Awake()
    {
        currentColor = IdleColor;
        HeroDetector?.OnDetected(MaybeHit);

        this.StartLibCoroutine(Run());
    }

    internal bool IsActivated() => activated;

    internal void Deactivate() => activated = false;

    internal void MaybeHit()
    {
        if (activated || onCooldown) return;
        activated = true;
    }

    private IEnumerator<CoroutineElement> Run()
    {
        while (true)
        {
            yield return Coroutines.SleepUntil(() => activated);

            yield return Coroutines.OneOf(
                Coroutines.SleepUntil(() => !activated),
                Coroutines.Sequence(ActivateCoin()));
            yield return Coroutines.SleepUntil(() => !activated);

            onCooldown = true;
            var prevColor = currentColor;
            var prevSpeed = Animator!.GetFloat(SPEED_MULTIPLIER);
            yield return Coroutines.SleepSecondsUpdatePercent(CooldownTime, pct =>
            {
                currentColor = prevColor.Interpolate(pct, IdleColor);
                Animator!.SetFloat(SPEED_MULTIPLIER, 1 + (prevSpeed - 1) * (1 - pct));
                return false;
            });
            onCooldown = false;
        }
    }

    private IEnumerator<CoroutineElement> ActivateCoin()
    {
        gameObject.PlaySound(ObtainedClip!, 0.45f);
        ParticleSystem?.Play();

        yield return Coroutines.SleepSecondsUpdatePercent(FlashTransitionTime, pct =>
        {
            currentColor = IdleColor.Interpolate(pct, FlashColor);
            Animator!.SetFloat(SPEED_MULTIPLIER, 1 + (FlashAnimationSpeed - 1) * pct);
            return false;
        });

        yield return Coroutines.SleepSeconds(FlashHangTime);

        yield return Coroutines.SleepSecondsUpdatePercent(ActiveTransitionTime, pct =>
        {
            currentColor = FlashColor.Interpolate(pct, ActiveColor);
            Animator!.SetFloat(SPEED_MULTIPLIER, 1 + (FlashAnimationSpeed - 1) * (1 - pct));
            return false;
        });
    }
}

[Shim]
internal class CoinNailDetector : MonoBehaviour, IHitResponder
{
    [ShimField] public Coin? Coin;

    public void Hit(HitInstance damageInstance)
    {
        if (damageInstance.AttackType == AttackTypes.Nail) Coin?.MaybeHit();
    }
}

[Shim]
internal class CoinDoor : MonoBehaviour
{
    [ShimField] public GameObject? ShakeBase;
    [ShimField] public SpriteRenderer? MarkerRenderer;
    [ShimField] public Animator? MarkerAnimator;
    [ShimField] public Sprite? InactiveMarkerSprite;
    [ShimField] public RuntimeAnimatorController? ActiveMarkerController;
    [ShimField] public Color IdleColor;
    [ShimField] public Color ActiveColor;

    [ShimField] public float ShakeRadius;
    [ShimField] public float ShakeTime;
    [ShimField] public float AfterShakeDelay;

    [ShimField] public float MoveDuration;
    [ShimField] public Vector3 MoveOffset;
    [ShimField] public float ResetDelay;
    [ShimField] public float ResetDuration;

    private Vector3 srcPos;
    private Vector3 destPos;

    private void Awake()
    {
        srcPos = transform.position;
        destPos = srcPos + MoveOffset;
        currentColor = IdleColor;

        this.StartLibCoroutine(Run());
    }

    private bool opened = false;

    private IEnumerator<CoroutineElement> Run()
    {
        while (true)
        {
            yield return Coroutines.SleepUntil(() => opened);

            yield return Coroutines.OneOf(
                Coroutines.SleepUntil(() => !opened),
                Coroutines.Sequence(OpenDoor()));
            yield return Coroutines.SleepUntil(() => !opened);
            ShakeBase!.transform.localPosition = Vector2.zero;

            var prevColor = currentColor;
            var prevPos = transform.position;
            yield return Coroutines.SleepSecondsUpdatePercent(ResetDelay, pct =>
            {
                currentColor = prevColor.Interpolate(pct, IdleColor);
                return false;
            });

            MarkerAnimator!.runtimeAnimatorController = null;
            MarkerRenderer!.sprite = InactiveMarkerSprite!;
            yield return Coroutines.SleepSecondsUpdatePercent(ResetDuration, pct =>
            {
                transform.position = prevPos.Interpolate(Mathf.Sin(pct * Mathf.PI / 2), srcPos);
                return false;
            });
        }
    }

    internal void Open() => opened = true;

    internal void Close() => opened = false;

    private Color _currentColor;
    private Color currentColor
    {
        get {  return _currentColor; }
        set
        {
            _currentColor = value;
            MarkerRenderer!.color = value;
        }
    }

    private IEnumerator<CoroutineElement> OpenDoor()
    {
        MarkerAnimator!.runtimeAnimatorController = ActiveMarkerController;
        yield return Coroutines.SleepSecondsUpdatePercent(ShakeTime, pct =>
        {
            currentColor = IdleColor.Interpolate(pct, ActiveColor);
            ShakeBase!.transform.localPosition = MathExt.RandomInCircle(Vector2.zero, ShakeRadius);
            return false;
        });
        ShakeBase!.transform.localPosition = Vector2.zero;

        yield return Coroutines.SleepSeconds(AfterShakeDelay);

        yield return Coroutines.SleepSecondsUpdatePercent(MoveDuration, pct =>
        {
            transform.position = srcPos.Interpolate(Mathf.Sin(pct * Mathf.PI / 2), destPos);
            return false;
        });
    }
}
