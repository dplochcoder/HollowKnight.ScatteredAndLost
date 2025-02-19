using HK8YPlando.Scripts.SharedLib;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HK8YPlando.Scripts.Environment;

// HeroPlatformStick does bad things if deleted while Knight is still on it. This fixes that
[Shim]
[RequireComponent(typeof(Collider2D))]
internal class HeroPlatformStickImproved : MonoBehaviour
{
    private HashSet<GameObject> children = [];

    public bool PlayerAttached => children.Count > 0;

    private void Awake() => On.HeroController.HazardRespawn += SetCooldown;

    private const float COOLDOWN_MAX = 3f;
    private float cooldown = 0;

    private IEnumerator SetCooldown(On.HeroController.orig_HazardRespawn orig, HeroController self)
    {
        cooldown = COOLDOWN_MAX;
        children.ForEach(ExitImpl);
        children.Clear();

        var ret = orig(self);
        return ret;
    }

    private void Update()
    {
        if (cooldown > 0)
        {
            cooldown -= Time.deltaTime;
            if (cooldown < 0) cooldown = 0;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (cooldown > 0) return;

        GameObject collider = collision.gameObject;
        if (collider.name == "Knight")
        {
            collider.transform.SetParent(transform, true);
            children.Add(collider);

            Rigidbody2D rigidbody = collider.GetComponent<Rigidbody2D>();
            if (rigidbody != null) rigidbody.interpolation = RigidbodyInterpolation2D.None;
        }
    }

    private void ExitImpl(GameObject collider)
    {
        collider.transform.SetParent(null);
        DontDestroyOnLoad(collider);

        Rigidbody2D rigidbody = collider.GetComponent<Rigidbody2D>();
        if (rigidbody != null) rigidbody.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        GameObject collider = collision.gameObject;
        if (children.Remove(collider)) ExitImpl(collider);
    }

    private void OnDestroy()
    {
        children.ForEach(ExitImpl);
        children.Clear();

        On.HeroController.HazardRespawn -= SetCooldown;
    }
}