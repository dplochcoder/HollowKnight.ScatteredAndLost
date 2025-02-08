using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using ItemChanger.Extensions;
using ItemChanger.FsmStateActions;

namespace HK8YPlando.Util;

internal static class FSMExtensions
{
    internal static void SetMinMax(this WaitRandom self, float min, float max)
    {
        self.timeMin.Value = min;
        self.timeMax.Value = max;
    }

    internal static void AlwaysFinishState(this PlayMakerFSM fsm, string state)
    {
        var fsmState = fsm.GetState(state);
        fsmState.ClearActions();
        fsmState.AddLastAction(new Lambda(() => fsm.SendEvent("FINISHED")));
    }
}
