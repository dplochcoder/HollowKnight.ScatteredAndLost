using GlobalEnums;
using ItemChanger;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HK8YPlando.IC;

internal class ArchivesSkipModule : ItemChanger.Modules.Module
{
    private const string SCENE_NAME = "Fungus3_archive_02";

    public override void Initialize() => Events.AddSceneChangeEdit(SCENE_NAME, AddRespawn);

    public override void Unload() => Events.RemoveSceneChangeEdit(SCENE_NAME, AddRespawn);

    private const float X_MIN = 31;
    private const float X_MAX = 39;
    private const float Y_MIN = 73;
    private const float Y_MAX = 77;
    private const float RESPAWN_X = 31;

    private void AddRespawn(Scene scene)
    {
        var markerObj = new GameObject("SkipMarker");
        markerObj.transform.position = new(RESPAWN_X, (Y_MIN + Y_MAX) / 2);
        var marker = markerObj.AddComponent<HazardRespawnMarker>();
        marker.respawnFacingRight = true;

        var hrt = new GameObject("SkipTrigger");
        hrt.transform.position = new((X_MIN + X_MAX) / 2, (Y_MIN + Y_MAX) / 2);
        hrt.layer = (int)PhysLayers.HERO_DETECTOR;

        var collider = hrt.AddComponent<BoxCollider2D>();
        collider.size = new(X_MAX - X_MIN, Y_MAX - Y_MIN);
        collider.isTrigger = true;

        hrt.AddComponent<HazardRespawnTrigger>().respawnMarker = marker;
    }
}
