using HK8YPlando.IC;
using HK8YPlando.Scripts.Proxy;
using HK8YPlando.Scripts.SharedLib;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HK8YPlando.Scripts.Framework;

[Shim]
internal class DreamgateFilter : MonoBehaviour
{
    private List<HeroDetectorProxy> detectors = [];

    private void Awake()
    {
        detectors = gameObject.FindComponentsRecursive<HeroDetectorProxy>().ToList();
        BrettasHouse.Get().RegisterDreamgateFilter(this);
    }

    private void OnDestroy() => BrettasHouse.Get().UnregisterDreamgateFilter(this);

    internal bool AllowDreamgate() => detectors.Count == 0 || detectors.Any(d => d.Detected());
}
