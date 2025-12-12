using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace HK8YPlando;

public static class ScatteredAndLostSceneManagerAPI
{
    private static AssetBundle? shared;
    private static readonly Dictionary<string, UnityEngine.Object> prefabs = [];

    static ScatteredAndLostSceneManagerAPI() => Load();

    private static bool loaded = false;
    internal static void Load()
    {
        if (loaded) return;
        loaded = true;

        shared = LoadCoreBundle();
        foreach (var obj in shared.LoadAllAssets()) prefabs[obj.name] = obj;
    }

    public static T LoadPrefab<T>(string name) where T : UnityEngine.Object
    {
        if (prefabs.TryGetValue(name, out var obj) && obj is T typed) return typed;
        throw new ArgumentException($"Unknown Prefab: {name}");
    }

    private const string CORE_BUNDLE_NAME = "scatteredandlostcorebundle";
    private const string CORE_BUNDLE = $"HK8YPlando.Unity.Assets.AssetBundles.{CORE_BUNDLE_NAME}";

    private static AssetBundle LoadCoreBundle()
    {
#if DEBUG
        try
        {
            ScatteredAndLostMod.Log($"Loading {CORE_BUNDLE_NAME} from disk");
            var debugData = PurenailCore.SystemUtil.JsonUtil<ScatteredAndLostMod>.DeserializeEmbedded<Data.DebugData>("HK8YPlando.Resources.Data.debug.json");
            var bundle = AssetBundle.LoadFromFile($"{debugData.LocalAssetBundlesPath}/{CORE_BUNDLE_NAME}");
            ScatteredAndLostMod.Log($"Loading {CORE_BUNDLE_NAME} from disk: success!");
            return bundle;
        }
        catch (Exception e) { ScatteredAndLostMod.BUG($"Failed to load {CORE_BUNDLE_NAME} from local assets: {e}"); }
#endif

        using StreamReader sr = new(typeof(ScatteredAndLostSceneManagerAPI).Assembly.GetManifestResourceStream(CORE_BUNDLE));
        return AssetBundle.LoadFromStream(sr.BaseStream);
    }
}
