using HK8YPlando.Scripts.Framework;
using HK8YPlando.Util;
using ItemChanger;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HK8YPlando.IC;

internal class BlockKPDoor : ItemChanger.Modules.Module
{
    private const string SCENE_LEFT = "Tutorial_01";
    private const string SCENE_RIGHT = "Town";

    public override void Initialize()
    {
        Events.AddSceneChangeEdit(SCENE_LEFT, BlockLeft);
        Events.AddSceneChangeEdit(SCENE_RIGHT, BlockRight);
    }

    public override void Unload()
    {
        Events.RemoveSceneChangeEdit(SCENE_LEFT, BlockLeft);
        Events.RemoveSceneChangeEdit(SCENE_RIGHT, BlockRight);
    }

    private void BlockLeft(Scene scene)
    {
        var door = GameObject.Find("/_Props/Hallownest_Main_Gate/Door")!;
        Object.Destroy(door.LocateMyFSM("Great Door"));
        door.AddComponent<TinkEffectProxy>().useNailPosition = true;

        MenderbugSpawner.SpawnMenderbug(new(188.5f, 63), true);
    }

    private void BlockRight(Scene scene)
    {
        var door = Object.Instantiate(HK8YPlandoPreloader.Instance.GreatDoor, new(3.5f, 49f), Quaternion.identity);
        door.transform.localScale = new(-1, 1, 1);
        Object.Destroy(door.LocateMyFSM("Great Door"));
        door.AddComponent<TinkEffectProxy>().useNailPosition = true;
        door.SetActive(true);

        MenderbugSpawner.SpawnMenderbug(new(6.75f, 44), false);
    }
}
