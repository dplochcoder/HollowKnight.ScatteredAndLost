using ItemChanger;
using ItemChanger.Extensions;
using ItemChanger.FsmStateActions;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HK8YPlando.IC;

internal class BlockFungalDrop : ItemChanger.Modules.Module
{
    private static readonly FsmID activatorId = new("Activator", "Activate");

    public override void Initialize()
    {
        Events.AddSceneChangeEdit("Deepnest_01", SpawnMenderBug);
        Events.AddFsmEdit(activatorId, Deactivator);
    }

    public override void Unload()
    {
        Events.RemoveSceneChangeEdit("Deepnest_01", SpawnMenderBug);
        Events.RemoveFsmEdit(activatorId, Deactivator);
    }

    private void SpawnMenderBug(Scene scene)
    {
        var mender = Object.Instantiate(HK8YPlandoPreloader.Instance.MenderBug,
            new Vector3(33.5f, 19, 0),
            Quaternion.identity);

        var fsm = mender.LocateMyFSM("Mender Bug Ctrl");
        var init = fsm.GetState("Init");
        init.ClearActions();
        init.AddLastAction(new Lambda(() => fsm.SetState("Idle")));

        mender.SetActive(true);
    }

    private void Deactivator(PlayMakerFSM fsm) => Object.Destroy(fsm.gameObject);
}
