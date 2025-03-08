using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using ItemChanger.Extensions;
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
