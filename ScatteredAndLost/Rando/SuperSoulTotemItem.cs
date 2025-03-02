using HK8YPlando.IC;
using ItemChanger;
using ItemChanger.Internal;
using ItemChanger.Items;
using UnityEngine;

namespace HK8YPlando.Rando;

internal class SuperSoulTotemItem : SoulTotemItem
{
    public SuperSoulTotemItem()
    {
        name = "C-Side Soul Refill";
        soulTotemSubtype = SoulTotemSubtype.C;
        hitCount = -1;
    }

    private const int FULL_SOUL = 33 * 6;
    private const int FULL_HEAL = 11;

    public override string GetPreferredContainer() => SuperSoulTotemContainer.ContainerName;

    public override void GiveImmediate(GiveInfo info)
    {
        if (info.Container == Container.Totem) return;

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
