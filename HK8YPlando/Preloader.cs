using PurenailCore.ModUtil;
using UnityEngine;

namespace HK8YPlando;

internal class HK8YPlandoPreloader : Preloader
{
    public static HK8YPlandoPreloader Instance { get; } = new();

    [Preload("Town", "_Managers/PlayMaker Unity 2D")]
    public GameObject PlayMaker { get; private set; }
}

