namespace HK8YPlando.Scripts.Platforming
{
    public class CoinDoor : UnityEngine.MonoBehaviour
    {
        public UnityEngine.GameObject ShakeBase;
        public UnityEngine.SpriteRenderer MarkerRenderer;
        public UnityEngine.Animator MarkerAnimator;
        public UnityEngine.Sprite InactiveMarkerSprite;
        public UnityEngine.RuntimeAnimatorController ActiveMarkerController;
        public UnityEngine.Color IdleColor;
        public UnityEngine.Color ActiveColor;
        public float ShakeRadius;
        public float ShakeTime;
        public float AfterShakeDelay;
        public float MoveDuration;
        public UnityEngine.Vector3 MoveOffset;
        public float ResetDelay;
        public float ResetDuration;
        
    }
}