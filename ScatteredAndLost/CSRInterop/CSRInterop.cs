using System;
using System.Collections.Generic;
using ConnectionSettingsRando;

namespace HK8YPlando.CSRInterop;

internal static class CSRInterop
{
    internal static void Setup()
    {
        CSR.Register(
            "ScatteredAndLost",
            rng =>
            {
                var (settings, stats) = RandomizeSettings(rng);
                Rando.ConnectionMenu.Instance?.ApplySettings(settings);
                return stats;
            }
        );
    }

    private static (RandomizerSettings, RandomizationStats) RandomizeSettings(System.Random rng)
    {
        SettingsRandomizer randomizer = new();
        var (settings, stats) = randomizer.Randomize(
            ScatteredAndLostMod.Settings.RandomizerSettings,
            rng,
            "ScatteredAndLost"
        );

        IReadOnlyList<string> path = ["ScatteredAndLost"];
        if (
            SettingsRandomizer.Skip(settings.MinHearts.GetType(), nameof(settings.MinHearts), path)
            || SettingsRandomizer.Skip(
                settings.MaxHearts.GetType(),
                nameof(settings.MaxHearts),
                path
            )
        )
        {
            SettingsRandomizer.TrackSkip(nameof(settings.MinHearts), path, stats);
            SettingsRandomizer.TrackSkip(nameof(settings.MaxHearts), path, stats);
        }
        else
        {
            SettingsRandomizer.TrackRando(nameof(settings.MinHearts), path, stats);
            SettingsRandomizer.TrackRando(nameof(settings.MaxHearts), path, stats);

            var a = rng.Next(1, RandomizerSettings.MAX_HEART_REQUIREMENT + 1);
            var b = rng.Next(1, RandomizerSettings.MAX_HEART_REQUIREMENT + 1);
            settings.MinHearts = Math.Min(a, b);
            settings.MaxHearts = Math.Max(a, b);
        }

        return (settings, stats);
    }
}
