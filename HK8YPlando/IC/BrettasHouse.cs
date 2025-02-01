using ItemChanger;
using ItemChanger.Extensions;
using ItemChanger.Modules;
using System.Collections.Generic;
using System.Text;
using UnityEngine.SceneManagement;

namespace HK8YPlando.IC;

internal record HeartDoorData
{
    public bool Closed = false;
    public int NumUnlocked = 0;
    public bool Opened = false;
}

internal class BrettasHouse : Module
{
    public int Hearts = 0;
    public Dictionary<string, HeartDoorData> DoorData = [];
    public bool ReachedHouse = false;

    public static BrettasHouse Get() => ItemChangerMod.Modules.Get<BrettasHouse>()!;

    private static bool GetTracker(out InventoryTracker tracker)
    {
        if (ItemChangerMod.Modules.Get<InventoryTracker>() is InventoryTracker t)
        {
            tracker = t;
            return true;
        }

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
        tracker = null;
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
        return false;
    }

    public override void Initialize()
    {
        if (GetTracker(out var t)) t.OnGenerateFocusDesc += ShowHeartsInInventory;
        Events.AddSceneChangeEdit("Town", RedirectBrettaDoor);
    }

    public override void Unload()
    {
        if (GetTracker(out var t)) t.OnGenerateFocusDesc -= ShowHeartsInInventory;
        Events.RemoveSceneChangeEdit("Town", RedirectBrettaDoor);
    }

    private void ShowHeartsInInventory(StringBuilder sb)
    {
        if (Hearts > 0)
        {
            sb.AppendLine();
            sb.Append($"You have collected {Hearts} Heart{(Hearts == 1 ? "" : "s")}");
        }
    }

    private void RedirectBrettaDoor(Scene scene)
    {
        if (ReachedHouse) return;

        var obj = scene.FindGameObject("bretta_house")!.FindChild("open")!.FindChild("door_bretta")!;
        var vars = obj.LocateMyFSM("Door Control").FsmVariables;
        vars.FindFsmString("New Scene").Value = "BrettaHouseEntry";
        vars.FindFsmString("Entry Gate").Value = "right1";
    }
}
