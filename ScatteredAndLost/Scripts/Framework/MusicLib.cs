﻿using HK8YPlando.Scripts.SharedLib;
using HK8YPlando.Util;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace HK8YPlando.Scripts.Framework;

internal abstract class MusicLib<B, M> : MonoBehaviour, IPersistentBehaviour<B, M> where B : MonoBehaviour, IPersistentBehaviour<B, M> where M : PersistentBehaviourManager<B, M>
{
    [ShimField] public string? FileName;

    private AudioSource? audioSource;

    public void AwakeWithManager(M initManager) => this.StartCoroutine(Run());

    private IEnumerator Run()
    {
        var path = Path.Combine(Path.GetDirectoryName(typeof(M).Assembly.Location), "Music", FileName!);
        AudioClip? clip;
        using (UnityWebRequest req = UnityWebRequestMultimedia.GetAudioClip($"file://{path}", AudioType.MPEG))
        {
            yield return req.SendWebRequest();
            clip = DownloadHandlerAudioClip.GetContent(req);
        }

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.loop = true;
        audioSource.clip = clip;
        audioSource.bypassEffects = true;
        audioSource.outputAudioMixerGroup = AudioMixerGroups.Music();

        audioSource.Play();
    }

    public void SceneChanged(M newManager) { }

    public void Stop() => FadeOut(1);

    private float fade = 1;

    private void Update()
    {
        if (audioSource == null) return;
        if (fade < audioSource.volume) audioSource.volume = fade;
    }

    internal void FadeOut(float duration) => this.StartLibCoroutine(FadeOutImpl(duration));

    private IEnumerator<CoroutineElement> FadeOutImpl(float duration)
    {
        yield return Coroutines.OneOf(
            Coroutines.SleepUntil(() => audioSource != null),
            Coroutines.SleepSeconds(5));
        yield return Coroutines.SleepSecondsUpdatePercent(duration, pct =>
        {
            fade = Mathf.Min(fade, 1 - pct);
            return false;
        });
    }
}

[Shim]
internal class SkylinesMusic : MusicLib<SkylinesMusic, SkylinesMusicManager> { }

[Shim]
internal class SkylinesMusicManager : PersistentBehaviourManager<SkylinesMusic, SkylinesMusicManager>
{
    public override SkylinesMusicManager Self() => this;
}

[Shim]
internal class TempleMusic : MusicLib<TempleMusic, TempleMusicManager> { }

[Shim]
internal class TempleMusicManager : PersistentBehaviourManager<TempleMusic, TempleMusicManager>
{
    public override TempleMusicManager Self() => this;
}