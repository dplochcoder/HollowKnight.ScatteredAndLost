using DecorationMaster.Attr;
using HK8YPlando.Scripts.Platforming;
using System;
using UnityEngine;

namespace HK8YPlando.Scripts.SharedLib
{
    public static class ZipperLib
    {
        public static (bool, bool, bool, bool) LoadSpikes(GameObject zipper)
        {
            var shaker = zipper.FindChild("Platform").FindChild("SpriteShaker");

            bool topSpikes = shaker.FindChild("SpikesTopSprites").activeSelf;
            bool rightSpikes = shaker.FindChild("SpikesRightSprites").activeSelf;
            bool botSpikes = shaker.FindChild("SpikesBotSprites").activeSelf;
            bool leftSpikes = shaker.FindChild("SpikesLeftSprites").activeSelf;

            return (topSpikes, rightSpikes, botSpikes, leftSpikes);
        }

        public static bool UpdateZipperAssets(GameObject zipper, bool topSpikes, bool rightSpikes, bool botSpikes, bool leftSpikes, Action<GameObject> markDirty)
        {
            bool changed = false;

            var plat = zipper.FindChild("Platform");
            var shaker = plat.FindChild("SpriteShaker");

            changed |= UpdateChild(topSpikes, plat, "SpikesTopHitbox", markDirty);
            changed |= UpdateChild(topSpikes, shaker, "SpikesTopSprites", markDirty);
            changed |= UpdateChild(rightSpikes, plat, "SpikesRightHitbox", markDirty);
            changed |= UpdateChild(rightSpikes, shaker, "SpikesRightSprites", markDirty);
            changed |= UpdateChild(botSpikes, plat, "SpikesBotHitbox", markDirty);
            changed |= UpdateChild(botSpikes, shaker, "SpikesBotSprites", markDirty);
            changed |= UpdateChild(leftSpikes, plat, "SpikesLeftHitbox", markDirty);
            changed |= UpdateChild(leftSpikes, shaker, "SpikesLeftSprites", markDirty);

            var gear1 = zipper.FindChild("Gear1");
            var pos1 = zipper.transform.position;
            pos1.z = 0.06f;
            var gear2 = zipper.FindChild("Gear2");
            var pos2 = zipper.GetComponent<Zipper>().TargetPosition.position;
            pos2.z = 0.06f;

            var forward = (pos2 - pos1).normalized * 1.6f;
            pos2 += forward;
            pos1 -= forward;

            changed |= gear1.transform.UpdatePosition(pos1);
            changed |= gear2.transform.UpdatePosition(pos2);
            pos1.z = 0.07f;
            pos2.z = 0.07f;

            var zip1 = zipper.FindChild("zip1");
            var zip2 = zipper.FindChild("zip2");
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

        private static bool UpdateChild(bool setting, GameObject parent, string name, Action<GameObject> markDirty)
        {
            var child = parent.FindChild(name);
            if (child.activeSelf != setting)
            {
                child.SetActive(setting);
                markDirty(child);
                return true;
            }

            return false;
        }

        private static void UpdateZipline(this Zipper self)
        {
            var (top, right, bot, left) = LoadSpikes(self.gameObject);
            UpdateZipperAssets(self.gameObject, top, right, bot, left, _ => { });
        }

        internal static void UpdateTargetPos(this Zipper self, Func<Vector3, Vector3> func)
        {
            self.TargetPosition.localPosition = func(self.TargetPosition.localPosition);
            self.UpdateZipline();
        }

        internal static void SetTopSpikes(this Zipper self, bool value)
        {
            var (_, right, bot, left) = LoadSpikes(self.gameObject);
            UpdateZipperAssets(self.gameObject, value, right, bot, left, _ => { });
        }

        internal static void SetRightSpikes(this Zipper self, bool value)
        {
            var (top, _, bot, left) = LoadSpikes(self.gameObject);
            UpdateZipperAssets(self.gameObject, top, value, bot, left, _ => { });
        }

        internal static void SetBotSpikes(this Zipper self, bool value)
        {
            var (top, right, _, left) = LoadSpikes(self.gameObject);
            UpdateZipperAssets(self.gameObject, top, right, value, left, _ => { });
        }

        internal static void SetLeftSpikes(this Zipper self, bool value)
        {
            var (top, right, bot, _) = LoadSpikes(self.gameObject);
            UpdateZipperAssets(self.gameObject, top, right, bot, value, _ => { });
        }
    }
}
