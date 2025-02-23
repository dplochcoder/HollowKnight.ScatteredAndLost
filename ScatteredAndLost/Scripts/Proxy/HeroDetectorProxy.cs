using HK8YPlando.Scripts.SharedLib;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace HK8YPlando.Scripts.Proxy;

[Shim]
internal class HeroDetectorProxy : MonoBehaviour
{
    private event Action? OnDetectedEvent;
    private event Action? OnUndetectedEvent;

    private HashSet<Collider2D> detected = [];
    private HashSet<Collider2D> ignored = [];
    private bool prevDetected = false;

    public void Ignore(Collider2D collider)
    {
        detected.Remove(collider);
        ignored.Add(collider);
    }

    public bool Detected() => detected.Count > 0;

    private void OnTriggerEnter2D(Collider2D collider)
    {
        if (!ignored.Contains(collider)) detected.Add(collider);
    }

    private void OnTriggerExit2D(Collider2D collider) => detected.Remove(collider);

    public void OnDetected(Action action)
    {
        if (Detected()) action();
        OnDetectedEvent += action;
    }

    public void Listen(Action detect, Action undetect)
    {
        if (Detected()) detect.Invoke();
        else undetect.Invoke();

        OnDetectedEvent += detect;
        OnUndetectedEvent += undetect;
    }

    private void Update()
    {
        bool newDetected = Detected();
        if (newDetected != prevDetected)
        {
            if (newDetected) OnDetectedEvent?.Invoke();
            else OnUndetectedEvent?.Invoke();

            prevDetected = newDetected;
        }
    }
}
