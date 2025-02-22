using HK8YPlando.Scripts.Framework;
using HK8YPlando.Scripts.SharedLib;
using HutongGames.PlayMaker.Actions;
using ItemChanger;
using ItemChanger.Extensions;
using ItemChanger.FsmStateActions;
using ItemChanger.Modules;
using Modding;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;
using SFCore.Utils;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
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
    public bool SeenBrettasHouseAreaTitle = false;

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

    private List<ILHook> ilHooks = [];

    public override void Initialize()
    {
        if (GetTracker(out var t)) t.OnGenerateFocusDesc += ShowHeartsInInventory;
        Events.AddSceneChangeEdit("Town", RedirectBrettaDoor);
        Events.AddSceneChangeEdit("Room_Bretta", GodhomeTransition);
        Events.AddFsmEdit(dreamNailId, EditDreamNail);
        Events.AddFsmEdit(shadeId, ForceShadeSpawn);
        ModHooks.LanguageGetHook += LanguageGetHook;
        ModHooks.GetPlayerBoolHook += GetPlayerBoolHook;
        ModHooks.SetPlayerBoolHook += SetPlayerBoolHook;
        ilHooks.Add(new(typeof(SoulOrb).GetMethod("Zoom", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetStateMachineTarget(), HookSoulOrbZoom));
        ilHooks.Add(new(typeof(FlingObjectsFromGlobalPool).GetMethod("OnEnter", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance), HookFlingObjectsFromGlobalPool));
    }

    public override void Unload()
    {
        if (GetTracker(out var t)) t.OnGenerateFocusDesc -= ShowHeartsInInventory;
        Events.RemoveSceneChangeEdit("Town", RedirectBrettaDoor);
        Events.RemoveSceneChangeEdit("Room_Bretta", GodhomeTransition);
        Events.RemoveFsmEdit(dreamNailId, EditDreamNail);
        Events.RemoveFsmEdit(shadeId, ForceShadeSpawn);
        ModHooks.LanguageGetHook -= LanguageGetHook;
        ModHooks.GetPlayerBoolHook -= GetPlayerBoolHook;
        ModHooks.SetPlayerBoolHook -= SetPlayerBoolHook;
        ilHooks.ForEach(h => h.Dispose());
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
        var obj = GameObjectExtensions.FindChild(GameObjectExtensions.FindChild(scene.FindGameObject("bretta_house")!, "open")!, "door_bretta")!;
        var vars = obj.LocateMyFSM("Door Control").FsmVariables;
        vars.FindFsmString("New Scene").Value = CheckpointScene;
        vars.FindFsmString("Entry Gate").Value = CheckpointGate;
    }

    internal static bool godhomeTransition = false;

    private void GodhomeTransition(Scene scene)
    {
        if (!godhomeTransition) return;
        godhomeTransition = false;

        var ggBattleTransitions = GameObjectExtensions.FindChild(HK8YPlandoPreloader.Instance.GorbStatue, "Inspect")
            .LocateMyFSM("GG Boss UI").GetFsmState("Transition").GetFirstActionOfType<CreateObject>().gameObject.Value;

        var transitions = Object.Instantiate(ggBattleTransitions);
        transitions.SetActive(true);
        transitions.LocateMyFSM("Transitions").SendEvent("GG TRANSITION IN");
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

    internal void EditDreamNail(PlayMakerFSM fsm) => fsm.GetFsmState("Can Set?")?.AddFirstAction(new Lambda(() =>
        {
            if (activeCheckpoints.Count > 0) fsm.SendEvent("FAIL");
        }));

    internal void ForceShadeSpawn(PlayMakerFSM fsm)
    {
        fsm.GetFsmState("Set Shade").AddFirstAction(new Lambda(() =>
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

    private string LanguageGetHook(string key, string sheetTitle, string orig)
    {
        return key switch
        {
            "BRETTOR_LORD2_SUPER" => "Revenge of the",
            "BRETTOR_LORD2_MAIN" => "Brettor Lords",
            "BRETTOR_LORD2_SUB" => "",
            $"{BrettaHouseAreaTitleController.AREA_NAME}_SUPER" => "",
            $"{BrettaHouseAreaTitleController.AREA_NAME}_MAIN" => "Bretta's House",
            $"{BrettaHouseAreaTitleController.AREA_NAME}_SUB" => "C-Side",
            _ => orig
        };
    }

    private bool GetPlayerBoolHook(string name, bool orig)
    {
        return name switch
        {
            nameof(SeenBrettasHouseAreaTitle) => SeenBrettasHouseAreaTitle,
            _ => orig
        };
    }

    private bool SetPlayerBoolHook(string name, bool value)
    {
        return name switch
        {
            nameof(SeenBrettasHouseAreaTitle) => (SeenBrettasHouseAreaTitle = value),
            _ => value,
        };
    }

    private void HookFlingObjectsFromGlobalPool(ILContext il)
    {
        ILCursor cursor = new(il);
        cursor.Goto(0);
        cursor.GotoNext(i => i.MatchCall<RigidBody2dActionBase>("CacheRigidBody2d"));
        cursor.EmitDelegate(MaybeBuffSoulOrb);
        cursor.Emit(OpCodes.Ldarg_0);
        cursor.Emit(OpCodes.Ldloc_S, 4);
    }

    private HashSet<FlingObjectsFromGlobalPool> superSoulOrbFlingers = [];
    internal void RegisterSuperSoulOrbFlinger(FlingObjectsFromGlobalPool action) => superSoulOrbFlingers.Add(action);
    internal void UnregisterSuperSoulOrbFlinger(FlingObjectsFromGlobalPool action) => superSoulOrbFlingers.Remove(action);

    private void MaybeBuffSoulOrb(FlingObjectsFromGlobalPool self, GameObject go)
    {
        if (!superSoulOrbFlingers.Contains(self)) return;

        var orb = go.GetComponent<SoulOrb>();
        if (orb == null) return;

        buffedSoulOrbs.Add(orb);
        go.AddComponent<OnDestroyHook>().Action = () => buffedSoulOrbs.Remove(orb);
    }

    private void HookSoulOrbZoom(ILContext il)
    {
        ILCursor cursor = new(il);
        cursor.Goto(0);
        cursor.GotoNext(i => i.MatchCallvirt<HeroController>("AddMPCharge"));
        cursor.Emit(OpCodes.Ldloc_1);
        cursor.EmitDelegate(MaybeSuperHeal);
    }

    private HashSet<SoulOrb> buffedSoulOrbs = [];

    private void MaybeSuperHeal(SoulOrb orb)
    {
        if (buffedSoulOrbs.Remove(orb))
        {
            HeroController.instance.AddMPCharge(16);  // (16 + 2) * 11 = 198 = max MP
            HeroController.instance.AddHealth(1);  // 1 * 11 = max health
        }
    }
}
