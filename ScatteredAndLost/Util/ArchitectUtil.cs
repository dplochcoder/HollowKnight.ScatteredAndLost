using Architect.Attributes.Config;
using Architect.Content.Elements;
using Architect.Content.Groups;
using System.Linq;
using UnityEngine;

namespace HK8YPlando.Util;

internal static class ArchitectUtil
{
    private static ConfigGroup GetConfigGroup(string name)
    {
        typeof(ConfigGroup).GetMethod("Initialize", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static).Invoke(null, []);
        return (typeof(ConfigGroup).GetField(name, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static).GetValue(null) as ConfigGroup)!;
    }

    // TODO: Make ConfigGroups accessible.
    internal static readonly ConfigGroup Generic = GetConfigGroup("Generic");
    internal static readonly ConfigGroup Stretchable = GetConfigGroup("Stretchable");

    internal static AbstractPackElement MakeArchitectObject(GameObject prefab, string name, string img, ConfigGroup root, params ConfigType[] types) => new SimplePackElement(
        prefab, name, "Scattered & Lost", new IC.EmbeddedSprite(img).Value)
        .WithConfigGroup(new(Generic, [.. types.Select(Architect.Attributes.ConfigManager.RegisterConfigType)]));

    internal static AbstractPackElement MakeArchitectObject(string prefab, string name, string img, ConfigGroup root, params ConfigType[] types) => MakeArchitectObject(ScatteredAndLostSceneManagerAPI.LoadPrefab<GameObject>(prefab), name, img, root, types);
}
