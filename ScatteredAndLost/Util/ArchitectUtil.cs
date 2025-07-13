using Architect.Attributes.Config;
using Architect.Content.Elements;
using Architect.Content.Groups;
using System.Linq;
using UnityEngine;

namespace HK8YPlando.Util;

internal static class ArchitectUtil
{
    internal static AbstractPackElement MakeArchitectObject(GameObject prefab, string name, string? img, ConfigGroup root, params (ConfigType, string)[] types) => new SimplePackElement(
        prefab, name, "Scattered & Lost", img != null ? new IC.EmbeddedSprite(img).Value : null)
        .WithConfigGroup(new(root, [.. types.Select(p => Architect.Attributes.ConfigManager.RegisterConfigType(p.Item1, p.Item2))]));

    internal static AbstractPackElement MakeArchitectObject(string prefab, string name, string? img, ConfigGroup root, params (ConfigType, string)[] types) => MakeArchitectObject(ScatteredAndLostSceneManagerAPI.LoadPrefab<GameObject>(prefab), name, img, root, types);
}
