using UnityEngine;
using UnityEngine.Events;

namespace HorrorGame
{
    public enum InteractionType
    {
        None,
        OpenDoor,
        PickUpItem,
        ActivateTrap,
        ToggleSwitch,
        ShowNarrative,
        ApplyHorrorEffect
    }

    [CreateAssetMenu(fileName = "InteractionConfig", menuName = "Interaction/InteractionConfig")]
    public class InteractionConfig : ScriptableObject
    {
        public InteractionType interactionType = InteractionType.None;
        public string displayText = "Осмотреть объект";
        public AudioClip interactSound;
        public GameObject targetObject;
        public Vector3 moveToPosition;
        public float moveDuration = 1f;
        public string narrativeText;
        public UnityEvent onInteract;
        public float horrorEffectIntensity = 0.2f;
    }
}