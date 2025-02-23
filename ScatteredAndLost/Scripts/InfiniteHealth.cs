using UnityEngine;

namespace HK8YPlando.Scripts;

internal class InfiniteHealth : MonoBehaviour
{
    private HealthManager? healthManager;

    private void Update()
    {
        healthManager ??= GetComponent<HealthManager>();
        if (healthManager == null) return;

        healthManager.hp = 1000;
    }
}
