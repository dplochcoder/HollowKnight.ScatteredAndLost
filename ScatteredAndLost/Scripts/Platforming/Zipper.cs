using DecorationMaster;
using DecorationMaster.Attr;
using DecorationMaster.MyBehaviour;
using HK8YPlando.Scripts.Environment;
using HK8YPlando.Scripts.InternalLib;
using HK8YPlando.Scripts.SharedLib;
using HK8YPlando.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HK8YPlando.Scripts.Platforming;

[Shim]
internal class Zipper : MonoBehaviour
{
    [ShimField] public List<Sprite> CogSprites = [];
    [ShimField] public Sprite? RedLightSprite;
    [ShimField] public Sprite? YellowLightSprite;
    [ShimField] public Sprite? GreenLightSprite;
    [ShimField] public float RotationPerUnit;
    [ShimField] public float SpritesPerUnit;

    [ShimField] public List<AudioClip> TouchClips = [];
    [ShimField] public List<AudioClip> ImpactClips = [];
    [ShimField] public AudioClip? RewindIntro;
    [ShimField] public AudioClip? RewindLoop;
    [ShimField] public List<AudioClip> ResetClips = [];

    [ShimField] public ZipperPlatform? Platform;
    [ShimField] public DamageHero? BottomHurtBox;

    [ShimField] public Transform? RestPosition;
    [ShimField] public Transform? TargetPosition;
    [ShimField] public float ShakeRadius;
    [ShimField] public float ShakeTime;
    [ShimField] public float StartSpeed;
    [ShimField] public float Accel;
    [ShimField] public float PauseTime;
    [ShimField] public float RewindSpeed;
    [ShimField] public float RewindCooldown;

    private Vector3 RestPos() => RestPosition!.position;
    private Vector3 TargetPos() => TargetPosition!.position;

    private List<SpriteRenderer> lineCogs = [];
    private List<GameObject> platCogs = [];

    private void Awake()
    {
        lineCogs = gameObject.FindComponentsRecursive<ZipperLineCog>().Select(b => b.gameObject.GetComponent<SpriteRenderer>()).ToList();
        platCogs = gameObject.FindComponentsRecursive<ZipperPlatformCog>().Select(b => b.gameObject).ToList();

        this.StartLibCoroutine(Run());
    }

    private void Update()
    {
        var pos = (Platform!.transform.position - RestPos()).magnitude;
        var spriteIndex = Mathf.RoundToInt(pos * SpritesPerUnit) % CogSprites.Count;
        var rot = Quaternion.Euler(0, 0, pos * RotationPerUnit);

        lineCogs.ForEach(s => s.sprite = CogSprites[spriteIndex]);
        platCogs.ForEach(o => o.transform.localRotation = rot);
    }

    private IEnumerator<CoroutineElement> Run()
    {
        var stick = Platform!.GetComponent<HeroPlatformStickImproved>();
        var audio = Platform.gameObject.AddComponent<AudioSource>();
        audio.outputAudioMixerGroup = AudioMixerGroups.Actors();

        Platform.Light!.sprite = RedLightSprite!;
        while (true)
        {
            yield return Coroutines.SleepUntil(() => stick.PlayerAttached);
            Platform.gameObject.PlaySound(TouchClips.Random(), 0.6f);

            var restPos = RestPos();
            var targetPos = TargetPos();
            var travelDist = (targetPos - restPos).magnitude;
            var rewindTime = travelDist / RewindSpeed;
            var shootTime = (Mathf.Sqrt(2 * Accel * travelDist + StartSpeed * StartSpeed) - StartSpeed) / Accel;
            bool disableBottomSpikes = BottomHurtBox!.gameObject.activeSelf && (targetPos.y - restPos.y) >= -0.1f;

            Platform.Light.sprite = GreenLightSprite;
            yield return Coroutines.SleepSecondsUpdateDelta(ShakeTime, _ =>
            {
                Platform!.SpriteShaker!.transform.localPosition = MathExt.RandomInCircle(Vector2.zero, ShakeRadius);
                return false;
            });
            Platform!.SpriteShaker!.transform.localPosition = Vector3.zero;

            if (disableBottomSpikes) BottomHurtBox!.gameObject.SetActive(false);
            Wrapped<float> launchTime = new(0);
            yield return Coroutines.SleepSecondsUpdateDelta(shootTime, time =>
            {
                launchTime.Value += time;

                var d = StartSpeed * launchTime.Value + Accel * launchTime.Value * launchTime.Value / 2;
                Platform!.transform.position = restPos + (targetPos - restPos).normalized * d;
                return false;
            });

            if (disableBottomSpikes) BottomHurtBox!.gameObject.SetActive(true);
            Platform.gameObject.PlaySound(ImpactClips.Random(), 0.6f);

            yield return Coroutines.SleepSeconds(PauseTime);

            Platform.gameObject.PlaySound(RewindIntro!, 1f, false);
            Platform.Light.sprite = YellowLightSprite;
            yield return Coroutines.SleepSecondsUpdatePercent(rewindTime, pct =>
            {
                if (!audio.isPlaying) Platform.gameObject.LoopSound(RewindLoop!, 1f, false);

                Platform!.transform.position = targetPos.Interpolate(pct, restPos);
                return false;
            });

            Platform.gameObject.PlaySound(ResetClips.Random(), 1f, false);

            Platform.Light.sprite = RedLightSprite;
            yield return Coroutines.SleepSeconds(RewindCooldown);
        }
    }
}

