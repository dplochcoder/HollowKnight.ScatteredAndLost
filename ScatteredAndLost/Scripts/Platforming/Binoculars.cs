using HK8YPlando.IC;
using HK8YPlando.Scripts.InternalLib;
using HK8YPlando.Scripts.Proxy;
using HK8YPlando.Scripts.SharedLib;
using HK8YPlando.Util;
using HutongGames.PlayMaker.Actions;
using ItemChanger.Extensions;
using SFCore.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HK8YPlando.Scripts.Platforming;

[Shim]
internal class Binoculars : MonoBehaviour
{
    [ShimField] public GameObject? HudPrefab;

    [ShimField] public HeroDetectorProxy? Detector;
    [ShimField] public GameObject? CollidersParent;
    [ShimField] public Transform? CameraStart;
    [ShimField] public float CameraSpeed;

    private BinocularsModule? module;
    private List<BoxCollider2D> validRanges = [];
    private PlayMakerFSM? promptMarker;

    private void Awake()
    {
        module = BinocularsModule.Get();
        validRanges = CollidersParent!.FindComponentsRecursive<BoxCollider2D>().ToList();

        var promptMarkerObj = Instantiate(ScatteredAndLostPreloader.Instance.KingsPassLoreTablet.LocateMyFSM("Inspection").GetFsmState("Init").GetFirstActionOfType<SpawnObjectFromGlobalPool>().gameObject.Value);
        promptMarkerObj.transform.SetParent(transform);
        promptMarkerObj.transform.localPosition = new(0, 1.5f, 0);
        promptMarker = promptMarkerObj.LocateMyFSM("Prompt Control");
        promptMarker.FsmVariables.GetFsmString("Prompt Name").Value = "Inspect";

        this.StartLibCoroutine(Run());
    }

    private bool CanInspect()
    {
        if (!Detector!.Detected()) return false;

        var cState = HeroController.instance.cState;
        if (cState.attacking || cState.downAttacking || cState.upAttacking || cState.dashing) return false;
        if (!cState.onGround) return false;

        return true;
    }

    private IEnumerator<CoroutineElement> Run()
    {
        var heroController = HeroController.instance;
        var vignette = GameObjectExtensions.FindChild(heroController.gameObject, "Vignette");
        var animator = heroController.gameObject.GetComponent<tk2dSpriteAnimator>();
        var inputHandler = InputHandler.Instance;
        var canvas = GameObject.Find("_GameCameras/HudCamera/Hud Canvas");

        while (true)
        {
            yield return Coroutines.SleepUntil(() => CanInspect() && inputHandler.inputActions.up.WasPressed);

            inspectable = false;
            heroController.RelinquishControl();
            heroController.StopAnimationControl();
#if !DEBUG
            PlayerData.instance.SetBool(nameof(PlayerData.disablePause), true);
#endif

            yield return Coroutines.Sequence(MoveKnightToCenter());
            animator.Play("TurnToBG");

            var hudObj = Instantiate(HudPrefab!);
            hudObj.transform.parent = canvas.transform;
            hudObj.layer = canvas.layer;
            foreach (var child in hudObj.Children()) child.layer = canvas.layer;
            hudObj.transform.localPosition = new(8.71f, -6.6f);
            var hud = hudObj.GetComponent<BinocularHud>();

            Wrapped<bool> changeCamera = new(false);
            hud.OnRelocateCamera = () => changeCamera.Value = true;
            var hudAnimator = hudObj.GetComponent<Animator>();
            hudAnimator.runtimeAnimatorController = hud.FadeIn;
            yield return Coroutines.SleepUntil(() => changeCamera.Value);

            module!.ActiveBinoculars = this;
            vignette.SetActive(false);
            activeCameraPos = ClampCameraPos(CameraStart!.position);

            yield return Coroutines.SleepSeconds(1);
            yield return Coroutines.SleepUntil(() => inputHandler.inputActions.attack.WasPressed);

            changeCamera.Value = false;
            hudAnimator.runtimeAnimatorController = hud.FadeOut;
            yield return Coroutines.SleepUntil(() => changeCamera.Value);
            hud.DoAfter(3, () => Destroy(hudObj.gameObject));

            module.ActiveBinoculars = null;
            vignette.SetActive(true);
            animator.Play("TurnFromBG");
            yield return Coroutines.SleepUntil(() => !animator.Playing);

            heroController.StartAnimationControl();
            heroController.RegainControl();
#if !DEBUG
            PlayerData.instance.SetBool(nameof(PlayerData.disablePause), false);
#endif

            yield return Coroutines.SleepSeconds(0.5f);
            inspectable = true;
        }
    }

