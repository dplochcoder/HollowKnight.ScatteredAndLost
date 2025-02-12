using HK8YPlando.IC;
using HK8YPlando.Scripts.InternalLib;
using HK8YPlando.Scripts.Proxy;
using HK8YPlando.Scripts.SharedLib;
using HK8YPlando.Util;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace HK8YPlando.Scripts.Platforming;

[Shim]
internal class HeartDoor : MonoBehaviour
{
    [ShimField] public string? DataKey;

    [ShimField] public int NumHearts;
    [ShimField] public int HeartsPerRow;
    [ShimField] public float HSpace;
    [ShimField] public float VSpace;

    [ShimField] public float FallHeight;
    [ShimField] public float FallSpeed;
    [ShimField] public float FallBuffer;
    [ShimField] public float FallDelay;
    [ShimField] public float HeartActivationDelay;
    [ShimField] public RuntimeAnimatorController? OpenController;
    [ShimField] public List<ParticleSystem> ClosedParticleSystems = [];
    [ShimField] public List<ParticleSystem> OpenParticleSystems = [];

    [ShimField] public List<AudioClip> HeartSounds = [];
    [ShimField] public AudioClip? OpenSound;

    [ShimField] public GameObject? Terrain;
    [ShimField] public GameObject? MainRender;
    [ShimField] public HeroDetectorProxy? ActivationTrigger;

    [ShimField] public GameObject? HeartPrefab;

    private void Awake() => this.StartLibCoroutine(Run());

    private HeartDoorHeart CreateHeart(int index, bool active)
    {
        var row = index / HeartsPerRow;
        var col = index % HeartsPerRow;
        var numRows = (NumHearts + (HeartsPerRow - 1)) / HeartsPerRow;
        var numCols = (row == numRows - 1 && NumHearts % HeartsPerRow != 0) ? (NumHearts % HeartsPerRow) : HeartsPerRow;

        var obj = Instantiate(HeartPrefab)!;
        obj.transform.SetParent(MainRender!.transform);
        obj.transform.localPosition = new(
            HSpace * (col - (numCols - 1) / 2f),
            VSpace * ((numRows - 1) / 2f - row),
            0);

        var h = obj.GetComponent<HeartDoorHeart>();
        h.SetHeartActive(active);
        return h;
    }

    private IEnumerator<CoroutineElement> Run()
    {
        var mod = BrettasHouse.Get();
        var data = mod.DoorData.GetOrAddNew(DataKey!);
        if (data.Opened)
        {
            Terrain?.SetActive(false);
            MainRender?.SetActive(false);
            yield break;
        }

        List<HeartDoorHeart> hearts = [];
        for (int i = 0; i < NumHearts; i++) hearts.Add(CreateHeart(i, data.NumUnlocked > i));

        var knight = HeroController.instance.gameObject;
        if (!data.Closed)
        {
            MainRender!.transform.SetPositionY(transform.position.y + FallHeight);

            var cdashSpeed = 30;
            var limit = transform.position.x + FallBuffer + FallHeight / FallSpeed * cdashSpeed;
            yield return Coroutines.SleepUntil(() => knight.transform.position.x <= limit);

            yield return Coroutines.SleepSecondsUpdatePercent(FallHeight / FallSpeed, pct =>
            {
                MainRender!.transform.SetPositionY(transform.position.y + FallHeight * (1 - pct));
                return false;
            });

            data.Closed = true;
            ClosedParticleSystems.ForEach(p => p.Play());

            yield return Coroutines.SleepSeconds(FallDelay);
        }
        else ClosedParticleSystems.ForEach(p => p.Play());

        yield return Coroutines.SleepUntil(() => ActivationTrigger!.Detected());
        while (data.NumUnlocked < NumHearts)
        {
            yield return Coroutines.SleepUntil(() => mod.Hearts > data.NumUnlocked);

            Wrapped<bool> done = new(false);
            hearts[data.NumUnlocked].StartAnim(() => done.Value = true);

            yield return Coroutines.SleepUntil(() => done.Value);
            data.NumUnlocked++;

            gameObject.PlaySound(HeartSounds.Random());
            yield return Coroutines.SleepSeconds(HeartActivationDelay);
        }

        GetComponent<Animator>().runtimeAnimatorController = OpenController;
        gameObject.PlaySound(OpenSound!);
        yield return Coroutines.SleepUntil(() => doorAnimFinished);

        data.Opened = true;
    }

    private bool doorAnimFinished = false;

    [ShimMethod]
    public void StopDoorParticles() => ClosedParticleSystems.ForEach(p => p.Stop(true, ParticleSystemStopBehavior.StopEmitting));

    [ShimMethod]
    public void DoorOpened()
    {
        doorAnimFinished = true;
        OpenParticleSystems.ForEach(p => p.Stop(true, ParticleSystemStopBehavior.StopEmitting));
    }
}

[Shim]
internal class SpriteOffsetter : MonoBehaviour
{
    [ShimField] public float XSpeed;
    [ShimField] public float XMod;
    [ShimField] public float YSpeed;
    [ShimField] public float YMod;

    private SpriteRenderer? spriteRenderer;
    
    private void Awake() => spriteRenderer = GetComponent<SpriteRenderer>();

    private float offX;
    private float offY;

    private void Update()
    {
        if (spriteRenderer == null) return;

        offX = (offX + XSpeed * Time.deltaTime) % XMod;
        offY = (offY + YSpeed * Time.deltaTime) % YMod;
        spriteRenderer.material.mainTextureOffset = new(offX, offY);
    }
}

[Shim]
[RequireComponent(typeof(SpriteRenderer))]
internal class HeartDoorHeart : MonoBehaviour
{
    [ShimField] public Sprite? EmptySprite;
    [ShimField] public Sprite? FullSprite;
    [ShimField] public RuntimeAnimatorController? HeartAnim;

    private event Action? OnAnimDone;

    private void Awake() => transform.localScale = new(0.85f, 0.85f, 1f);

    internal void SetHeartActive(bool active)
    {
        GetComponent<SpriteRenderer>().sprite = active ? FullSprite : EmptySprite;
    }

    internal void StartAnim(Action onDone)
    {
        OnAnimDone += onDone;
        GetComponent<Animator>().runtimeAnimatorController = HeartAnim;
    }

    [ShimMethod] public void AnimDone() => OnAnimDone?.Invoke();
}
