namespace HK8YPlando.Scripts.Platforming
{
    public class HeartDoor : UnityEngine.MonoBehaviour
    {
        public int DoorIndex;
        public float FallHeight;
        public float FallSpeed;
        public float FallBuffer;
        public float FallDelay;
        public float HeartActivationDelay;
        public UnityEngine.RuntimeAnimatorController OpenController;
        public System.Collections.Generic.List<UnityEngine.ParticleSystem> ClosedParticleSystems;
        public System.Collections.Generic.List<UnityEngine.ParticleSystem> OpenParticleSystems;
        public System.Collections.Generic.List<UnityEngine.AudioClip> HeartSounds;
        public UnityEngine.AudioClip OpenSound;
        public UnityEngine.GameObject Terrain;
        public UnityEngine.GameObject MainRender;
        public HK8YPlando.Scripts.Proxy.HeroDetectorProxy ActivationTrigger;
        public UnityEngine.GameObject HeartPrefab;
        public void StopDoorParticles() { }
        public void DoorOpened() { }
        
    }
}