namespace HK8YPlando.Scripts.Platforming
{
    public class BubbleController : UnityEngine.MonoBehaviour
    {
        public UnityEngine.Animator BubbleAnimator;
        public UnityEngine.RuntimeAnimatorController IdleController;
        public UnityEngine.RuntimeAnimatorController FillController;
        public UnityEngine.RuntimeAnimatorController ActiveController;
        public UnityEngine.RuntimeAnimatorController DissolveController;
        public UnityEngine.RuntimeAnimatorController RespawnController;
        public UnityEngine.AudioClip EntryClip;
        public UnityEngine.AudioClip LoopClip;
        public UnityEngine.AudioClip WallClip;
        public UnityEngine.AudioClip DashClip;
        public UnityEngine.AudioClip RespawnClip;
        public UnityEngine.GameObject Bubble;
        public UnityEngine.Rigidbody2D RigidBody;
        public HK8YPlando.Scripts.Proxy.HeroDetectorProxy Trigger;
        public float StallTime;
        public float Speed;
        public float RespawnDelay;
        public float RespawnCooldown;
        public UnityEngine.Vector3 KnightOffset;
        
    }
}