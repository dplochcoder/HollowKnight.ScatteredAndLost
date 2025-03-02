using ItemChanger;
using ItemChanger.Locations;
using ItemChanger.Tags;
using RandomizerMod.RandomizerData;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HK8YPlando.Rando;

public class SuperSoulTotemLocation : PlaceableLocation
{
    public string? ObjectName;
    public float MapX;
    public float MapY;

    public override void PlaceContainer(GameObject obj, string containerType)
    {
        var totem = GameObject.Find(ObjectName!)!;
        var pos = totem.transform.position;

        Container.GetContainer(containerType)!.ApplyTargetContext(obj, pos.x, pos.y, 0);
        obj.SetActive(true);
    }

    protected override void OnLoad()
    {
        Events.AddSceneChangeEdit(UnsafeSceneName, PlaceItem);

        var tag = GetOrAddTag<InteropTag>();
        tag.Message = "RandoSupplementalMetadata";
        tag.Properties["ModSource"] = nameof(ScatteredAndLostMod);
        tag.Properties["PinSpriteKey"] = PoolNames.Soul;
        tag.Properties["PoolGroup"] = PoolNames.Soul;
        tag.Properties["WorldMapLocations"] = WorldMapLocations().ToArray();
    }

    private List<(string, float, float)> WorldMapLocations() => [(sceneName!, MapX, MapY)];

    protected override void OnUnload() => Events.RemoveSceneChangeEdit(UnsafeSceneName, PlaceItem);

    private void PlaceItem(Scene scene)
    {
        if (managed) return;

        GetContainer(out var obj, out var containerType);
        PlaceContainer(obj, containerType);
    }
}
