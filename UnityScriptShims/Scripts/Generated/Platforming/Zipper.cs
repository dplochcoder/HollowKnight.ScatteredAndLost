namespace HK8YPlando.Scripts.Platforming
{
    public class Zipper : UnityEngine.MonoBehaviour
    {
        public System.Collections.Generic.List<UnityEngine.Sprite> CogSprites;
        public float CogFpsFast;
        public float CogFpsSlow;
        public UnityEngine.Sprite RedLightSprite;
        public UnityEngine.Sprite YellowLightSprite;
        public UnityEngine.Sprite GreenLightSprite;
        public ZipperPlatform Platform;
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