using HK8YPlando.Scripts.Platforming;
using ItemChanger;
using UnityEngine;

namespace HK8YPlando.IC;

internal class BinocularsModule : ItemChanger.Modules.Module
{
    internal Binoculars? ActiveBinoculars;

    internal static BinocularsModule Get() => ItemChangerMod.Modules.Get<BinocularsModule>()!;

    public override void Initialize() => On.CameraController.LateUpdate += OverrideCameraControl;

    public override void Unload() => On.CameraController.LateUpdate -= OverrideCameraControl;

    private Vector3? origCameraPos;

    private void OverrideCameraControl(On.CameraController.orig_LateUpdate orig, CameraController self)
    {
        if (origCameraPos != null)
        {
            self.transform.position = origCameraPos.Value;
            origCameraPos = null;
        }
        orig(self);

        if (ActiveBinoculars != null)
        {
            origCameraPos = self.transform.position;
            self.transform.position = ActiveBinoculars.GetCameraPos();
        }
    }
}
