using ItemChanger.Locations;
using ItemChanger.Tags;
using RandomizerMod.RandomizerData;

namespace HK8YPlando.Rando;

internal class HeartDoorRewardLocation : CoordinateLocation
{
    protected override void OnLoad()
    {
        base.OnLoad();

        var tag = AddTag<InteropTag>();
        tag.Message = "RandoSupplementalMetadata";
        tag.Properties["ModSource"] = nameof(ScatteredAndLostMod);
        tag.Properties["PinSpriteKey"] = PoolNames.Skill;
        tag.Properties["PoolGroup"] = PoolNames.Skill;

        var riTag = AddTag<InteropTag>();
        riTag.Message = "RecentItems";
        riTag.Properties["DisplaySource"] = "Bretta's House: C-Side";
    }
}
