using HK8YPlando.IC;
using ItemChanger;
using ItemChanger.Locations;
using PurenailCore.ICUtil;
using RandomizerCore.Logic;
using RandomizerCore.LogicItems;
using RandomizerMod.RandomizerData;
using RandomizerMod.RC;
using RandomizerMod.Settings;

namespace HK8YPlando.Rando;

internal static class LogicPatcher
{
    public static void Setup()
    {
        RCData.RuntimeLogicOverride.Subscribe(101f, ModifyLogic);
        RequestBuilder.OnUpdate.Subscribe(101f, ModifyRequest);
    }

    private static void AddSplitItems(string name, LogicManagerBuilder lmb)
    {
        for (int i = 0; i < 2; i++)
        {
            var sName = $"Scatternest{i}-{name}";
            lmb.AddItem(new EmptyItem(sName));
        }
    }

    private static void ModifyLogic(GenerationSettings gs, LogicManagerBuilder lmb)
    {
        // Add hearts
        var term = lmb.GetOrAddTerm(BrettaHeart.TermName);
        lmb.AddItem(new CappedItem(BrettaHeart.ItemName, [new(term, 1)], new(term, 23)));

        // Add bretta locations
        lmb.AddLogicDef(new("BrettaHouse15", $"Rescued_Bretta + {BrettaHeart.TermName} > 14"));
        lmb.AddLogicDef(new("BrettaHouse23", $"Rescued_Bretta + {BrettaHeart.TermName} > 22"));

        Finder.DefineCustomLocation(new CoordinateLocation
        {
            name = "BrettaHouse15",
            sceneName = "BrettaHouseEntry",
            x = 81,
            y = 4,
        });
        Finder.DefineCustomLocation(new CoordinateLocation
        {
            name = "BrettaHouse23",
            sceneName = "BrettaHouseEntry",
            x = 22,
            y = 4,
        });

        AddSplitItems("Left_Mantis_Claw", lmb);
        AddSplitItems("Right_Mantis_Claw", lmb);

        foreach (var e in MoreDoors.Data.DoorData.All())
        {
            var keyName = e.Value.Key!.ItemName;
            AddSplitItems(keyName, lmb);
        }
    }

    private static void AddSplitItems(string name, RequestBuilder rb)
    {
        for (int i = 0; i < 2; i++)
        {
            var sName = $"Scatternest{i}-{name}";
            Finder.DefineCustomItem(new ScatternestRestrictedItem()
            {
                name = sName,
                Wrapped = Finder.GetItem(name),
                ScatternestIndex = i,
            });
            rb.AddItemByName(sName);
        }
    }

    private static void ModifyRequest(RequestBuilder rb)
    {
        rb.EditItemRequest(BrettaHeart.ItemName, info =>
        {
            info.getItemDef = () => new()
            {
                Name = BrettaHeart.ItemName,
                Pool = PoolNames.Key,
                MajorItem = false,
                PriceCap = 1000,
            };
        });

        rb.EditLocationRequest("BrettaHouse15", info =>
        {
            info.getLocationDef = () => new()
            {
                Name = "BrettaHouse15",
                SceneName = "BrettaHouseEntry",
            };
        });
        rb.AddLocationByName("BrettaHouse15");

        rb.EditLocationRequest("BrettaHouse23", info =>
        {
            info.getLocationDef = () => new()
            {
                Name = "BrettaHouse23",
                SceneName = "BrettaHouseEntry",
            };
        });
        rb.AddLocationByName("BrettaHouse23");

        for (int i = 0; i < 24; i++) rb.AddItemByName(BrettaHeart.ItemName);

        AddSplitItems("Left_Mantis_Claw", rb);
        AddSplitItems("Right_Mantis_Claw", rb);

        foreach (var e in MoreDoors.Data.DoorData.All())
        {
            var keyName = e.Value.Key!.ItemName;
            AddSplitItems(keyName, rb);
        }
    }
}
