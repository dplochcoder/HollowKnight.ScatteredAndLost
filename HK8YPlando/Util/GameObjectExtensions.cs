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

    public static void PlaySound(this GameObject self, AudioClip clip) => PlaySoundImpl(self, clip, false);

    public static void LoopSound(this GameObject self, AudioClip clip) => PlaySoundImpl(self, clip, true);

    private static void PlaySoundImpl(this GameObject self, AudioClip clip, bool loop)
    {
        var source = self.GetOrAddComponent<AudioSource>();
        source.outputAudioMixerGroup = AudioMixerGroups.Actors();

        if (loop)
        {
            source.loop = true;
            source.clip = clip;
            source.Play();
        }
        else
        {
            source.loop = false;
            source.PlayOneShot(clip);
        }
    }
}
