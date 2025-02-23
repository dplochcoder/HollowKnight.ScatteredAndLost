using HutongGames.PlayMaker.Actions;
using ItemChanger.Extensions;
using ItemChanger.FsmStateActions;
using UnityEngine;

namespace HK8YPlando.Util;

internal static class MenderbugSpawner
{
    internal static void SpawnMenderbug(Vector2 pos, bool facingRight)
    {
        var mender = Object.Instantiate(ScatteredAndLostPreloader.Instance.MenderBug, pos, Quaternion.identity);
        if (!facingRight) mender.transform.localScale = new(-1, 1, 1);

        var fsm = mender.LocateMyFSM("Mender Bug Ctrl");
        var init = fsm.GetState("Init");
        init.ClearActions();
        init.AddLastAction(new Lambda(() => fsm.SetState("Idle")));

        // Shrink alert range
        mender.FindChild("Hero Detect")!.GetComponent<BoxCollider2D>().size = new(16.23f, 7.4687f);

        if (!facingRight)
        {
            var flyLeft = fsm.GetState("Fly Left");
            flyLeft.AddLastAction(new Lambda(() =>
            {
                var scale = mender.transform.localScale;
                scale.x *= -1;
                mender.transform.localScale = scale;
            }));

            var flyRight = fsm.GetState("Fly Right");
            flyRight.RemoveActionsOfType<SetScale>();
        }

        mender.SetActive(true);
    }
}
