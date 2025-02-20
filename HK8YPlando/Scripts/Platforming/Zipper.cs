using HK8YPlando.Scripts.Environment;
using HK8YPlando.Scripts.InternalLib;
using HK8YPlando.Scripts.SharedLib;
using HK8YPlando.Util;
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

    private Vector3 restPos;
    private Vector3 targetPos;
    private List<SpriteRenderer> lineCogs = [];
    private List<GameObject> platCogs = [];

    private void Awake()
    {
        restPos = RestPosition!.position;
        targetPos = TargetPosition!.position;
        lineCogs = gameObject.FindComponentsRecursive<ZipperLineCog>().Select(b => b.gameObject.GetComponent<SpriteRenderer>()).ToList();
        platCogs = gameObject.FindComponentsRecursive<ZipperPlatformCog>().Select(b => b.gameObject).ToList();

        this.StartLibCoroutine(Run());
    }

    private void Update()
    {
        var pos = (Platform!.transform.position - restPos).magnitude;
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

        var travelDist = (targetPos - restPos).magnitude;
        var rewindTime = travelDist / RewindSpeed;
        var shootTime = (Mathf.Sqrt(2 * Accel * travelDist + StartSpeed * StartSpeed) - StartSpeed) / Accel;

        bool disableBottomSpikes = BottomHurtBox!.gameObject.activeSelf && (targetPos.y - restPos.y) >= -0.1f;
        Platform.Light!.sprite = RedLightSprite!;

        while (true)
        {
            yield return Coroutines.SleepUntil(() => stick.PlayerAttached);
            Platform.gameObject.PlaySound(TouchClips.Random(), 0.6f);

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
