using HK8YPlando.Scripts.SharedLib;
using HK8YPlando.Util;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace HK8YPlando.Scripts.Framework;

internal abstract class MusicLib<B, M> : MonoBehaviour, IPersistentBehaviour<B, M> where B : MonoBehaviour, IPersistentBehaviour<B, M> where M : PersistentBehaviourManager<B, M>
{
    [ShimField] public string? BaseFileName;

    private AudioSource? audioSource;

    public void AwakeWithManager(M initManager) => this.StartCoroutine(Run());

    private static List<(string, AudioType)> extensions = [("mp3", AudioType.MPEG), ("ogg", AudioType.OGGVORBIS), ("wav", AudioType.WAV)];

    private (string, AudioType)? GetPath(string suffix)
    {
        var path = Path.Combine(Path.GetDirectoryName(typeof(M).Assembly.Location), "Music", $"{BaseFileName}{suffix}");
        foreach (var (ext, type) in extensions)
        {
            var realPath = $"{path}.{ext}";
            if (File.Exists(realPath)) return (realPath, type);
        }
        return null;
    }

    private UnityWebRequest GetReq(string path, AudioType type) => UnityWebRequestMultimedia.GetAudioClip($"file://{path}", type);

    private IEnumerator Run()
    {
        var intro = GetPath("intro");
        var loop = GetPath("loop");
        if (intro != null && loop != null)
        {
            var (introPath, introType) = intro.Value;
            var (loopPath, loopType) = loop.Value;
            AudioClip? introClip;
            AudioClip? loopClip;
            using (var reqIntro = GetReq(introPath, introType))
            {
                yield return reqIntro.SendWebRequest();
                introClip = DownloadHandlerAudioClip.GetContent(reqIntro);
            }
            using (var reqLoop = GetReq(loopPath, loopType))
            {
                yield return reqLoop.SendWebRequest();
                loopClip = DownloadHandlerAudioClip.GetContent(reqLoop);
            }

            // Play the intro until it's complete, then switch to the loop.
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.loop = false;
            audioSource.clip = introClip!;
            audioSource.bypassEffects = true;
            audioSource.outputAudioMixerGroup = AudioMixerGroups.Music();
            audioSource.Play();

            yield return new WaitUntil(() => !audioSource.isPlaying);
            audioSource.Stop();
            audioSource.clip = loopClip!;
            audioSource.loop = true;
            audioSource.Play();
            yield break;
        }

        var main = GetPath("");
        if (main != null)
        {
            var (path, type) = main.Value;
            AudioClip? clip;
            using (var req = GetReq(path, type))
            {
                yield return req.SendWebRequest();
                clip = DownloadHandlerAudioClip.GetContent(req);
            }

            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.loop = true;
            audioSource.clip = clip!;
            audioSource.bypassEffects = true;
            audioSource.outputAudioMixerGroup = AudioMixerGroups.Music();
            audioSource.Play();
            yield break;
        }

        ScatteredAndLostMod.LogError($"Could not load music for {BaseFileName}");
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