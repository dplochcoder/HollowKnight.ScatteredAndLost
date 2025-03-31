using DecorationMaster;
using DecorationMaster.Attr;
using DecorationMaster.MyBehaviour;
using DecorationMaster.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace HK8YPlando.Util;

internal static class DecorationMasterUtil
{
    internal static void RegisterDecoration<B, I>(string name, GameObject prefab, string spriteName)
    {
        ObjectLoader.InstantiableObjects.Add(name, prefab);
        BehaviourProcessor.Register(name, typeof(B), typeof(I));

        Texture2D tex = new(1, 1);
        using Stream stream = typeof(ScatteredAndLostMod).Assembly.GetManifestResourceStream($"HK8YPlando.Resources.Sprites.{spriteName}.png");
        byte[] buffer = new byte[stream.Length];
        stream.Read(buffer, 0, buffer.Length);
        tex.LoadImage(buffer);
        GUIController.Instance.images.Add(name, tex);
    }

    // This should be a public API
    internal static void RefreshItemManager()
    {
        var items = GetDecoMasterPairs();
        
        var group = ItemManager.group;
        group.Clear();
        int idx = 0;
        for (int i = 0; i < items.Count; i += ItemManager.GroupMax) group.Add(++idx, [.. items.GetRange(i, Math.Min(ItemManager.GroupMax, items.Count - i))]);
        ItemManager.Instance.SwitchGroup(0);
    }

    private static List<string> GetDecoMasterPairs() => ObjectLoader.InstantiableObjects.Where(
        x =>
        {
            var cd = x.Value.GetComponent<CustomDecoration>();
            if (cd == null) return false;

            var settings = DecorationMaster.DecorationMaster.instance.Settings;
            if (settings.ProfessorMode) return !cd.GetType().IsDefined(typeof(ObsoleteAttribute), false);
            else if (settings.MemeItem) return !cd.GetType().IsDefined(typeof(AdvanceDecoration), false);
            else return !cd.GetType().IsDefined(typeof(AdvanceDecoration), false) && !cd.GetType().IsDefined(typeof(MemeDecoration), false);
        })
        .Select(x => x.Key)
        .ToList();
}
