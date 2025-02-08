using ItemChanger;
using ItemChanger.Deployers;
using ItemChanger.Extensions;
using UnityEngine;

namespace HK8YPlando.IC;

internal class BlockPeaksToll : ItemChanger.Modules.Module
{
    public override void Initialize() => Events.AddSceneChangeEdit("Mines_33", BlockToll);

    public override void Unload() => Events.RemoveSceneChangeEdit("Mines_33", BlockToll);

    private void BlockToll(UnityEngine.SceneManagement.Scene scene)
    {
        Object.Destroy(scene.FindGameObject("Toll Gate Machine"));
        Object.Destroy(scene.FindGameObject("Toll Gate Machine (1)"));

        TabletDeployer d = new()
        {
            X = 30,
            Y = 12.65f,
            Text = new BoxedString("The Peaks Geo Toll is out of service.<br>Please use EZPass in the other lane."),
        };
        d.Deploy();
    }
}
