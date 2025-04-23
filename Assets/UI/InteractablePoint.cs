using UnityEngine;
using UnityEngine.Events;

public class InteractablePoint : MonoBehaviour
{
    [SerializeField] private string displayText = "Осмотреть объект"; 
    [SerializeField] private string buttonText = "Осмотреть";
    [SerializeField] private AudioClip hoverSound;
    [SerializeField] private UnityEvent onInteract = new UnityEvent();

    public string GetDisplayText() => displayText;
    public string GetButtonText() => buttonText;
    public AudioClip GetHoverSound() => hoverSound;

    public void Interact()
    {
        onInteract.Invoke();
    }
}