[Shim]
[RequireComponent(typeof(SpriteRenderer))]
internal class ZipperLineCog : MonoBehaviour { }

[Shim]
[RequireComponent(typeof(SpriteRenderer))]
internal class ZipperPlatformCog : MonoBehaviour { }

[Shim]
[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(HeroPlatformStickImproved))]
internal class ZipperPlatform : MonoBehaviour
{
    [ShimField] public GameObject? SpriteShaker;
    [ShimField] public SpriteRenderer? Light;
}

[Serializable]
internal class ZipperDecorationItem : Item
{
    [Description("X-Distance to move when activated","en-us")]
    [Handle(Operation.SetSizeX)]
    [FloatConstraint(-40, 40)]
    public float XMove { get; set; } = 6;

    [Description("Y-Distance to move when activated", "en-us")]
    [Handle(Operation.SetSizeY)]
    [FloatConstraint(-40, 40)]
    public float YMove { get; set; } = 0;

    [Handle(Operation.SetColorA)]
    [IntConstraint(0, 1)]
    public int TopSpikes { get; set; } = 0;

    [Handle(Operation.SetColorR)]
    [IntConstraint(0, 1)]
    public int RightSpikes { get; set; } = 1;

    [Handle(Operation.SetColorG)]
    [IntConstraint(0, 1)]
    public int BotSpikes { get; set; } = 1;

    [Handle(Operation.SetColorB)]
    [IntConstraint(0, 1)]
    public int LeftSpikes { get; set; } = 1;
}

[Description("Celeste Zipper\nSet distance with XMove and YMove\nSet spikes with 1", "en-us")]
[Decoration("scattered_and_lost_zipper")]
internal class ZipperDecoration : CustomDecoration
{
    public static void Register() => DecorationMasterUtil.RegisterDecoration<ZipperDecoration, ZipperDecorationItem>(
        "scattered_and_lost_zipper",
        ScatteredAndLostSceneManagerAPI.LoadPrefab<GameObject>("Zipper"),
        "zipper");

    private void Awake() => UnVisableBehaviour.AttackReact.Create(gameObject);

    private void Start()
    {
        var itemTyped = (ZipperDecorationItem)item;

        var zipper = gameObject.GetComponent<Zipper>();
        zipper.TargetPosition!.position = new(itemTyped.XMove, itemTyped.YMove);
        ZipperLib.UpdateZipperAssets(gameObject, itemTyped.TopSpikes == 1, itemTyped.RightSpikes == 1, itemTyped.BotSpikes == 1, itemTyped.LeftSpikes == 1, _ => { });
    }

    public override void HandlePos(Vector2 val)
    {
        var zipper = gameObject.GetComponent<Zipper>();
        var delta = zipper.TargetPosition!.position - zipper.RestPosition!.position;

        zipper.transform.position = val;
        zipper.RestPosition.position = val;
        zipper.TargetPosition.position = val.To3d() + delta;
    }

    private void UpdateZipline()
    {
        var (top, right, bot, left) = ZipperLib.LoadSpikes(gameObject);
        ZipperLib.UpdateZipperAssets(gameObject, top, right, bot, left, _ => { });
    }

    [Handle(Operation.SetSizeX)]
    public void SetXMove(float x)
    {
        var target = gameObject.GetComponent<Zipper>().TargetPosition!;
        target.localPosition = new(x, target.localPosition.y, target.localPosition.z);
        UpdateZipline();
    }

    [Handle(Operation.SetSizeY)]
    public void SetYMove(float y)
    {
        var target = gameObject.GetComponent<Zipper>().TargetPosition!;
        target.localPosition = new(target.localPosition.x, y, target.localPosition.z);
        UpdateZipline();
    }

    [Handle(Operation.SetColorA)]
    public void SetTopSpikes(int value)
    {
        var (_, right, bot, left) = ZipperLib.LoadSpikes(gameObject);
        ZipperLib.UpdateZipperAssets(gameObject, value == 1, right, bot, left, _ => { });
    }

    [Handle(Operation.SetColorR)]
    public void SetRightSpikes(int value)
    {
        var (top, _, bot, left) = ZipperLib.LoadSpikes(gameObject);
        ZipperLib.UpdateZipperAssets(gameObject, top, value == 1, bot, left, _ => { });
    }

    [Handle(Operation.SetColorG)]
    public void SetBotSpikes(int value)
    {
        var (top, right, _, left) = ZipperLib.LoadSpikes(gameObject);
        ZipperLib.UpdateZipperAssets(gameObject, top, right, value == 1, left, _ => { });
    }

    [Handle(Operation.SetColorB)]
    public void SetLeftSpikes(int value)
    {
        var (top, right, bot, _) = ZipperLib.LoadSpikes(gameObject);
        ZipperLib.UpdateZipperAssets(gameObject, top, right, bot, value == 1, _ => { });
    }
}
