using UnityEngine;
using UnityEngine.Events;
using dev.susybaka.TurnBasedGame.Interfaces;

namespace dev.susybaka.TurnBasedGame.Characters
{
    public class FriendCharacter : Character, IInteractable
    {
        public UnityEvent<Character> onInteract;

        public void Interact()
        {
            Debug.Log("Interact");
            onInteract?.Invoke(this);
        }
    }
}