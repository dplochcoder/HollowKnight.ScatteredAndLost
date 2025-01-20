using HK8YPlando.Scripts.SharedLib;
using UnityEngine;

namespace HK8YPlando.Scripts.Framework;

[Shim]
public class PatchPlayMakerManager : MonoBehaviour
{
    private void Awake()
    {
        var go = Instantiate(HK8YPlandoPreloader.Instance.PlayMaker);
        go.SetActive(true);
        go.name = "PlayMaker Unity 2D";
    }
}
