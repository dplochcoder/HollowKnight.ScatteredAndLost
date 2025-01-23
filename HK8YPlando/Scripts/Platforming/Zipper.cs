using DarknestDungeon.Scripts.Environment;
using HK8YPlando.Scripts.SharedLib;
using System.Collections.Generic;
using UnityEngine;

namespace HK8YPlando.Scripts.Platforming;
internal enum ZipperState
{
    Rest,
    Shake,
    Shoot,
    Wait,
    Rewind,
    RewindCooldown,
}

[Shim]
internal class Zipper : MonoBehaviour
{
    [ShimField] public List<Sprite> CogSprites = [];
    [ShimField] public float CogFpsFast;
    [ShimField] public float CogFpsSlow;
    [ShimField] public Sprite? RedLightSprite;
    [ShimField] public Sprite? YellowLightSprite;
    [ShimField] public Sprite? GreenLightSprite;

    [ShimField] public ZipperPlatform? Platform;

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
    private float travelDist;
    private float shootTime;
    private float rewindTime;

    private ZipperState state = ZipperState.Rest;
    private float timeProgress = 0;
    private HeroPlatformStickImproved? stick;

    private void Awake()
    {
        stick = Platform!.GetComponent<HeroPlatformStickImproved>()!;
        restPos = RestPosition!.position;
        targetPos = TargetPosition!.position;
        travelDist = (targetPos - restPos).magnitude;
        shootTime = (Mathf.Sqrt(2 * Accel * travelDist + StartSpeed * StartSpeed) - StartSpeed) / Accel;
        rewindTime = travelDist / rewindTime;
    }

    private Sprite ComputeLightSprite()
    {
        return state switch
        {
            ZipperState.Rest => RedLightSprite!,
            ZipperState.Shake or ZipperState.Shoot or ZipperState.Wait => GreenLightSprite!,
            ZipperState.Rewind => YellowLightSprite!,
            ZipperState.RewindCooldown => RedLightSprite!,
            _ => RedLightSprite!,
        };
    }

    private void Update()
    {
        UpdateTime(Time.deltaTime);
        Platform!.Light!.sprite = ComputeLightSprite();
    }

    private void UpdateTime(float time)
    {
        while (time > 0) time = UpdateTimeForState(time);
    }

    private float UpdateTimeForState(float time)
    {
        switch (state)
        {
            case ZipperState.Rest:
                {
                    if (stick!.PlayerAttached)
                    {
                        state = ZipperState.Shake;
                        timeProgress = 0;
                        return time;
                    }

                    return 0;
                }
            case ZipperState.Shake:
                {
                    timeProgress += time;
                    if (timeProgress >= ShakeTime)
                    {
                        Platform!.SpriteShaker!.transform.localPosition = Vector3.zero;
                        var remaining = timeProgress - ShakeTime;

                        state = ZipperState.Shoot;
                        timeProgress = 0;
                        return remaining;
                    }

                    Platform!.SpriteShaker!.transform.localPosition = MathExt.RandomInCircle(Vector2.zero, ShakeRadius);
                    return 0;
                }
            case ZipperState.Shoot:
                {
                    // TODO: Update gears

                    timeProgress += time;
                    if (timeProgress >= shootTime)
                    {
                        Platform!.transform.position = targetPos;
                        var remaining = timeProgress - shootTime;

                        state = ZipperState.Wait;
                        timeProgress = 0;
                        return remaining;
                    }

                    var d = StartSpeed * timeProgress + Accel * timeProgress * timeProgress / 2;
                    Platform!.transform.position = restPos + (targetPos - restPos).normalized * d;
                    return 0;
                }
            case ZipperState.Wait:
                {
                    timeProgress += time;
                    if (timeProgress >= PauseTime)
                    {
                        var remaining = timeProgress - PauseTime;

                        state = ZipperState.Rewind;
                        timeProgress = 0;
                        return remaining;
                    }

                    return 0;
                }
            case ZipperState.Rewind:
                {
                    // TODO: Update gears
                    timeProgress += time;
                    if (timeProgress >= travelDist / RewindSpeed)
                    {
                        Platform!.transform.position = restPos;
                        var remaining = (timeProgress - travelDist / RewindSpeed);

                        state = ZipperState.RewindCooldown;
                        timeProgress = 0;
                        return remaining;
                    }

                    Platform!.transform.position = targetPos + (restPos - targetPos).normalized * RewindSpeed * timeProgress;
                    return 0;
                }
            case ZipperState.RewindCooldown:
                {
                    timeProgress += time;
                    if (timeProgress >= RewindCooldown)
                    {
                        state = ZipperState.Rest;
                        return (timeProgress - RewindCooldown);
                    }

                    return 0;
                }
            default:
                HK8YPlandoMod.BUG($"Unknown state: {state}");
                return 0;
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
