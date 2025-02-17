using HK8YPlando.Scripts;
using HK8YPlando.Scripts.SharedLib;
using ItemChanger;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HK8YPlando.IC;

internal class BeastsDenSpiders : ItemChanger.Modules.Module
{
    private const string SCENE_NAME = "Deepnest_Spider_Town";

    public override void Initialize() => Events.AddSceneChangeEdit(SCENE_NAME, BuffSpiders);

    public override void Unload() => Events.RemoveSceneChangeEdit(SCENE_NAME, BuffSpiders);

    private void BuffSpiders(Scene scene)
    {
        foreach (var obj in Object.FindObjectsOfType<PlayMakerFSM>())
        {
            if (obj.name.Contains("Spider Mini")) obj.gameObject.GetOrAddComponent<InfiniteHealth>();
        }
    }
}
