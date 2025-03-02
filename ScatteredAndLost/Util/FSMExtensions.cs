using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using ItemChanger.Extensions;
using ItemChanger.FsmStateActions;
using SFCore.Utils;
using System.Collections.Generic;
using System.Linq;

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
        var fsmState = fsm.GetFsmState(state);
        fsmState.ClearActions();
        fsmState.AddLastAction(new Lambda(() => fsm.SendEvent("FINISHED")));
    }

    internal static void ForceSetState(this PlayMakerFSM fsm, string state)
    {
        string eventName = $"FORCE_STATE_{state.ToUpper().Replace(' ', '_')}";
        if (fsm.FsmGlobalTransitions.Any(t => t.EventName == eventName)) return;

        fsm.AddFsmGlobalTransitions(eventName, state);
        fsm.SendEvent(eventName);
    }

    internal static void InsertBefore<T>(this FsmState self, FsmStateAction action) where T : FsmStateAction
    {
        List<FsmStateAction> actions = self.Actions.ToList();

        for (int i = 0; i < actions.Count; i++) if (actions[i] is T)
        {
            actions.Insert(i + 1, action);
            self.Actions = actions.ToArray();
            return;
        }

        self.AddLastAction(action);
    }
}
