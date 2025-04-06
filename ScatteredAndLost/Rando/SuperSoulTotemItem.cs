using HK8YPlando.Scripts.Framework;
using ItemChanger;
using ItemChanger.Internal;
using ItemChanger.Items;
using ItemChanger.UIDefs;
using PurenailCore.SystemUtil;
using UnityEngine;

namespace HK8YPlando.Rando;

internal class SuperSoulTotemItem : SoulTotemItem
{
    public const string NAME = "C-Side Soul Refill";

    public SuperSoulTotemItem()
    {
        name = NAME;
        soulTotemSubtype = SoulTotemSubtype.C;
        hitCount = -1;
    }

    private const int FULL_SOUL = 33 * 6;
    private const int FULL_HEAL = 11;

    public override void ResolveItem(GiveEventArgs args)
    {
        args.Item = this;

        UIDef = new MsgUIDef()
        {
            name = new BoxedString(NAME),
            shopDesc = new BoxedString("This one's got a little extra juice in it."),
            sprite = new IC.EmbeddedSprite("soul"),
        };
    }

    public override string GetPreferredContainer() => SuperSoulTotemContainer.ContainerName;

    public override bool GiveEarly(string containerType) => base.GiveEarly(containerType) || containerType == SuperSoulTotemContainer.ContainerName;

    public override void GiveImmediate(GiveInfo info)
    {
        if (HeroController.SilentInstance == null || (info.Container != SuperSoulTotemContainer.ContainerName && info.Container != Container.Totem))
        {
            PlayerData.instance.AddMPCharge(FULL_SOUL);
            PlayerData.instance.AddHealth(FULL_HEAL);
        }
        else if (info.FlingType == FlingType.DirectDeposit)
        {
            HeroController.SilentInstance.AddMPCharge(FULL_SOUL);
            HeroController.SilentInstance.AddHealth(FULL_HEAL);
        }
        else
        {
            var prefab = ObjectCache.SoulOrb;
            Object.Destroy(prefab.Spawn());
            prefab.SetActive(true);

            FlingUtils.Config config = new()
            {
                Prefab = prefab,
                AmountMin = 11,
                AmountMax = 11,
                SpeedMin = 10,
                SpeedMax = 20,
                AngleMin = 0,
                AngleMax = 360,
            };

            var objects = FlingUtils.SpawnAndFling(config, info.Transform ?? HeroController.SilentInstance.transform, Vector3.zero);
            objects.ForEach(SuperSoulTotemHooks.BuffSoulOrb);

            prefab.SetActive(false);
        }
    }
}
