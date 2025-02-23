using HK8YPlando.Scripts.InternalLib;
using HK8YPlando.Scripts.SharedLib;
using SFCore.Utils;
using UnityEngine;

namespace HK8YPlando.Scripts.Framework;

[Shim]
[RequireComponent(typeof(BoxCollider2D))]
public class TinkEffectProxy : TinkEffect
{
    private static readonly MonobehaviourPatcher<TinkEffect> Patcher = new(
        () => ScatteredAndLostPreloader.Instance.Goam.GetComponent<TinkEffect>(),
        "blockEffect");

    private void Awake()
    {
        Patcher.Patch(this);
        this.SetAttr<TinkEffect, BoxCollider2D>("boxCollider", gameObject.GetComponent<BoxCollider2D>());
    }
}