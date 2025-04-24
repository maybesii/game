using UnityEngine;
using HorrorGame;

public class TrapComponent : MonoBehaviour
{
    [SerializeField] private GameObject trapEffect;
    [SerializeField] private AudioClip trapSound;

    public void Activate()
    {
        if (trapEffect != null)
        {
            trapEffect.SetActive(true);
        }

        if (trapSound != null)
        {
            AudioSource.PlayClipAtPoint(trapSound, transform.position);
        }

        DebugLogger.Log($"Trap {gameObject.name} activated!");
    }
}