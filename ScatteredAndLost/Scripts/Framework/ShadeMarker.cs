using HK8YPlando.Scripts.SharedLib;
using UnityEngine;

namespace HK8YPlando.Scripts.Framework;

[Shim]
internal class ShadeMarker : MonoBehaviour
{
    private void Awake()
    {
        Instantiate(ScatteredAndLostPreloader.Instance.ShadeMarker, transform.position, Quaternion.identity);
        Destroy(gameObject);
    }
}
