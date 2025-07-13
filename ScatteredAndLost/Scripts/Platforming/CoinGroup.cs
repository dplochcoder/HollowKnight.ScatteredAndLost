using Architect.Attributes.Config;
using Architect.Content.Elements;
using DecorationMaster;
using DecorationMaster.Attr;
using DecorationMaster.MyBehaviour;
using GlobalEnums;
using HK8YPlando.Scripts.Proxy;
using HK8YPlando.Scripts.SharedLib;
using HK8YPlando.Util;
using IL.InControl.UnityDeviceProfiles;
using Modding;
using PurenailCore.GOUtil;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HK8YPlando.Scripts.Platforming;

internal class CoinGroupController
{
    private bool doorsOpened = false;
    private bool tookDamage = false;

    internal CoinGroupController()
    {
        ModHooks.TakeDamageHook += OnTakeDamage;
    }

    private int OnTakeDamage(ref int hazardType, int damage)
    {
        if (damage > 0 && hazardType == (int)HazardType.SPIKES + 1) tookDamage = true;
        return damage;
    }

    internal void Update(IEnumerable<Coin> coins, IEnumerable<CoinDoor> coinDoors)
    {
        if (tookDamage || coins.Count() == 0)
        {
            tookDamage = false;

            coins.ForEach(c => c.Deactivate());
            coinDoors.ForEach(d => d.Close());
            doorsOpened = false;
        }
        else if (coins.Any(c => !c.IsActivated()))
        {
            if (doorsOpened)
            {
                coinDoors.ForEach(d => d.Close());
                doorsOpened = false;
            }
        }
        else
        {
            if (!doorsOpened) coinDoors.FirstOrDefault()?.gameObject.PlaySound(ScatteredAndLostSceneManagerAPI.LoadPrefab<AudioClip>("game_gen_touchswitch_last"), 0.7f);

            coinDoors.ForEach(d => d.Open());
            doorsOpened = true;
        }
    }

    internal void Release() => ModHooks.TakeDamageHook -= OnTakeDamage;
}

internal class NumberedCoinGroup
{
    public HashSet<Coin> Coins = [];
    public HashSet<CoinDoor> CoinDoors = [];
    private CoinGroupController controller = new();

    public bool Empty() => Coins.Count == 0 && CoinDoors.Count == 0;

    public void Update() => controller.Update(Coins, CoinDoors);

    public void Release() => controller.Release();
}

internal class NumberedCoinGroups : MonoBehaviour
{
    private Dictionary<int, NumberedCoinGroup> groups = [];

    internal static NumberedCoinGroups Get()
    {
        var go = GameObject.Find("NumberedCoinGroups");
        if (go == null)
        {
            go = new("NumberedCoinGroups");
            go.AddComponent<NumberedCoinGroups>();
        }
        return go.GetComponent<NumberedCoinGroups>();
    }

    private void Update() => groups.Values.ForEach(v => v.Update());

    internal void AddCoin(Coin coin) => groups.GetOrAddNew(coin.GateNumber).Coins.Add(coin);

    internal void AddCoinDoor(CoinDoor coinDoor) => groups.GetOrAddNew(coinDoor.GateNumber).CoinDoors.Add(coinDoor);

    internal void RemoveCoin(Coin coin)
    {
        if (!groups.TryGetValue(coin.GateNumber, out var group)) return;

        group.Coins.Remove(coin);
        if (group.Empty())
        {
            group.Release();
            groups.Remove(coin.GateNumber);
        }
    }

    internal void RemoveCoinDoor(CoinDoor coinDoor)
    {
        if (!groups.TryGetValue(coinDoor.GateNumber, out var group)) return;

        group.CoinDoors.Remove(coinDoor);
        if (group.Empty())
        {
            group.Release();
            groups.Remove(coinDoor.GateNumber);
        }
    }
}

[Shim]
internal class CoinGroup : MonoBehaviour
{
    private List<Coin> coins = [];
    private List<CoinDoor> coinDoors = [];
    private CoinGroupController controller = new();

    private void Awake()
    {
        coins = gameObject.FindComponentsRecursive<Coin>().ToList();
        coinDoors = gameObject.FindComponentsRecursive<CoinDoor>().ToList();
    }

