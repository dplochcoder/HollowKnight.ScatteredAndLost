using HK8YPlando.IC;
using HK8YPlando.Scripts.SharedLib;
using UnityEngine;

namespace HK8YPlando.Scripts.Framework;

[Shim]
internal class BrettaCheckpoint : MonoBehaviour
{
    [ShimField] public int Priority;
    [ShimField] public string? EntryGate;

    private void Awake() => BrettasHouse.Get().LoadCheckpoint(this);

    private void OnDestroy() => BrettasHouse.Get().UnloadCheckpoint(this);
}
