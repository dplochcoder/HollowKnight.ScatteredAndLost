using ItemChanger;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HK8YPlando.IC;

internal enum HeartType
{
    Blue,
    Red,
    Yellow,
}

internal static class HeartTypeExtensions
{
    internal static string ItemName(this HeartType type) => type switch { HeartType.Blue => "BrettaHeart-Blue", HeartType.Red => "BrettaHeart-Red", HeartType.Yellow => "BrettaHeart-Yellow", _ => "" };

    internal static List<HeartType> All() => [HeartType.Blue, HeartType.Red, HeartType.Yellow];
}

internal class BrettaHeart : AbstractItem
{
    public static List<BrettaHeart> All() => HeartTypeExtensions.All().Select(t => new BrettaHeart(t)).ToList();

    public const string TermName = "BRETTA_HEART";

    public HeartType HeartType;
    public string FlavorName = "";

    public BrettaHeart(HeartType heartType, string flavorName = "")
    {
        name = heartType.ItemName();
        HeartType = heartType;
        FlavorName = flavorName;
        UIDef = new HeartUIDef(heartType, FlavorName);
    }

    public BrettaHeart() { }

    public override void GiveImmediate(GiveInfo info) => ItemChangerMod.Modules.Get<BrettasHouse>()!.Hearts++;

    public override bool Redundant() => false;

    public override AbstractItem Clone() => new BrettaHeart(HeartType, FlavorName);
}

internal class HeartUIDef : UIDef
{
    private static EmbeddedSprite BlueSprite = new("heart_blue");
    private static EmbeddedSprite RedSprite = new("heart_red");
    private static EmbeddedSprite YellowSprite = new("heart_yellow");

    public HeartType HeartType;
    public string FlavorName = "";

    public HeartUIDef(HeartType heartType, string flavorName = "")
    {
        HeartType = heartType;
        FlavorName = flavorName;
    }

    public HeartUIDef() { }

    public override string GetPreviewName()
    {
        var mod = ItemChangerMod.Modules.Get<BrettasHouse>()!;
        if (FlavorName.Length == 0) return $"Heart #{mod.Hearts + 1}";
        else return $"{FlavorName} #{mod.Hearts + 1}";
    }

    public override string GetPostviewName()
    {
        var mod = ItemChangerMod.Modules.Get<BrettasHouse>()!;
        if (FlavorName.Length == 0) return $"Heart #{mod.Hearts}";
        else return $"{FlavorName} #{mod.Hearts}";
    }

    public override string? GetShopDesc() => "A relic of challenge and dedication. I think Bretta collects these?";

    public override Sprite GetSprite() => HeartType switch { HeartType.Blue => BlueSprite.Value, HeartType.Red => RedSprite.Value, HeartType.Yellow => YellowSprite.Value, _ => BlueSprite.Value };

    public override void SendMessage(MessageType type, Action? callback)
    {
        if ((type & MessageType.Corner) == MessageType.Corner)
        {
            ItemChanger.Internal.MessageController.Enqueue(GetSprite(), GetPostviewName());
        }

        callback?.Invoke();
    }

    public override UIDef Clone() => new HeartUIDef(HeartType, FlavorName);
}
