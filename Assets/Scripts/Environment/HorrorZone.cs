using UnityEngine;

namespace HorrorGame
{
    public class HorrorZone : MonoBehaviour
    {
        public enum HorrorEffect { RiseHorrorSound }
        [SerializeField] private HorrorEffect effectType = HorrorEffect.RiseHorrorSound;
        [SerializeField] [Range(0f, 1f)] private float soundIntensity = 1f;

        private void Awake()
        {
            Collider collider = GetComponent<Collider>();
            if (collider == null)
            {
                collider = gameObject.AddComponent<BoxCollider>();
            }
            collider.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                PlayerMovement player = other.GetComponent<PlayerMovement>();
                if (player)
                {
                    player.TriggerHorrorSound(effectType, soundIntensity);
                    Destroy(gameObject);
                }
            }
        }

        public HorrorEffect GetEffectType() => effectType;
        public float GetSoundIntensity() => soundIntensity;
    }
}