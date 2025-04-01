using ItemChanger.Locations;
using ItemChanger.Tags;

namespace HK8YPlando.Rando;

internal class HeartDoorRewardLocation : CoordinateLocation
{
    public float PinX;
    public float PinY;

    protected override void OnLoad()
    {
        base.OnLoad();

        var tag = AddTag<InteropTag>();
        tag.Message = "RandoSupplementalMetadata";
        tag.Properties["ModSource"] = nameof(ScatteredAndLostMod);
        (string, float, float)[] mapLocs = [("Town", PinX, PinY)];
        tag.Properties["MapLocations"] = mapLocs;
        tag.Properties["PinSpriteKey"] = "Skills";

        var riTag = AddTag<InteropTag>();
        riTag.Message = "RecentItems";
        riTag.Properties["DisplaySource"] = "Bretta's House: C-Side";
    }
}