    private void OnDestroy() => controller.Release();

    private void Update() => controller.Update(coins, coinDoors);
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

    // Used by decoration master
    private int? _gateNumber;
    internal int GateNumber
    {
        get { return _gateNumber!.Value; }
        set {
            var singleton = NumberedCoinGroups.Get();

            if (_gateNumber.HasValue) singleton.RemoveCoin(this);
            _gateNumber = value;
            singleton.AddCoin(this);
        }
    }

    private void OnDestroy()
    {
        if (_gateNumber.HasValue) NumberedCoinGroups.Get().RemoveCoin(this);
    }

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

    // Used by decoration master
    private int? _gateNumber;
    internal int GateNumber
    {
        get { return _gateNumber!.Value; }
        set
        {
            var singleton = NumberedCoinGroups.Get();

            if (_gateNumber.HasValue) singleton.RemoveCoinDoor(this);
            _gateNumber = value;
            singleton.AddCoinDoor(this);
        }
    }

    private void OnDestroy()
    {
        if (_gateNumber.HasValue) NumberedCoinGroups.Get().RemoveCoinDoor(this);
    }

    private Vector3? _decoMasterPos;
    internal void DecoMasterSetPos(Vector3 newPos)
    {
        if (!_decoMasterPos.HasValue)
        {
            _decoMasterPos = newPos;
            srcPos = newPos;
            destPos = srcPos + MoveOffset;
            transform.position = srcPos;
            return;
        }

        var delta = newPos - _decoMasterPos.Value;
        _decoMasterPos = newPos;

        srcPos += delta;
        destPos += delta;
        transform.position += delta;
    }

    internal void DecoMasterSetMoveOffset(Vector3 offset)
    {
        var dist = (transform.position - srcPos).magnitude / MoveOffset.magnitude;

        MoveOffset = offset;
        destPos = srcPos + offset;
        transform.position = srcPos + dist * offset.normalized;
    }

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

[Serializable]
internal class CoinDecorationItem : Item
{
    internal const int MAX_GROUP = 20;

    [Description("Group number for switch(es) + door(s)", "en-us")]
    [Handle(Operation.SetGate)]
    [IntConstraint(1, MAX_GROUP)]
    public int Gate { get; set; } = 1;
}

[Description("Celeste Switch\nSet gate number to match doors", "en-us")]
[Decoration("scattered_and_lost_switch")]
internal class CoinDecoration : CustomDecoration
{
    public static void Register() => DecorationMasterUtil.RegisterDecoration<CoinDecoration, CoinDecorationItem>(
        "scattered_and_lost_switch",
        ScatteredAndLostSceneManagerAPI.LoadPrefab<GameObject>("Switch"),
        "switch");

    private void Awake() => UnVisableBehaviour.AttackReact.Create(gameObject);

    private void Start() => SetGate(((CoinDecorationItem)item).Gate);

    [Handle(Operation.SetGate)]
    public void SetGate(int gate) => gameObject.GetComponent<Coin>().GateNumber = gate;
}

public static class CoinArchitectObject
{
    public static AbstractPackElement Create() => ArchitectUtil.MakeArchitectObject(
        "Switch", "Switch", "switch",
        ArchitectUtil.Generic,
        new IntConfigType("Switch.Group", (o, value) => o.GetComponent<Coin>().GateNumber = value.GetValue()));
}

[Serializable]
internal class CoinDoorDecorationItem : Item
{
    [Description("Group number for switch(es) + door(s)", "en-us")]
    [Handle(Operation.SetGate)]
    [IntConstraint(1, CoinDecorationItem.MAX_GROUP)]
    public int Gate { get; set; } = 1;

    [Description("X-Size of block in units", "en-us")]
    [Handle(Operation.SetSizeX)]
    [FloatConstraint(1f, 8f)]
    public float XScale { get; set; } = 2;

    [Description("Y-Size of block in units", "en-us")]
    [Handle(Operation.SetSizeY)]
    [FloatConstraint(1f, 8f)]
    public float YScale { get; set; } = 2;

    [Description("Distance moved on x-axis when opened", "en-us")]
    [Handle(Operation.SetColorR)]
    [FloatConstraint(-20f, 20f)]
    public float XOpen { get; set; } = 0;

