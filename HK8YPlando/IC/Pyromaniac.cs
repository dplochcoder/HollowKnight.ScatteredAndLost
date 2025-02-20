using HK8YPlando.Scripts.InternalLib;
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
        var attackChoice = fsm.GetState("Attack Choice");
        attackChoice.ClearActions();

        Wrapped<int> consecutiveAttacks = new(0);
        attackChoice.AddFirstAction(new Lambda(() =>
        {
            if (consecutiveAttacks.Value < 3) fsm.SendEvent(++consecutiveAttacks.Value == 3 ? "FIREBALL" : "SLASH");
        }));
    }
}
