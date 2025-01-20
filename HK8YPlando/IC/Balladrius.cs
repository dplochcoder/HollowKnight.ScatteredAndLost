using HK8YPlando.Scripts;
using HK8YPlando.Util;
using HutongGames.PlayMaker.Actions;
using ItemChanger;
using ItemChanger.Extensions;
using ItemChanger.FsmStateActions;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

namespace HK8YPlando.IC;

internal class Balladrius : ItemChanger.Modules.Module
{
    private const string SCENE_NAME = "Crossroads_11_alt";

    public override void Initialize() => Events.AddSceneChangeEdit(SCENE_NAME, BuffBaldur);

    public override void Unload() => Events.RemoveSceneChangeEdit(SCENE_NAME, BuffBaldur);

    private static void BuffBaldur(Scene scene)
    {
        var obj = scene.FindGameObject("_Enemies/Battle Scene/Blocker");
        var fsm = obj.LocateMyFSM("Blocker Control");

        var health = obj.GetComponent<HealthManager>();
        health.hp = 1000;

        List<int> numFires = [0];
        fsm.GetState("Fire").AddFirstAction(new Lambda(() =>
        {
            if (++numFires[0] == 3) ReallyBuffBaldur(fsm);
        }));

        obj.AddComponent<InfiniteHealth>();
    }

    private static void ReallyBuffBaldur(PlayMakerFSM fsm)
    {
        var accel = fsm.gameObject.AddComponent<AnimationAccelerator>();

        fsm.GetState("Close").AccelerateAnimation(accel, 3f);
        fsm.GetState("Close2").AccelerateAnimation(accel, 3f);

        fsm.GetState("Fire").AccelerateAnimation(accel, 3.5f);

        var idleState = fsm.GetState("Idle");
        idleState.AccelerateAnimation(accel, 3f);
        idleState.GetFirstActionOfType<WaitRandom>().SetMinMax(0.075f, 0.1f);

        fsm.GetState("Open").AccelerateAnimation(accel, 3.5f);

        fsm.GetState("Shot Antic").AccelerateAnimation(accel, 3.5f);

        fsm.GetState("Sleep 1").AccelerateAnimation(accel, 3.5f);
        fsm.GetState("Sleep 2").AccelerateAnimation(accel, 3.5f);
    }
}
