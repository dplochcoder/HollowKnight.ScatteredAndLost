using HK8YPlando.Scripts.SharedLib;
using PurenailCore.ModUtil;
using System;
using UnityEngine;
using static ItemChanger.Internal.SpriteManager;

namespace HK8YPlando.Scripts.Framework;

internal class AreaTitleController : MonoBehaviour, IPersistentBehaviour<AreaTitleController, AreaTitleControllerManager>
{
    public void Awake(AreaTitleControllerManager initManager)
    {
        var obj = Instantiate(HK8YPlandoPreloader.Instance.AreaTitleController);
        var fsm = obj.LocateMyFSM("Area Title Controller");

        var vars = fsm.FsmVariables;
        vars.GetFsmString("Area Event").Value = info.AreaName;
        vars.GetFsmBool("Display Right").Value = false;
        vars.GetFsmBool("Sub Area").Value = false;
        vars.GetFsmFloat("Unvisited Pause").Value = 2;
        vars.GetFsmFloat("Visited Pause").Value = 1;
        vars.GetFsmGameObject("Area Title").Value = GameObject.Find("Area Title");

        // Define private Area object
        var areaType = typeof(AreaTitleController).GetNestedType("Area", System.Reflection.BindingFlags.NonPublic);
        var con = areaType.GetConstructor(new Type[] { typeof(string), typeof(int), typeof(bool), typeof(string) });
        var areaObj = con.Invoke(new object[] { area.Name, area.Id, area.SubArea, area.ShowFullTitle ? area.BoolName : "" });

        // Add new areas
        var atc = obj.GetComponent<AreaTitleController>();
        var atcList = atc.GetAttr<AreaTitleController, object>("areaList");
        var addMethod = atcList.GetType().GetMethods().Where(mi => mi.Name == "Add" && mi.GetParameters().Length == 1).FirstOrDefault();
        addMethod.Invoke(atcList, new object[] { areaObj });

        obj.AddComponent<NonBouncer>();
        obj.SetActive(true);
    }

    public void SceneChanged(AreaTitleControllerManager newManager) { }

    public void Stop() { }
}

[Shim]
internal class AreaTitleControllerManager : PersistentBehaviourManager<AreaTitleController, AreaTitleControllerManager>
{
    public override AreaTitleControllerManager Self() => this;
}
