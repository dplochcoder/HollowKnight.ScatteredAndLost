namespace HK8YPlando.Scripts.Platforming
{
    [UnityEngine.RequireComponent(typeof(UnityEngine.SpriteRenderer))]
    public class HeartDoorHeart : UnityEngine.MonoBehaviour
    {
        public UnityEngine.Sprite EmptySprite;
        public UnityEngine.Sprite FullSprite;
        public UnityEngine.RuntimeAnimatorController HeartAnim;
        public void AnimDone() { }
        
    }
}