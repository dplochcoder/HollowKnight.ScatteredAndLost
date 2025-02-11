using PurenailCore.ModUtil;
using UnityEngine;

namespace HK8YPlando;

internal class HK8YPlandoPreloader : Preloader
{
    public static HK8YPlandoPreloader Instance { get; } = new();

    [PrefabPreload("Waterways_08", "Gas Explosion M2")]
    public GameObject BelflyExplosion { get; private set; }

    [Preload("Tutorial_01", "_Props/Hallownest_Main_Gate/Door")]
    public GameObject GreatDoor { get; private set; }

    [Preload("Crossroads_13", "_Enemies/Worm")]
    public GameObject Goam { get; private set; }

    [Preload("Crossroads_01", "_Scenery/Mender Bug")]
    public GameObject MenderBug { get; private set; }

    [Preload("Town", "_Managers/PlayMaker Unity 2D")]
    public GameObject PlayMaker { get; private set; }

    [Preload("Deepnest_East_08", "Hollow_Shade Marker")]
    public GameObject ShadeMarker { get; private set; }

    [Preload("Tutorial_01", "_Scenery/plat_float_07")]
    public GameObject SmallPlatform { get; private set; }

    public PhysicsMaterial2D TerrainMaterial => SmallPlatform.GetComponent<Collider2D>().sharedMaterial;
}

