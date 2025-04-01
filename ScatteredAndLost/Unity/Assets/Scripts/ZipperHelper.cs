using HK8YPlando.Scripts.Lib;
using HK8YPlando.Scripts.Platforming;
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
    [ContextMenu("FixTarget")]
#endif
    public void FixTarget()
    {
        var zipper = GetComponent<Zipper>();

        var targetPos = gameObject.FindChild("TargetPos");
        targetPos.transform.position = zipper.TargetPosition.position;
        UnityEditorShims.MarkDirty(targetPos);

        DestroyImmediate(zipper.TargetPosition.gameObject);
        zipper.TargetPosition = targetPos.transform;
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

        var gear1 = gameObject.FindChild("Gear1");
        var pos1 = gameObject.transform.position;
        pos1.z = 0.06f;
        var gear2 = gameObject.FindChild("Gear2");
        var pos2 = gameObject.FindChild("Target").transform.position;
        pos2.z = 0.06f;

        var forward = (pos2 - pos1).normalized * 1.6f;
        pos2 += forward;
        pos1 -= forward;

        changed |= gear1.transform.UpdatePosition(pos1);
        changed |= gear2.transform.UpdatePosition(pos2);
        pos1.z = 0.07f;
        pos2.z = 0.07f;

        var zip1 = gameObject.FindChild("zip1");
        var zip2 = gameObject.FindChild("zip2");
        var mid = (pos1 + pos2) / 2;
        var vec = pos2 - pos1;
        var rot = Mathf.Atan2(vec.y, vec.x) * 180 / Mathf.PI;
        var normal = Quaternion.Euler(0, 0, rot + 90) * new Vector3(0.3f, 0, 0);

        changed |= zip1.transform.UpdatePosition(mid + normal);
        changed |= zip2.transform.UpdatePosition(mid - normal);
        changed |= zip1.transform.UpdateLocalRotation(Quaternion.Euler(0, 0, rot));
        changed |= zip2.transform.UpdateLocalRotation(Quaternion.Euler(0, 0, rot + 180));

        var width = (pos2 - pos1).magnitude / 0.4f;
        changed |= zip1.GetComponent<SpriteRenderer>().UpdateSize(new Vector2(width, 0.4f));
        changed |= zip2.GetComponent<SpriteRenderer>().UpdateSize(new Vector2(width, 0.4f));

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
