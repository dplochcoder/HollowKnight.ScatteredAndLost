using HK8YPlando.IC;
using HK8YPlando.Scripts.InternalLib;
using HK8YPlando.Scripts.Proxy;
using HK8YPlando.Scripts.SharedLib;
using HK8YPlando.Util;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace HK8YPlando.Scripts.Platforming;

internal record HeartSpacing
{
    public int NumHearts;
    public int NumPerRow;
    public float HSpace;
    public float VSpace;

    public HeartSpacing(int numHearts)
    {
        NumHearts = numHearts;

        int? rowSize = null;
        for (int i = 1; i <= 4; i++) if (numHearts <= i * i)
        {
            rowSize = i;
            break;
        }
        rowSize ??= 5;

        NumPerRow = rowSize.Value;
        HSpace = 5f / NumPerRow;
        VSpace = HSpace;
    }

    public Vector2 LocalHeartPos(int idx)
    {
        var row = idx / NumPerRow;
        var col = idx % NumPerRow;
        var numRows = (NumHearts + (NumPerRow - 1)) / NumPerRow;
        var numCols = (row == numRows - 1 && NumHearts % NumPerRow != 0) ? (NumHearts % NumPerRow) : NumPerRow;

        return new(HSpace * (col - (numCols - 1) / 2f), VSpace * ((numRows - 1) / 2f - row));
    }
}

[Shim]
internal class HeartDoor : MonoBehaviour
{
    [ShimField] public int DoorIndex;

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

    private BrettasHouse? mod;
    private HeartSpacing? spacing;

    private void Awake()
    {
        mod = BrettasHouse.Get();
        spacing = new(mod.DoorData[DoorIndex].Total);

        this.StartLibCoroutine(Run());
    }

    private HeartDoorHeart CreateHeart(int index, bool active)
    {
        var obj = Instantiate(HeartPrefab)!;
        obj.transform.SetParent(MainRender!.transform);
        obj.transform.localPosition = spacing!.LocalHeartPos(index);

        var h = obj.GetComponent<HeartDoorHeart>();
        h.SetHeartActive(active);
        return h;
    }

    private IEnumerator<CoroutineElement> Run()
    {
        var mod = BrettasHouse.Get();
        var data = mod.DoorData[DoorIndex];
        if (data.Opened)
        {
            Terrain?.SetActive(false);
            MainRender?.SetActive(false);
            yield break;
        }

        List<HeartDoorHeart> hearts = [];
        for (int i = 0; i < data.Total; i++) hearts.Add(CreateHeart(i, data.NumUnlocked > i));

        var knight = HeroController.instance.gameObject;
        if (!data.Closed)
        {
            MainRender!.transform.SetPositionY(transform.position.y + FallHeight);

            var cdashSpeed = 30;
            var wakeRange = FallBuffer + FallHeight / FallSpeed * cdashSpeed;
            yield return Coroutines.SleepUntil(() => Mathf.Abs(knight.transform.position.x - transform.position.x) <= wakeRange);

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
        while (data.NumUnlocked < data.Total)
        {
            yield return Coroutines.SleepUntil(() => mod.Hearts > data.NumUnlocked);

            gameObject.PlayOneShot(HeartSounds.Random(), 0.8f);

            Wrapped<bool> done = new(false);
            hearts[data.NumUnlocked].StartAnim(() => done.Value = true);

            yield return Coroutines.SleepUntil(() => done.Value);
            data.NumUnlocked++;

            yield return Coroutines.SleepSeconds(HeartActivationDelay);
        }

        GetComponent<Animator>().runtimeAnimatorController = OpenController;
        gameObject.PlaySound(OpenSound!, 0.9f);
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
