using HK8YPlando.Data;
using HK8YPlando.Scripts.Framework;
using HK8YPlando.Scripts.SharedLib;
using HK8YPlando.Util;
using HutongGames.PlayMaker.Actions;
using ItemChanger;
using ItemChanger.Deployers;
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
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HK8YPlando.IC;

internal record HeartDoorData
{
    public int Total = 1;
    public bool Closed = false;
    public int NumUnlocked = 0;
    public bool Opened = false;
}

internal class Dummy : MonoBehaviour { }

internal class BrettasHouse : Module
{
    private static readonly FsmID shadeId = new("Hero Death", "Hero Death Anim");
    private static readonly FsmID dreamNailId = new("Dream Nail");

    public bool EnableHeartDoors;
    public bool EnablePreviews;
    public CheckpointLevel? Checkpoint;
    public bool RandomizeSoulTotems;

    public int Hearts = 0;
    public List<HeartDoorData> DoorData = [new(), new()];
    public bool SeenBrettasHouseAreaTitle = false;
    public bool DefeatedBrettorLords = false;

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
        Events.AddSceneChangeEdit("BrettaHouseEntry", MaybePreviewTablet);
        Events.AddSceneChangeEdit("BrettaHouseZippers", MaybeSkipEntrance);
        Events.AddSceneChangeEdit("Room_Bretta", RedirectBrettaDoorInside);
        Events.AddSceneChangeEdit("Town", RedirectBrettaDoorOutside);
        Events.AddFsmEdit(shadeId, ForceShadeSpawn);
        Events.AddFsmEdit(dreamNailId, EditDreamNail);
        ModHooks.LanguageGetHook += LanguageGetHook;
        ModHooks.GetPlayerBoolHook += GetPlayerBoolHook;
        ModHooks.SetPlayerBoolHook += SetPlayerBoolHook;
        ilHooks.Add(new(typeof(SoulOrb).GetMethod("Zoom", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetStateMachineTarget(), HookSoulOrbZoom));
        ilHooks.Add(new(typeof(FlingObjectsFromGlobalPool).GetMethod("OnEnter", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance), HookFlingObjectsFromGlobalPool));
    }

