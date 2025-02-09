namespace HK8YPlando.Scripts.Platforming
{
    public class HeartDoor : UnityEngine.MonoBehaviour
    {
        public string DataKey;
        public int NumHearts;
        public int HeartsPerRow;
        public float HSpace;
        public float VSpace;
        public float FallHeight;
        public float FallSpeed;
        public float FallBuffer;
        public float FallDelay;
        public float HeartActivationDelay;
        public UnityEngine.RuntimeAnimatorController OpenController;
        public System.Collections.Generic.List<UnityEngine.ParticleSystem> ClosedParticleSystems;
        public System.Collections.Generic.List<UnityEngine.ParticleSystem> OpenParticleSystems;
        public UnityEngine.GameObject Terrain;
        public UnityEngine.GameObject MainRender;
        public HK8YPlando.Scripts.Proxy.HeroDetectorProxy ActivationTrigger;
        public UnityEngine.GameObject HeartPrefab;
        public void StopDoorParticles() { }
        public void DoorOpened() { }
        
    }
}