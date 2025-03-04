using HK8YPlando.IC;
using ItemChanger;
using ItemChanger.Internal;
using ItemChanger.Items;
using ItemChanger.UIDefs;
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

        UIDef = new MsgUIDef()
        {
            name = new BoxedString(NAME),
            shopDesc = new BoxedString("This one's got a little extra juice in it"),
            sprite = new ItemChangerSprite("ShopIcons.Soul"),
        };
    }

    private const int FULL_SOUL = 33 * 6;
    private const int FULL_HEAL = 11;

    public override string GetPreferredContainer() => SuperSoulTotemContainer.ContainerName;

    public override bool GiveEarly(string containerType) => base.GiveEarly(containerType) || containerType == SuperSoulTotemContainer.ContainerName;

    public override void GiveImmediate(GiveInfo info)
    {
        if (info.Container != SuperSoulTotemContainer.ContainerName)
        {
            base.GiveImmediate(info);
            return;
        }

        if (HeroController.SilentInstance == null)
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

            var mod = BrettasHouse.Get();
            foreach (var go in objects) mod.BuffSoulOrb(go);

            prefab.SetActive(false);
        }
    }
}
