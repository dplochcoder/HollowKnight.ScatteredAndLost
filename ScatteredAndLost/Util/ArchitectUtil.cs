using System.Collections.Generic;
using System.Linq;
using Architect.Config;
using Architect.Config.Types;
using Architect.Objects.Placeable;
using UnityEngine;

namespace HK8YPlando.Util;

internal static class ArchitectUtil
{
    internal static PlaceableObject MakeArchitectObject(
        GameObject prefab,
        string name,
        string? img,
        List<ConfigType> configGroup,
        params ConfigType[] extraTypes
    ) =>
        new CustomObject(
            name: name,
            id: $"ScatteredAndLost.{name}",
            prefab: prefab,
            sprite: img != null ? new IC.EmbeddedSprite(img).Value : null,
            uiSprite: img != null ? new IC.EmbeddedSprite(img).Value : null
        ).WithConfigGroup([
            .. configGroup.Concat(extraTypes.Select(ConfigurationManager.RegisterConfigType)),
        ]);

    internal static PlaceableObject MakeArchitectObject(
        string prefab,
        string name,
        string? img,
        List<ConfigType> configGroup,
        params ConfigType[] extraTypes
    ) =>
        MakeArchitectObject(
            ScatteredAndLostSceneManagerAPI.LoadPrefab<GameObject>(prefab),
            name,
            img,
            configGroup,
            extraTypes
        );
}
