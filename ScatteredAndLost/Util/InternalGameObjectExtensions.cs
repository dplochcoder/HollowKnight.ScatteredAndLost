using HK8YPlando.Scripts.SharedLib;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HK8YPlando.Util;

internal static class InternalGameObjectExtensions
{
    internal static void StartLibCoroutine(this MonoBehaviour self, CoroutineElement co) => self.StartCoroutine(EvaluateLibCoroutine(co));

    internal static void StartLibCoroutine(this MonoBehaviour self, IEnumerator<CoroutineElement> enumerator) => self.StartCoroutine(EvaluateLibCoroutine(CoroutineSequence.Create(enumerator)));

    private static IEnumerator EvaluateLibCoroutine(CoroutineElement co)
    {
        while (!co.Update(Time.deltaTime).done) yield return 0;
    }

    public static void DoAfter(this MonoBehaviour self, float seconds, Action action) => self.StartLibCoroutine(DoAfterImpl(seconds, action));

    private static IEnumerator<CoroutineElement> DoAfterImpl(float seconds, Action action)
    {
        yield return Coroutines.SleepSeconds(seconds);
        action();
    }

    public static void PlaySound(this GameObject self, AudioClip clip, float volume = 1, bool global = true) => PlaySoundImpl(self, clip, volume, global, false);

    public static void StopSound(this GameObject self) => self.GetComponent<AudioSource>()?.Stop();

    public static void LoopSound(this GameObject self, AudioClip clip, float volume = 1, bool global = true) => PlaySoundImpl(self, clip, volume, global, true);

    private static void PlaySoundImpl(this GameObject self, AudioClip clip, float volume, bool global, bool loop)
    {
        var source = self.GetOrAddComponent<AudioSource>();
        source.outputAudioMixerGroup = AudioMixerGroups.Actors();
        source.minDistance = global ? 1 : 39;
        source.maxDistance = global ? 500 : 50;
        source.rolloffMode = global ? AudioRolloffMode.Logarithmic : AudioRolloffMode.Custom;
        source.reverbZoneMix = global ? 0 : 1;
        source.spatialBlend = global ? 0 : 1;
        source.Stop();

        source.loop = loop;
        source.clip = clip;
        source.volume = volume;
        source.Play();
    }

    public static void PlayOneShot(this GameObject self, AudioClip clip, float volume = 1)
    {
        var source = self.GetOrAddComponent<AudioSource>();
        source.outputAudioMixerGroup = AudioMixerGroups.Actors();
        source.PlayOneShot(clip, volume);
    }
}