    public override void Unload()
    {
        if (GetTracker(out var t)) t.OnGenerateFocusDesc -= ShowHeartsInInventory;
        Events.RemoveSceneChangeEdit("BrettaHouseEntry", MaybePreviewTablet);
        Events.RemoveSceneChangeEdit("BrettaHouseZippers", MaybeSkipEntrance);
        Events.RemoveSceneChangeEdit("Room_Bretta", RedirectBrettaDoorInside);
        Events.RemoveSceneChangeEdit("Town", RedirectBrettaDoorOutside);
        Events.RemoveFsmEdit(shadeId, ForceShadeSpawn);
        Events.RemoveFsmEdit(dreamNailId, EditDreamNail);
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

    private void MaybePreviewTablet(Scene scene)
    {
        if (!EnablePreviews || !EnableHeartDoors) return;

        TabletDeployer tablet = new()
        {
            X = 122.5f,
            Y = 3.6f,
            SceneName = "BrettaHouseEntry",
            Text = new BrettaHousePreviewText(),
        };
        tablet.Deploy();
    }

    private void MaybeSkipEntrance(Scene scene)
    {
        if (EnableHeartDoors) return;

        var gate = GameObjectExtensions.FindChild(scene.FindGameObject("_Transition Gates")!, "right1");
        var tp = gate.GetComponent<TransitionPoint>();
        tp.targetScene = "Town";
        tp.entryPoint = "door_bretta";
    }

    private (string, string) GetBrettaDoorTarget()
    {
        if (Checkpoint == null) return (EnableHeartDoors ? "BrettaHouseEntry" : "BrettaHouseZippers", "right1");
        else return Checkpoint.Value.SceneAndGate();
    }

    private void RedirectBrettaDoorOutside(Scene scene)
    {
        var obj = GameObjectExtensions.FindChild(GameObjectExtensions.FindChild(scene.FindGameObject("bretta_house")!, "open")!, "door_bretta")!;

        var vars = obj.LocateMyFSM("Door Control").FsmVariables;
        var (sceneName, gateName) = GetBrettaDoorTarget();
        vars.FindFsmString("New Scene").Value = sceneName;
        vars.FindFsmString("Entry Gate").Value = gateName;

        GameObject spawner = new("fake_transition_spawner");
        spawner.AddComponent<Dummy>().DoAfter(0.25f, () =>
        {
            GameObject t = new("door_bretta");
            t.transform.parent = spawner.transform;

            var tp = t.AddComponent<TransitionPoint>();
            tp.targetScene = sceneName;
            tp.entryPoint = gateName;
        });
    }

    private void RedirectBrettaDoorInside(Scene scene)
    {
        var tp = scene.FindGameObject("right1")!.GetComponent<TransitionPoint>();
        tp.targetScene = "BrettaHouseBubbles";
        tp.entryPoint = "left1";
    }

    private HashSet<BrettaCheckpoint> activeCheckpoints = [];

    internal void UpdateCheckpoint(CheckpointLevel level)
    {
        if (Checkpoint == null || Checkpoint >= level) return;

        // Only update if all prior placements are obtained.
        foreach (var loc in RandomizerData.Locations)
        {
            if (loc.Value.Checkpoint >= level) continue;
            if (ItemChanger.Internal.Ref.Settings.Placements.TryGetValue(loc.Key, out var placement) && !placement.Items.All(i => i.WasEverObtained())) return;
        }

        Checkpoint = level;
    }

    internal void LoadCheckpoint(BrettaCheckpoint checkpointObj)
    {
        activeCheckpoints.Add(checkpointObj);
        UpdateCheckpoint(checkpointObj.Level);
    }

    internal void UnloadCheckpoint(BrettaCheckpoint checkpoint) => activeCheckpoints.Remove(checkpoint);

    internal ShadeSpawnTrigger? lastShadeTrigger;

    internal void ForceShadeSpawn(PlayMakerFSM fsm)
    {
        fsm.GetFsmState("Set Shade").AddFirstAction(new Lambda(() =>
        {
            var marker = lastShadeTrigger?.ShadeMarker;
            if (marker != null)
            {
                var pd = PlayerData.instance;
                pd.SetString(nameof(PlayerData.shadeScene), marker.gameObject.scene.name);
                pd.SetFloat(nameof(PlayerData.shadePositionX), marker.transform.position.x);
                pd.SetFloat(nameof(PlayerData.shadePositionY), marker.transform.position.y);

                fsm.SetState("Check MP");
            }
        }));
    }

    internal void SetShadeSpawnTrigger(ShadeSpawnTrigger trigger) => lastShadeTrigger = trigger;
    internal void ForgetShadeSpawnTrigger(ShadeSpawnTrigger trigger)
    {
        if (lastShadeTrigger == trigger) lastShadeTrigger = null;
    }

    private HashSet<DreamgateFilter> dreamgateFilters = [];

    internal void RegisterDreamgateFilter(DreamgateFilter filter) => dreamgateFilters.Add(filter);
    internal void UnregisterDreamgateFilter(DreamgateFilter filter) => dreamgateFilters.Remove(filter);

    internal void EditDreamNail(PlayMakerFSM fsm) => fsm.GetFsmState("Can Set?")?.AddFirstAction(new Lambda(() =>
    {
        if (dreamgateFilters.Count > 0 && dreamgateFilters.Any(f => !f.AllowDreamgate())) fsm.SendEvent("FAIL");
    }));

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
        cursor.Emit(OpCodes.Ldarg_0);
        cursor.Emit(OpCodes.Ldloc_S, (byte)4);
        cursor.EmitDelegate(MaybeBuffSoulOrb);
    }

    private HashSet<FlingObjectsFromGlobalPool> superSoulOrbFlingers = [];
    internal void RegisterSuperSoulOrbFlinger(FlingObjectsFromGlobalPool action) => superSoulOrbFlingers.Add(action);
    internal void UnregisterSuperSoulOrbFlinger(FlingObjectsFromGlobalPool action) => superSoulOrbFlingers.Remove(action);

    private void MaybeBuffSoulOrb(FlingObjectsFromGlobalPool self, GameObject go)
    {
        if (superSoulOrbFlingers.Contains(self)) BuffSoulOrb(go);
    }

    internal void BuffSoulOrb(GameObject go)
    {
        var orb = go.GetComponent<SoulOrb>();
        if (orb == null) return;

        buffedSoulOrbs.Add(orb);
        GameObjectExtensions.GetOrAddComponent<OnDestroyHook>(go).Action += () => buffedSoulOrbs.Remove(orb);
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
