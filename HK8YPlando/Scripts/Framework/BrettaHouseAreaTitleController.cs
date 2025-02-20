using HK8YPlando.IC;
using HK8YPlando.Scripts.SharedLib;
using SFCore.Utils;
using System.Linq;
using UnityEngine;

namespace HK8YPlando.Scripts.Framework;

[Shim]
internal class BrettaHouseAreaTitleController : MonoBehaviour, IPersistentBehaviour<BrettaHouseAreaTitleController, BrettaHouseAreaTitleControllerManager>
{
    public const string AREA_NAME = "BRETTAS_HOUSE";
    public const int AREA_ID = 189234;

    public void AwakeWithManager(BrettaHouseAreaTitleControllerManager initManager)
    {
        var obj = Instantiate(HK8YPlandoPreloader.Instance.AreaTitleController);
        var fsm = obj.LocateMyFSM("Area Title Controller");

        var vars = fsm.FsmVariables;
        vars.GetFsmString("Area Event").Value = AREA_NAME;
        vars.GetFsmBool("Display Right").Value = false;
        vars.GetFsmBool("Sub Area").Value = false;
        vars.GetFsmFloat("Unvisited Pause").Value = 2;
        vars.GetFsmFloat("Visited Pause").Value = 1;
        vars.GetFsmGameObject("Area Title").Value = GameObject.Find("Area Title");

        // Define private Area object
        var areaType = typeof(AreaTitleController).GetNestedType("Area", System.Reflection.BindingFlags.NonPublic);
        var con = areaType.GetConstructor([typeof(string), typeof(int), typeof(bool), typeof(string)]);
        var areaObj = con.Invoke([AREA_NAME, AREA_ID, false, nameof(BrettasHouse.SeenBrettasHouseAreaTitle)]);

        // Add new areas
        var atc = obj.GetComponent<AreaTitleController>();
        var atcList = atc.GetAttr<AreaTitleController, object>("areaList");
        var addMethod = atcList.GetType().GetMethods().Where(mi => mi.Name == "Add" && mi.GetParameters().Length == 1).FirstOrDefault();
        addMethod.Invoke(atcList, [areaObj]);

        obj.SetActive(true);
    }

    public void SceneChanged(BrettaHouseAreaTitleControllerManager newManager) { }

    public void Stop() { }
}

[Shim]
internal class BrettaHouseAreaTitleControllerManager : PersistentBehaviourManager<BrettaHouseAreaTitleController, BrettaHouseAreaTitleControllerManager>
{
    public override BrettaHouseAreaTitleControllerManager Self() => this;
}
