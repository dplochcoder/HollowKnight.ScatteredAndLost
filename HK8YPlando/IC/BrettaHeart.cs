using ItemChanger;
using System;
using UnityEngine;

namespace HK8YPlando.IC;

internal class BrettaHeart : AbstractItem
{
    public const int MAX_HEARTS = 23;

    public const string ItemName = "BrettaHeart";

    public const string TermName = "BRETTA_HEART";

    public BrettaHeart()
    {
        name = ItemName;
        UIDef = new HeartUIDef();
    }

    public override void GiveImmediate(GiveInfo info) => ItemChangerMod.Modules.Get<BrettasHouse>()!.Hearts++;

    public override bool Redundant() => ItemChangerMod.Modules.Get<BrettasHouse>()!.Hearts >= MAX_HEARTS;

    public override AbstractItem Clone() => new BrettaHeart();
}

internal class HeartUIDef : UIDef
{
    private static EmbeddedSprite Sprite = new("heart");

    public override string GetPreviewName()
    {
        var mod = ItemChangerMod.Modules.Get<BrettasHouse>()!;
        return $"Heart #{mod.Hearts + 1}";
    }

    public override string GetPostviewName()
    {
        var mod = ItemChangerMod.Modules.Get<BrettasHouse>()!;
        return $"Heart #{mod.Hearts}";
    }

    public override string? GetShopDesc() => "A relic of challenge and dedication. I think Bretta collects these?";

    public override Sprite GetSprite() => Sprite.Value;

    public override void SendMessage(MessageType type, Action? callback)
    {
        if ((type & MessageType.Corner) == MessageType.Corner)
        {
            ItemChanger.Internal.MessageController.Enqueue(GetSprite(), GetPostviewName());
        }

        callback?.Invoke();
    }

    public override UIDef Clone() => new HeartUIDef();
}
