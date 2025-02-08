namespace HK8YPlando.Scripts.Platforming
{
    public class Bubble : UnityEngine.MonoBehaviour
    {
        public UnityEngine.Animator BubbleAnimator;
        public UnityEngine.RuntimeAnimatorController IdleController;
        public UnityEngine.RuntimeAnimatorController ActiveController;
        public UnityEngine.RuntimeAnimatorController DissolveController;
        public HK8YPlando.Scripts.Proxy.HeroDetectorProxy HeroDetector;
        public BubblePlayerDetector PlayerDetector;
        public float StallTime;
        public float Speed;
        
    }
}