using ItemChanger;
using ItemChanger.Locations;
using MoreDoors.Data;
using MoreDoors.IC;

namespace HK8YPlando.MoreDoorsInterop;

internal class MoreDoorsInterop
{
    internal static void AddMinerDoor()
    {
        var crownDoor = DoorData.GetDoor("Crown")!;

        DoorData minersDoor = new()
        {
            CamelCaseName = "Miner",
            UpperCaseName = "MINER",
            UIName = "Miner",
            Door = new()
            {
                Sprite = crownDoor.Door!.Sprite,
                LeftLocation = new()
                {
                    SceneName = "Crossroads_45",
                    GateName = "left1",
                    NoKeyDesc = "A crystallized door echoing of light and song.",
                    KeyDesc = "The purgatorial chaos of transition rando beckons.<br>Insert the Miner's Key?",
                    Masks = [],
                    RequiresLantern = false,
                    X = 1.8f,
                    Y = 10.3f,
                    Decorators = [],
                },
                RightLocation = new()
                {
                    SceneName = "Crossroads_14",
                    GateName = "right2",
                    NoKeyDesc = "A crystallized door echoing of light and song.",
                    KeyDesc = "The call of the mines reigns out.<br>Insert the Miner's Key?",
                    Masks = [],
                    RequiresLantern = false,
                    X = 31.2f,
                    Y = 8.3f,
                    Decorators = [],
                },
                Mode = DoorData.DoorInfo.SplitMode.Normal,
                Deployers = [],
            },
            Key = new()
            {
                ItemName = "MoreDoors-Miner's_Key",
                UIItemName = "Miner's Key",
                ShopDesc = "A precious little soul. Such a shame...",
                InvDesc = "A delicate key filled with spirit. It gleams with a twinkle of orange light.",
                UsedInvDesc = "Damned are those who prey on innocence.",
                Sprite = new IC.EmbeddedSprite("miner_key"),
                Location = new ItemChanger.Locations.DualLocation()
                {
                    Test = new SDBool("Mushroom Roller", "Fungus2_23")
                    {
                        semiPersistent = true,
                    },
                    falseLocation = new ShinyEnemyLocation()
                    {
                        HintShinyScale = 1.5f,
                        HintShinyX = 0,
                        HintShinyY = 0,
                        objectName = "Mushroom Roller",
                        removeGeo = false,
                        forceShiny = false,
                        name = "MoreDoors-Miner's_Key-Mushroom_Roller-Bretta",
                        sceneName = "Fungus2_23",
                        flingType = FlingType.Everywhere,
                    },
                    trueLocation = new CoordinateLocation()
                    {
                        x = 25,
                        y = 6,
                        elevation = 0,
                        managed = false,
                        forceShiny = false,
                        name = "MoreDoors-Miner's_Key-Mushroom_Roller-Bretta",
                        sceneName = "Fungus2_23",
                        flingType = FlingType.Everywhere,
                    },
                    name = "MoreDoors-Miner's_Key-Mushroom_Roller-Bretta",
                    sceneName = "Fungus2_23",
                    flingType = FlingType.Everywhere
                },
                Logic = "Fungus2_23[right1]"
            }
        };

        DoorData.AddExtensionDoor("Miner", minersDoor);
    }
}
