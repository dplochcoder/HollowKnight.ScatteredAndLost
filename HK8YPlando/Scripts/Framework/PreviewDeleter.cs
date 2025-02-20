using HK8YPlando.Scripts.SharedLib;
using UnityEngine;

namespace HK8YPlando.Scripts.Framework;

[Shim]
internal class PreviewDeleter : MonoBehaviour
{
    private void Awake() => Destroy(gameObject);
}
