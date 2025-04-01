using HK8YPlando.Scripts.Lib;
using HK8YPlando.Scripts.SharedLib;
using UnityEngine;

public class ZipperHelper : SceneDataOptimizer
{
    public bool TopSpikes;
    public bool RightSpikes;
    public bool BotSpikes;
    public bool LeftSpikes;

#if UNITY_EDITOR
    [ContextMenu("Load")]
#endif
    public void Load()
    {
        (TopSpikes, RightSpikes, BotSpikes, LeftSpikes) = ZipperLib.LoadSpikes(gameObject);
        UnityEditorShims.MarkDirty(this);
    }

#if UNITY_EDITOR
    [ContextMenu("Apply")]
#endif
    public bool Apply() => ZipperLib.UpdateZipperAssets(gameObject, TopSpikes, RightSpikes, BotSpikes, LeftSpikes, UnityEditorShims.MarkDirty);

    public override bool OptimizeScene() => Apply();
}
