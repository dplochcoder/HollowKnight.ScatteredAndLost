using HK8YPlando.Util;
using ItemChanger;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HK8YPlando.IC;

internal class FungalActivatorDeleter : MonoBehaviour
{
    private void Update()
    {
        var obj = GameObject.Find("Activator");
        if (obj != null)
        {
            Destroy(obj);
            Destroy(gameObject);
        }
    }
}

internal class BlockFungalDrop : ItemChanger.Modules.Module
{
    public override void Initialize() => Events.AddSceneChangeEdit("Deepnest_01", SpawnMenderBug);

    public override void Unload() => Events.RemoveSceneChangeEdit("Deepnest_01", SpawnMenderBug);

    private void SpawnMenderBug(Scene scene)
    {
        GameObject deleter = new("Deleter");
        deleter.AddComponent<FungalActivatorDeleter>();

        MenderbugSpawner.SpawnMenderbug(new(33.5f, 19), false);
    }

    private void Deactivator(PlayMakerFSM fsm) => Object.Destroy(fsm.gameObject);
}
