using HK8YPlando.Scripts.Framework;
using ItemChanger;
using ItemChanger.Extensions;
using ItemChanger.FsmStateActions;
using ItemChanger.Modules;
using Modding;
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
    private static readonly FsmID shadeId = new("Hero Death", "Hero Death Anim");
    private static readonly FsmID dreamNailId = new("Dream Nail");

    public int Hearts = 0;
    public Dictionary<string, HeartDoorData> DoorData = [];
    public bool AreaTitle = false;

    public string CheckpointScene = "BrettaHouseEntry";
    public string CheckpointGate = "right1";
    public int CheckpointPriority = 0;

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
        Events.AddFsmEdit(dreamNailId, EditDreamNail);
        Events.AddFsmEdit(shadeId, ForceShadeSpawn);
        ModHooks.LanguageGetHook += TraitorLordsHook;
        ModHooks.GetPlayerBoolHook += GetPlayerBoolHook;
    }

    public override void Unload()
    {
        if (GetTracker(out var t)) t.OnGenerateFocusDesc -= ShowHeartsInInventory;
        Events.RemoveSceneChangeEdit("Town", RedirectBrettaDoor);
        Events.RemoveFsmEdit(dreamNailId, EditDreamNail);
        Events.RemoveFsmEdit(shadeId, ForceShadeSpawn);
        ModHooks.LanguageGetHook -= TraitorLordsHook;
        ModHooks.GetPlayerBoolHook -= GetPlayerBoolHook;
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
        var obj = scene.FindGameObject("bretta_house")!.FindChild("open")!.FindChild("door_bretta")!;
        var vars = obj.LocateMyFSM("Door Control").FsmVariables;
        vars.FindFsmString("New Scene").Value = CheckpointScene;
        vars.FindFsmString("Entry Gate").Value = CheckpointGate;
    }

    private HashSet<BrettaCheckpoint> activeCheckpoints = [];

    internal void LoadCheckpoint(BrettaCheckpoint checkpoint)
    {
        activeCheckpoints.Add(checkpoint);

        if (checkpoint.Priority > CheckpointPriority)
        {
            CheckpointPriority = checkpoint.Priority;
            CheckpointGate = checkpoint.EntryGate!;
            CheckpointScene = checkpoint.gameObject.scene.name;
        }
    }

    internal void UnloadCheckpoint(BrettaCheckpoint checkpoint) => activeCheckpoints.Remove(checkpoint);

    internal void EditDreamNail(PlayMakerFSM fsm) => fsm.GetState("Can Set?")?.AddFirstAction(new Lambda(() =>
        {
            if (activeCheckpoints.Count > 0) fsm.SendEvent("FAIL");
        }));

    internal void ForceShadeSpawn(PlayMakerFSM fsm)
    {
        fsm.GetState("Set Shade").AddFirstAction(new Lambda(() =>
        {
            if (activeCheckpoints.Count > 0)
            {
                PlayerData.instance.SetString("shadeScene", "Town");
                PlayerData.instance.SetFloat("shadePositionX", 165);
                PlayerData.instance.SetFloat("shadePositionY", 18);

                fsm.SetState("Check MP");
            }
        }));
    }

    private string TraitorLordsHook(string key, string sheetTitle, string orig)
    {
        return key switch
        {
            "BRETTOR_LORD2_SUPER" => "Revenge of the",
            "BRETTOR_LORD2_MAIN" => "Brettor Lords",
            "BRETTOR_LORD2_SUB" => "Now there are two of them",
            _ => orig
        };
    }
}
