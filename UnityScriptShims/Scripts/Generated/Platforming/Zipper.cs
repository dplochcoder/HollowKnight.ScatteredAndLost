namespace HK8YPlando.Scripts.Platforming
{
    public class Zipper : UnityEngine.MonoBehaviour
    {
        public System.Collections.Generic.List<UnityEngine.Sprite> CogSprites;
        public UnityEngine.Sprite RedLightSprite;
        public UnityEngine.Sprite YellowLightSprite;
        public UnityEngine.Sprite GreenLightSprite;
        public float RotationPerUnit;
        public float SpritesPerUnit;
        public System.Collections.Generic.List<UnityEngine.AudioClip> TouchClips;
        public System.Collections.Generic.List<UnityEngine.AudioClip> ImpactClips;
        public UnityEngine.AudioClip RewindIntro;
        public UnityEngine.AudioClip RewindLoop;
        public System.Collections.Generic.List<UnityEngine.AudioClip> ResetClips;
        public ZipperPlatform Platform;
        public DamageHero BottomHurtBox;
        public UnityEngine.Transform RestPosition;
        public UnityEngine.Transform TargetPosition;
        public float ShakeRadius;
        public float ShakeTime;
        public float StartSpeed;
        public float Accel;
        public float PauseTime;
        public float RewindSpeed;
        public float RewindCooldown;
        
    }
}