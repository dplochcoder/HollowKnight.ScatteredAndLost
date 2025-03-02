using HK8YPlando.Data;
using HK8YPlando.IC;
using HK8YPlando.Scripts.SharedLib;
using UnityEngine;

namespace HK8YPlando.Scripts.Framework;

[Shim]
internal class BrettaCheckpoint : MonoBehaviour
{
    [ShimField] public CheckpointLevel Level;

    private void Awake() => BrettasHouse.Get().LoadCheckpoint(this);

    private void OnDestroy() => BrettasHouse.Get().UnloadCheckpoint(this);
}
