using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HK8YPlando;

public class ScatteredAndLostSceneManagerAPI : SceneManagerAPI
{
    internal static readonly ScatteredAndLostSceneManagerAPI Instance = new();

    public static void Load() => overrideAPI = Instance;

    private readonly AssetBundle shared;
    private readonly Dictionary<string, UnityEngine.Object> prefabs = [];
    private readonly Dictionary<string, AssetBundle?> sceneBundles = [];

    private ScatteredAndLostSceneManagerAPI()
    {
        shared = LoadAsset("objectsbundle");

        foreach (var obj in shared.LoadAllAssets()) prefabs[obj.name] = obj;

        foreach (var str in typeof(ScatteredAndLostSceneManagerAPI).Assembly.GetManifestResourceNames())
        {
            if (!str.StartsWith(PREFIX) || str.EndsWith(".manifest") || str.EndsWith(".meta")) continue;
            string name = str.Substring(PREFIX.Length);
            if (name == "AssetBundles" || name == "objectsbundle" || name == "scenes") continue;

            sceneBundles[name] = null;
        }
    }

    public static T LoadPrefab<T>(string name) where T : UnityEngine.Object
    {
        if (Instance.prefabs.TryGetValue(name, out var obj) && obj is T typed) return typed;
        throw new ArgumentException($"Unknown Prefab: {name}");
    }

    private static string AssetBundleName(string sceneName) => sceneName.Replace("_", "").ToLower();

    protected override AsyncOperation LoadSceneAsyncByNameOrIndex(string sceneName, int sceneBuildIndex, LoadSceneParameters parameters, bool mustCompleteNextFrame)
    {
        MaybeLoadScene(sceneName);
        return base.LoadSceneAsyncByNameOrIndex(sceneName, sceneBuildIndex, parameters, mustCompleteNextFrame);
    }

    protected override AsyncOperation UnloadSceneAsyncByNameOrIndex(string sceneName, int sceneBuildIndex, bool immediately, UnloadSceneOptions options, out bool outSuccess)
    {
        MaybeUnloadScene(sceneName);
        return base.UnloadSceneAsyncByNameOrIndex(sceneName, sceneBuildIndex, immediately, options, out outSuccess);
    }

    private const string PREFIX = "HK8YPlando.Unity.Assets.AssetBundles.";

    private AssetBundle LoadAsset(string name)
    {
#if DEBUG
        try
        {
            ScatteredAndLostMod.Log($"Loading {name} from disk");
            var debugData = PurenailCore.SystemUtil.JsonUtil<ScatteredAndLostMod>.DeserializeEmbedded<Data.DebugData>("HK8YPlando.Resources.Data.debug.json");
            var bundle = AssetBundle.LoadFromFile($"{debugData.LocalAssetBundlesPath}/{name}");
            ScatteredAndLostMod.Log($"Loading {name} from disk: success!");
            return bundle;
        }
        catch (Exception e) { ScatteredAndLostMod.BUG($"Failed to load {name} from local assets: {e}"); }
#endif

        using StreamReader sr = new(typeof(ScatteredAndLostSceneManagerAPI).Assembly.GetManifestResourceStream($"{PREFIX}{name}"));
        return AssetBundle.LoadFromStream(sr.BaseStream);
    }

    private void MaybeLoadScene(string sceneName)
    {
        var assetBundleName = AssetBundleName(sceneName);
        if (sceneBundles.ContainsKey(assetBundleName))
        {
#if DEBUG
            sceneBundles[assetBundleName]?.Unload(false);
            sceneBundles[assetBundleName] = LoadAsset(assetBundleName);
#else
            sceneBundles[assetBundleName] ??= LoadAsset(assetBundleName);
#endif
        }
    }

    private void MaybeUnloadScene(string sceneName)
    {
        var assetBundleName = AssetBundleName(sceneName);
        if (sceneBundles.ContainsKey(assetBundleName))
        {
            sceneBundles[assetBundleName]?.Unload(false);
            sceneBundles[assetBundleName] = null;
        }
    }
}
