using HK8YPlando.Scripts.SharedLib;
using UnityEngine;

namespace HK8YPlando.Scripts.Framework;

[Shim]
internal class ShadeMarker : MonoBehaviour
{
    private void Awake()
    {
        var obj = Instantiate(ScatteredAndLostPreloader.Instance.ShadeMarker, transform.position, Quaternion.identity);
        obj.SetActive(true);
    }
}
