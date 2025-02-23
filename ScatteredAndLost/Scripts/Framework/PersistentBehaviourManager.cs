using HK8YPlando.Scripts.SharedLib;
using HK8YPlando.Util;
using SFCore.Utils;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HK8YPlando.Scripts.Framework;

interface IPersistentBehaviour<B, M> where B : MonoBehaviour, IPersistentBehaviour<B, M> where M : PersistentBehaviourManager<B, M>
{
    void AwakeWithManager(M initManager);

    void SceneChanged(M newManager);

    void Stop();
}

internal abstract class PersistentBehaviourManager<B, M> : MonoBehaviour where B : MonoBehaviour, IPersistentBehaviour<B, M> where M : PersistentBehaviourManager<B, M>
{
    [ShimField] public GameObject? prefab;

    private static string mgrName = "";
    private static UnityEngine.Events.UnityAction<Scene, LoadSceneMode>? sceneChangeHandler;
    private static GameObject? existing = null;

    public static B? Get() => existing?.GetComponent<B>();

    public abstract M Self();

    public static void Drop(B current)
    {
        current.Stop();
        current.DoAfter(10f, () => Destroy(current.gameObject));
        if (existing?.gameObject != current.gameObject) return;

        existing = null;
        mgrName = "";
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= sceneChangeHandler;
    }

    protected void Awake()
    {
        var prevObj = Get();
        if (prevObj != null)
        {
            prevObj.SceneChanged(Self());
            Destroy(gameObject);
            return;
        }

        mgrName = gameObject.name;
        existing = Instantiate(prefab);
        existing!.name = $"Persistent_{typeof(B).Name}";
        Get()!.AwakeWithManager(Self());
        DontDestroyOnLoad(existing);

        sceneChangeHandler = (scene, mode) =>
        {
            if (scene.Find(mgrName) != null) return;
            Drop(existing.GetComponent<B>());
        };
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += sceneChangeHandler;
    }
}