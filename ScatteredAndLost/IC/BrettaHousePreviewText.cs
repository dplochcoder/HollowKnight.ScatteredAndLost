using ItemChanger;
using System.Collections.Generic;

namespace HK8YPlando.IC;

internal class BrettaHousePreviewText : IString
{
    public string Value => BuildText();

    private static string BuildText()
    {
        var mod = BrettasHouse.Get();
        var cost1 = mod.DoorData[0].Total;
        var cost2 = mod.DoorData[1].Total;

        List<string> lines = [];
        MaybePreview("BrettaHouseEntry_Gate1", $"{cost1} Hearts", true, lines);
        MaybePreview("BrettaHouseEntry_Gate2", $"{cost2} Hearts", true, lines);
        MaybePreview("Mask_Shard-Bretta", "Bretta's Mask Shard", false, lines);
        MaybePreview("Boss_Essence-Grey_Prince_Zote", "Grey Prince Zote", false, lines);

        if (lines.Count == 0) lines.Add("You know what's in this house");

        return string.Join("<br>", lines);
    }

    private static void MaybePreview(string location, string label, bool isCostLabel, List<string> lines)
    {
        if (!ItemChanger.Internal.Ref.Settings.Placements.TryGetValue(location, out var placement)) return;

        List<string> itemStrings = [];
        foreach (var item in placement.Items)
        {
            if (item.IsObtained()) itemStrings.Add(Language.Language.Get("OBTAINED", "IC"));
            else itemStrings.Add(item.GetPreviewName());
        }
        if (itemStrings.Count == 0) itemStrings.Add("Nothing?");

        string itemText = string.Join(", ", itemStrings);
        string line = $"{label}: {itemText}";
        lines.Add(line);

        placement.GetOrAddTag<ItemChanger.Tags.PreviewRecordTag>().previewText = isCostLabel ? $"{itemText}  -  {label}" : itemText;
        placement.AddVisitFlag(VisitState.Previewed);
    }

    public IString Clone() => new BrettaHousePreviewText();
}
