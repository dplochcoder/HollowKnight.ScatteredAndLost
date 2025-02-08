using ItemChanger;
using ItemChanger.Extensions;
using ItemChanger.FsmStateActions;
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
