using HK8YPlando.IC;
using ItemChanger;
using ItemChanger.Locations;
using PurenailCore.ICUtil;
using RandomizerCore.Logic;
using RandomizerCore.LogicItems;
using RandomizerMod.RandomizerData;
using RandomizerMod.RC;
using RandomizerMod.Settings;
using System;

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
        lmb.AddItem(new CappedItem(new BrettaHeart(HeartType.Blue).name, [new(term, 1)], new(term, 23)));
        lmb.AddItem(new CappedItem(new BrettaHeart(HeartType.Red).name, [new(term, 1)], new(term, 23)));
        lmb.AddItem(new CappedItem(new BrettaHeart(HeartType.Yellow).name, [new(term, 1)], new(term, 23)));

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
        AddSplitItems("Mothwing_Cloak", lmb);
        AddSplitItems("Queen's_Gardens_Stag", lmb);
        AddSplitItems("Distant_Village_Stag", lmb);
        AddSplitItems("Tram_Pass", lmb);
        AddSplitItems("Swim", lmb);
        AddSplitItems("Isma's_Tear", lmb);

        foreach (var e in MoreDoors.Data.DoorData.All())
        {
            var keyName = e.Value.Key!.ItemName;
            AddSplitItems(keyName, lmb);
        }
    }

    private static void AddSplitItems(string name, RequestBuilder rb)
    {
        var orig = Finder.GetItem(name)!;
        if (orig == null) throw new ArgumentException($"Bad item: '{name}'");

        for (int i = 0; i < 2; i++)
        {
            var sName = $"Scatternest{i}-{name}";

            Finder.DefineCustomItem(new ScatternestRestrictedItem()
            {
                name = sName,
                Wrapped = orig,
                ScatternestIndex = i,
            });
            rb.AddItemByName(sName);
        }
    }

    private static void AddHearts(HeartType heartType, RequestBuilder rb)
    {
        var heart = new BrettaHeart(heartType);
        Finder.DefineCustomItem(heart);

        rb.EditItemRequest(heart.name, info =>
        {
            info.getItemDef = () => new()
            {
                Name = heart.name,
                Pool = PoolNames.Key,
                MajorItem = false,
                PriceCap = 1000,
            };
        });

        for (int i = 0; i < 8; i++) rb.AddItemByName(heart.name);
    }

    private static void ModifyRequest(RequestBuilder rb)
    {
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

        AddHearts(HeartType.Blue, rb);
        AddHearts(HeartType.Red, rb);
        AddHearts(HeartType.Yellow, rb);

        AddSplitItems("Left_Mantis_Claw", rb);
        AddSplitItems("Right_Mantis_Claw", rb);
        AddSplitItems("Mothwing_Cloak", rb);
        AddSplitItems("Queen's_Gardens_Stag", rb);
        AddSplitItems("Distant_Village_Stag", rb);
        AddSplitItems("Tram_Pass", rb);
        AddSplitItems("Swim", rb);
        AddSplitItems("Isma's_Tear", rb);

        foreach (var e in MoreDoors.Data.DoorData.All())
        {
            var keyName = e.Value.Key!.ItemName;
            AddSplitItems(keyName, rb);
        }
    }
}
