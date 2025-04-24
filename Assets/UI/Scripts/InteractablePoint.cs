using UnityEngine;
using HorrorGame;

namespace HorrorGame
{
    public class InteractablePoint : MonoBehaviour
    {
        [SerializeField] private InteractionConfig interactionConfig;

        public string GetDisplayText() => interactionConfig.displayText;
        public InteractionConfig GetInteractionConfig() => interactionConfig;

        public void Interact(InteractionManager interactionManager)
        {
            interactionManager.HandleInteraction(interactionConfig, gameObject);
        }
    }
}