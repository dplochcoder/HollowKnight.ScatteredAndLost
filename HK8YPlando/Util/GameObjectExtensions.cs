using HK8YPlando.Scripts.SharedLib;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace HK8YPlando.Util;

internal static class GameObjectExtensions
{
    internal static void StartLibCoroutine(this MonoBehaviour self, CoroutineElement co) => self.StartCoroutine(EvaluateLibCoroutine(co));

    internal static void StartLibCoroutine(this MonoBehaviour self, IEnumerator<CoroutineElement> enumerator) => self.StartCoroutine(EvaluateLibCoroutine(CoroutineSequence.Create(enumerator)));

    private static IEnumerator EvaluateLibCoroutine(CoroutineElement co)
    {
        while (!co.Update(Time.deltaTime).done) yield return 0;
    }

    public static void PlaySound(this GameObject self, AudioClip clip, float volume = 1, bool global = true) => PlaySoundImpl(self, clip, volume, global, false);

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
}
