using HK8YPlando.Scripts.SharedLib;
using UnityEngine;

namespace HK8YPlando.Scripts
{
    [RequireComponent(typeof(BoxCollider2D))]
    public class NoTerrainZone : SceneDataOptimizer
    {
        public override bool OptimizeScene()
        {
            var box = GetComponent<BoxCollider2D>();
            bool changed = MathExt.Snap(box, 1);

            if (!box.isTrigger)
            {
                box.isTrigger = true;
                changed = true;
            }

            if (gameObject.layer != 13)
            {
                gameObject.layer = 13;
                changed = true;
            }

            return changed;
        }
    }
}