    [Description("Distance moved on y-axis when opened", "en-us")]
    [Handle(Operation.SetColorG)]
    [FloatConstraint(-20f, 20f)]
    public float YOpen { get; set; } = 2;
}

[Description("Celeste Switch Door\nSet gate number to match switches\nSet XOpen and YOpen for gate direction\nWill not open without switches", "en-us")]
[Decoration("scattered_and_lost_switch_door")]
internal class CoinDoorDecoration : CustomDecoration
{
    public static void Register() => DecorationMasterUtil.RegisterDecoration<CoinDoorDecoration, CoinDoorDecorationItem>(
        "scattered_and_lost_switch_door",
        ScatteredAndLostSceneManagerAPI.LoadPrefab<GameObject>("SSwitchDoor"),
        "switchdoor");

    private void Awake() => UnVisableBehaviour.AttackReact.Create(gameObject);

    private const float SCALE_MULTIPLIER = 5f / 12;

    private (GameObject, BoxCollider2D) GetBlockAndTerrain()
    {
        var block = gameObject.FindChild("ShakeBase").FindChild("Block");
        var terrain = gameObject.FindChild("Terrain").GetComponent<BoxCollider2D>();
        return (block, terrain);
    }

    private void Start()
    {
        var itemTyped = (CoinDoorDecorationItem)item;

        var coinDoor = gameObject.GetComponent<CoinDoor>();
        coinDoor.GateNumber = itemTyped.Gate;
        coinDoor.DecoMasterSetMoveOffset(new(itemTyped.XOpen, itemTyped.YOpen));

        var (block, terrain) = GetBlockAndTerrain();
        block.transform.localScale = new(itemTyped.XScale * SCALE_MULTIPLIER, itemTyped.YScale * SCALE_MULTIPLIER);
        terrain.size = new(itemTyped.XScale, itemTyped.YScale);
    }

    public override void HandlePos(Vector2 val) => gameObject.GetComponent<CoinDoor>().DecoMasterSetPos(val);

    [Handle(Operation.SetGate)]
    public void SetGate(int gate) => gameObject.GetComponent<CoinDoor>().GateNumber = gate;

    [Handle(Operation.SetSizeX)]
    public void SetXScale(float x)
    {
        var (block, terrain) = GetBlockAndTerrain();
        block.transform.SetScaleX(x * SCALE_MULTIPLIER);
        terrain.size = new(x, terrain.size.y);
    }

    [Handle(Operation.SetSizeY)]
    public void SetYScale(float y)
    {
        var (block, terrain) = GetBlockAndTerrain();
        block.transform.SetScaleY(y * SCALE_MULTIPLIER);
        terrain.size = new(terrain.size.x, y);
    }

    [Handle(Operation.SetColorR)]
    public void SetXMove(float x)
    {
        var coinDoor = gameObject.GetComponent<CoinDoor>();

        var offset = coinDoor.MoveOffset;
        offset.x = x;
        coinDoor.DecoMasterSetMoveOffset(offset);
    }

    [Handle(Operation.SetColorG)]
    public void SetYMove(float y)
    {
        var coinDoor = gameObject.GetComponent<CoinDoor>();

        var offset = coinDoor.MoveOffset;
        offset.y = y;
        coinDoor.DecoMasterSetMoveOffset(offset);
    }
}

public static class CoinDoorArchitectObject
{
    public static AbstractPackElement Create() => ArchitectUtil.MakeArchitectObject(
        "SSwitchDoor", "Switch Door", "switchdoor",
        ArchitectUtil.Stretchable,
        new IntConfigType("SwitchDoor.Group", (o, value) => o.GetComponent<CoinDoor>().GateNumber = value.GetValue()),
        new FloatConfigType("SwitchDoor.XMove", (o, value) => o.GetComponent<CoinDoor>().UpdateMoveOffset(m => m with { x = value.GetValue() })),
        new FloatConfigType("SwitchDoor.YMove", (o, value) => o.GetComponent<CoinDoor>().UpdateMoveOffset(m => m with { y = value.GetValue() })));

    private static void UpdateMoveOffset(this CoinDoor self, Func<Vector3, Vector3> func) => self.DecoMasterSetMoveOffset(func(self.MoveOffset));
}