    private IEnumerator<CoroutineElement> MoveKnightToCenter()
    {
        var heroController = HeroController.instance;
        var knight = heroController.gameObject;
        var kx = knight.transform.position.x;
        var animator = knight.GetComponent<tk2dSpriteAnimator>();

        bool facingRight = knight.transform.localScale.x < 0;
        bool onRight = kx > transform.position.x;
        if (facingRight != !onRight)
        {
            animator.Play("Turn");
            yield return Coroutines.SleepUntil(() => !animator.Playing);

            if (facingRight) heroController.FaceLeft();
            else heroController.FaceRight();
        }

        animator.Play("Walk");
        var rigidbody = knight.GetComponent<Rigidbody2D>();
        var origSign = Mathf.Sign(transform.position.x - kx);
        rigidbody.velocity = new(origSign * 6, 0);

        yield return Coroutines.SleepUntil(() => Mathf.Sign(transform.position.x - knight.transform.position.x) != origSign);
        rigidbody.velocity = Vector3.zero;
        knight.transform.SetPositionX(transform.position.x);
    }

    private bool promptToggle = false;
    private bool inspectable = true;

    private void Update()
    {
        var curPromptState = inspectable && Detector!.Detected();
        if (curPromptState != promptToggle)
        {
            promptToggle = curPromptState;
            promptMarker?.SendEvent(promptToggle ? "UP" : "DOWN");
        }

        if (module!.ActiveBinoculars == this)
        {
            var vec = InputHandler.Instance.inputActions.moveVector;
            Vector2 dir = new(vec.X, vec.Y);
            if (dir.sqrMagnitude > 0.01f)
            {
                var newPos = activeCameraPos + dir.normalized * CameraSpeed * Time.deltaTime;
                activeCameraPos = ClampCameraPos(newPos);
            }
        }
    }

    private Vector3 ClampCameraPos(Vector2 pos)
    {
        Vector2 closest = pos;
        float dist = Mathf.Infinity;
        foreach (var range in validRanges)
        {
            var cClosest = ClampToCollider(range, pos);
            var cDist = (cClosest - pos).sqrMagnitude;
            if (cDist < dist)
            {
                dist = cDist;
                closest = cClosest;
            }
        }

        return closest;
    }

    private Vector2 ClampToCollider(BoxCollider2D range, Vector2 pos)
    {
        var min = range.bounds.min;
        var max = range.bounds.max;

        var minX = min.x + GameConstants.CAMERA_HALF_WIDTH;
        var maxX = max.x - GameConstants.CAMERA_HALF_WIDTH;
        var minY = min.y + GameConstants.CAMERA_HALF_HEIGHT;
        var maxY = max.y - GameConstants.CAMERA_HALF_HEIGHT;

        if (minX < maxX) pos.x = Mathf.Clamp(pos.x, minX, maxX);
        else pos.x = (minX + maxX) / 2;
        if (minY < maxY) pos.y = Mathf.Clamp(pos.y, minY, maxY);
        else pos.y = (minY + maxY) / 2;

        return pos;
    }

    private Vector2 activeCameraPos;

    internal Vector3 GetCameraPos() => new(activeCameraPos.x, activeCameraPos.y, -38.1f);
}

[Shim]
internal class BinocularHud : MonoBehaviour
{
    [ShimField] public RuntimeAnimatorController? FadeIn;
    [ShimField] public RuntimeAnimatorController? FadeOut;

    internal Action? OnRelocateCamera;

    [ShimMethod] public void RelocateCamera() => OnRelocateCamera?.Invoke();
}