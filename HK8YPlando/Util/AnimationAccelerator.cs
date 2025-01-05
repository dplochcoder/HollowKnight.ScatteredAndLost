using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using ItemChanger.Extensions;
using ItemChanger.FsmStateActions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace HK8YPlando.Util;

internal class AnimationAccelerator : MonoBehaviour
{
    private float accel = 1;
    private string? clip;
    private tk2dSpriteAnimator? animator;

    private void Awake() => animator = GetComponent<tk2dSpriteAnimator>();

    internal void AccelerateClip(string name, float a)
    {
        clip = name;
        accel = a;
    }

    private void Update()
    {
        if (animator == null) return;

        if (animator.CurrentClip.name != clip)
        {
            clip = null;
            accel = 1;
        }
        else if (accel > 1) animator.UpdateAnimation(Time.deltaTime * (accel - 1));
    }
}

internal static class AnimationAcceleratorExtensions
{
    internal static void AccelerateAnimation(this FsmState self, AnimationAccelerator accelerator, float accel)
    {
        var clip = self.GetFirstActionOfType<Tk2dPlayAnimationWithEvents>()?.clipName.Value ?? self.GetFirstActionOfType<Tk2dPlayAnimation>().clipName.Value;
        self.AddLastAction(new Lambda(() => accelerator.AccelerateClip(clip, accel)));
    }
}
