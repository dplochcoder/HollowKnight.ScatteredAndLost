using HK8YPlando.Scripts.Lib;
using HK8YPlando.Scripts.SharedLib;
using UnityEngine;

public class ZipperHelper : SceneDataOptimizer
{
    public bool TopSpikes;
    public bool LeftSpikes;
    public bool RightSpikes;
    public bool BotSpikes;

#if UNITY_EDITOR
    [ContextMenu("Load")]
#endif
    public void Load()
    {
        var shaker = gameObject.FindChild("Platform").FindChild("SpriteShaker");

        TopSpikes = shaker.FindChild("SpikesTopSprites").activeSelf;
        LeftSpikes = shaker.FindChild("SpikesLeftSprites").activeSelf;
        RightSpikes = shaker.FindChild("SpikesRightSprites").activeSelf;
        BotSpikes = shaker.FindChild("SpikesBotSprites").activeSelf;

        UnityEditorShims.MarkDirty(this);
    }

#if UNITY_EDITOR
    [ContextMenu("Apply")]
#endif
    public bool Apply()
    {
        bool changed = false;

        var plat = gameObject.FindChild("Platform");
        var shaker = plat.FindChild("SpriteShaker");

        changed |= UpdateChild(TopSpikes, plat, "SpikesTopHitbox");
        changed |= UpdateChild(TopSpikes, shaker, "SpikesTopSprites");
        changed |= UpdateChild(LeftSpikes, plat, "SpikesLeftHitbox");
        changed |= UpdateChild(LeftSpikes, shaker, "SpikesLeftSprites");
        changed |= UpdateChild(RightSpikes, plat, "SpikesRightHitbox");
        changed |= UpdateChild(RightSpikes, shaker, "SpikesRightSprites");
        changed |= UpdateChild(BotSpikes, plat, "SpikesBotHitbox");
        changed |= UpdateChild(BotSpikes, shaker, "SpikesBotSprites");

        return changed;
    }

    public override bool OptimizeScene() => Apply();

    private bool UpdateChild(bool setting, GameObject parent, string name)
    {
        var child = parent.FindChild(name);
        if (child.activeSelf != setting)
        {
            child.SetActive(setting);
            UnityEditorShims.MarkDirty(child);
            return true;
        }

        return false;
    }
}
