using UnityEngine;
using UnityEngine.Events;
using dev.susybaka.TurnBasedGame.Characters;
using dev.susybaka.TurnBasedGame.Interfaces;

namespace dev.susybaka.TurnBasedGame
{
    public class InteractionEvent : MonoBehaviour, IInteractable
    {
        public UnityEvent<Character> onInteract;

        public void Interact(Character actor)
        {
            onInteract?.Invoke(actor);
        }
    }
}