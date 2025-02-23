using UnityEngine;
using UnityEngine.Audio;

namespace HK8YPlando.Util;

internal static class AudioMixerGroups
{
    // Master, UI, Actors, EnviroEffects, ShadeMixer, Music Options, Atmos, DamageEffects, Sound Options, Music, Music Effects
    private static AudioMixerGroup? _actorsGroup;
    private static AudioMixerGroup? _musicGroup;

    public static AudioMixerGroup Actors()
    {
        if (_musicGroup == null) LoadGroups();
        return _actorsGroup!;
    }

    public static AudioMixerGroup Music()
    {
        if (_musicGroup == null) LoadGroups();
        return _musicGroup!;
    }

    private static void LoadGroups()
    {
        foreach (var mixer in Resources.FindObjectsOfTypeAll<AudioMixer>())
        {
            if (mixer.name == "Actors") _actorsGroup = mixer.outputAudioMixerGroup;
            if (mixer.name == "Music") _musicGroup = mixer.outputAudioMixerGroup;
        }
    }
}
