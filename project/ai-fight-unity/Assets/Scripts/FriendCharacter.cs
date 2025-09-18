using UnityEngine;
using dev.susybaka.TurnBasedGame.Characters;
using dev.susybaka.TurnBasedGame.Interfaces;
using UnityEngine.Events;

public class FriendCharacter : Character, IInteractable
{
    public UnityEvent<Character> onInteract;

    public void Interact()
    {
        Debug.Log("Interact");
        onInteract?.Invoke(this);
    }
}
