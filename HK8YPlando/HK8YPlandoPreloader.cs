using HutongGames.PlayMaker.Actions;
using ItemChanger.Extensions;
using PurenailCore.ModUtil;
using UnityEngine;

namespace HK8YPlando;

internal class HK8YPlandoPreloader : Preloader
{
    public static HK8YPlandoPreloader Instance { get; } = new();

    [Preload("Fungus2_03", "Area Title Controller")]
    public GameObject AreaTitleController { get; private set; }

    [PrefabPreload("Waterways_08", "Gas Explosion M2")]
    public GameObject BelflyExplosion { get; private set; }

    [Preload("Tutorial_01", "_Props/Hallownest_Main_Gate/Door")]
    public GameObject GreatDoor { get; private set; }

    [Preload("Crossroads_13", "_Enemies/Worm")]
    public GameObject Goam { get; private set; }

    [Preload("GG_Workshop", "GG_Statue_Gorb")]
    public GameObject GorbStatue { get; private set; }

    [Preload("Crossroads_01", "_Scenery/Mender Bug")]
    public GameObject MenderBug { get; private set; }

    [Preload("Room_Colosseum_Silver", "Colosseum Manager/Waves/Wave 30 Obble/Mega Fat Bee")]
    public GameObject Oblobble { get; private set; }

    public AudioClip OblobbleRoar => (AudioClip)Oblobble.LocateMyFSM("Set Rage").GetState("Roar").GetFirstActionOfType<AudioPlayerOneShotSingle>().audioClip.Value;

    [Preload("Town", "_Managers/PlayMaker Unity 2D")]
    public GameObject PlayMaker { get; private set; }

    [Preload("Deepnest_East_08", "Hollow_Shade Marker")]
    public GameObject ShadeMarker { get; private set; }

    [Preload("Tutorial_01", "_Scenery/plat_float_07")]
    public GameObject SmallPlatform { get; private set; }

    [Preload("Fungus2_10", "Soul Totem mini_horned")]
    public GameObject SoulTotem { get; private set; }

    public PhysicsMaterial2D TerrainMaterial => SmallPlatform.GetComponent<Collider2D>().sharedMaterial;

    [Preload("Fungus3_23_boss", "Battle Scene/Wave 3/Mantis Traitor Lord")]
    public GameObject TraitorLord { get; private set; }
}

