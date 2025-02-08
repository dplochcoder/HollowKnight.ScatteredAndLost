using HK8YPlando.Scripts.InternalLib;
using HK8YPlando.Util;
using ItemChanger;
using ItemChanger.Extensions;
using ItemChanger.FsmStateActions;

namespace HK8YPlando.IC;

internal class Pyromaniac : ItemChanger.Modules.Module
{
    private static readonly FsmID shadeId = new("Hollow Shade(Clone)", "Shade Control");

    public override void Initialize() => Events.AddFsmEdit(shadeId, AlterShade);

    public override void Unload() => Events.RemoveFsmEdit(shadeId, AlterShade);

    private void AlterShade(PlayMakerFSM fsm)
    {
        fsm.GetState("Sp Check").AddFirstAction(new Lambda(() => fsm.FsmVariables.GetFsmInt("SP").Value = 10));

        var pos = fsm.GetState("Position");
        pos.RemoveTransitionsOn("QUAKE");
        pos.RemoveTransitionsOn("SCREAM");

        fsm.AlwaysFinishState("Quake?");
        fsm.AlwaysFinishState("Scream?");
        fsm.AlwaysFinishState("Q Other?");

        var attackChoice = fsm.GetState("Attack Choice");
        attackChoice.ClearActions();

        Wrapped<int> consecutiveSlashes = new(0);
        attackChoice.AddFirstAction(new Lambda(() =>
        {
            if (++consecutiveSlashes.Value == 3)
            {
                consecutiveSlashes.Value = 0;
                fsm.SendEvent("FIREBALL");
            }
            else fsm.SendEvent("SLASH");
        }));
    }
}
