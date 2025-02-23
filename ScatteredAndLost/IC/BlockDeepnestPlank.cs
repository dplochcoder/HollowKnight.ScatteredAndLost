using HK8YPlando.Scripts.Framework;
using HK8YPlando.Util;
using ItemChanger;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HK8YPlando.IC;

internal class BlockDeepnestPlank : ItemChanger.Modules.Module
{
    private const string SCENE_NAME = "Deepnest_42";

    public override void Initialize() => Events.AddSceneChangeEdit(SCENE_NAME, ReinforcePlank);

    public override void Unload() => Events.RemoveSceneChangeEdit(SCENE_NAME, ReinforcePlank);

    private void ReinforcePlank(Scene scene)
    {
        var plank = GameObject.Find("Plank Solid 1 (2)")!;
        Object.Destroy(plank.GetComponent<Breakable>());
        plank.AddComponent<TinkEffectProxy>();

        MenderbugSpawner.SpawnMenderbug(new(12.5f, 134), true);
    }
}
