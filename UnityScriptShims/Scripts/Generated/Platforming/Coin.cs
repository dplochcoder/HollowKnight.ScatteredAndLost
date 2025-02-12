namespace HK8YPlando.Scripts.Platforming
{
    public class Coin : UnityEngine.MonoBehaviour
    {
        public System.Collections.Generic.List<UnityEngine.SpriteRenderer> Renderers;
        public UnityEngine.ParticleSystem ParticleSystem;
        public UnityEngine.Animator Animator;
        public HK8YPlando.Scripts.Proxy.HeroDetectorProxy HeroDetector;
        public UnityEngine.AudioClip ObtainedClip;
        public UnityEngine.Color IdleColor;
        public UnityEngine.Color FlashColor;
        public UnityEngine.Color ActiveColor;
        public float CooldownTime;
        public float FlashTransitionTime;
        public float FlashHangTime;
        public float FlashAnimationSpeed;
        public float ActiveTransitionTime;
        
    }
}