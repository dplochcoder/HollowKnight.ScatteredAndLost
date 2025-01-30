using GlobalEnums;
using HK8YPlando.Scripts.SharedLib;
using Modding;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HK8YPlando.Scripts.Platforming;

[Shim]
internal class CoinGroup : MonoBehaviour
{
    private List<CoinDoor> doors = [];
    private List<Coin> coins = [];

    private void Awake()
    {
        doors = gameObject.FindComponentsRecursive<CoinDoor>().ToList();
        coins = gameObject.FindComponentsRecursive<Coin>().ToList();

        ModHooks.TakeDamageHook += OnTakeDamage;
    }

    private void OnDestroy() => ModHooks.TakeDamageHook -= OnTakeDamage;

    private int OnTakeDamage(ref int hazardType, int damage)
    {
        if (damage > 0 && hazardType == (int)HazardType.SPIKES + 1)
        {
            coins.ForEach(c => c.Deactivate());
            doors.ForEach(d => d.Close());
            doorsOpened = false;
        }

        return damage;
    }

    private bool doorsOpened = false;

    private void Update()
    {
        if (doorsOpened) return;
        if (coins.Any(c => !c.IsActivated())) return;

        doors.ForEach(d => d.Open());
        doorsOpened = true;
    }
}

[Shim]
internal class Coin : MonoBehaviour, IHitResponder
{
    // TODO: Particles
    private static readonly Color IdleColor = new(0.3f, 0.8f, 1f);
    private static readonly Color FlashColor = Color.white;
    private static readonly Color ActiveColor = new(1f, 0.3f, 0.7f);

    private const float COOLDOWN_TIME = 1f;

    private float cooldown;
    private bool activated;

    internal bool IsActivated() => activated;

    internal void Deactivate()
    {
        activated = false;
        cooldown = COOLDOWN_TIME;
        // TODO: Animation
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.layer == (int)PhysLayers.PLAYER) MaybeHit();
    }

    public void Hit(HitInstance damageInstance)
    {
        if (damageInstance.AttackType == AttackTypes.Nail) MaybeHit();
    }

    private void MaybeHit()
    {
        if (activated || cooldown > 0) return;

        activated = true;
        // TODO: Animation
    }
}

internal class CoinDoor : MonoBehaviour
{
    internal void Open()
    {

    }

    internal void Close()
    {

    }
}
