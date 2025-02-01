using HK8YPlando.Scripts.SharedLib;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace HK8YPlando.Scripts.Framework;

[Shim]
public class TilemapPatcher : MonoBehaviour
{
    private void Awake()
    {
        GameObject go = new("TileMap");
        go.tag = "TileMap";
        var newMap = go.AddComponent<tk2dTileMap>();
        var oldMap = gameObject.GetComponent<Tilemap>();
        newMap.width = oldMap.size.x;
        newMap.height = oldMap.size.y;

        gameObject.GetComponent<TilemapRenderer>().material.shader = Shader.Find("Sprites/Default");

        // Patch terrain material.
        var terrainMaterial = HK8YPlandoPreloader.Instance.TerrainMaterial;
        foreach (var collider in gameObject.FindComponentsRecursive<Collider2D>()) collider.sharedMaterial = terrainMaterial;

        GameManager.instance.RefreshTilemapInfo(gameObject.scene.name);

        Destroy(this);
    }
}