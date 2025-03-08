using ItemChanger;
using ItemChanger.Locations;
using ItemChanger.Tags;
using RandomizerMod.RandomizerData;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using ItemChanger.Extensions;

namespace HK8YPlando.Rando;

public class SuperSoulTotemLocation : PlaceableLocation
{
    public string? ObjectName;
    public float MapX;
    public float MapY;
    private Scene? loadedScene;

    public override void PlaceContainer(GameObject obj, string containerType)
    {
        var totem = loadedScene?.FindGameObject(ObjectName!) ?? GameObject.Find(ObjectName!)!;
        var pos = totem.transform.position;

        Container.GetContainer(containerType)!.ApplyTargetContext(obj, pos.x, pos.y, 0);
        obj.SetActive(true);
    }

    protected override void OnLoad()
    {
        Events.AddSceneChangeEdit(UnsafeSceneName, PlaceItem);

        var tag = AddTag<InteropTag>();
        tag.Message = "RandoSupplementalMetadata";
        tag.Properties["ModSource"] = nameof(ScatteredAndLostMod);
        tag.Properties["PinSpriteKey"] = PoolNames.Soul;
        tag.Properties["PoolGroup"] = PoolNames.Soul;
        tag.Properties["WorldMapLocations"] = WorldMapLocations().ToArray();

        var riTag = AddTag<InteropTag>();
        riTag.Message = "RecentItems";
        riTag.Properties["DisplaySource"] = "Bretta's House: C-Side";
    }

    private List<(string, float, float)> WorldMapLocations() => [(sceneName!, MapX, MapY)];

    protected override void OnUnload() => Events.RemoveSceneChangeEdit(UnsafeSceneName, PlaceItem);

    private void PlaceItem(Scene scene)
    {
        loadedScene = scene;

        GetContainer(out var obj, out var containerType);
        PlaceContainer(obj, containerType);
    }
}
