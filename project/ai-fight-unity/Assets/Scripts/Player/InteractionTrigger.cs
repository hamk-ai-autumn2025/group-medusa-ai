using System.Collections.Generic;
using UnityEngine;
using dev.susybaka.TurnBasedGame.Characters;
using dev.susybaka.TurnBasedGame.Input;
using dev.susybaka.TurnBasedGame.Interfaces;

namespace dev.susybaka.TurnBasedGame.Player
{
    [RequireComponent(typeof(Collider2D))]
    public class InteractionTrigger : MonoBehaviour
    {
        InputHandler Input;
        Character character;

        private List<IInteractable> interactables = new List<IInteractable>();

        private void Start()
        {
            Input = GameManager.Instance.Input;
            interactables = new List<IInteractable>();
            character = transform.GetComponentInParents<Character>();
        }

        private void Update()
        {
            if (Input == null)
                return;

            if (Input.InteractInput && interactables.Count > 0)
            {
                // Interact with the first interactable in the list
                interactables[0].Interact(character);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Player"))
                return; // Ignore player colliders
            else if (other.CompareTag("Interactable"))
            {
                if (other.TryGetComponent(out IInteractable interactable))
                {
                    if (interactables.Contains(interactable))
                        return;

                    interactables.Add(interactable);
                }
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.CompareTag("Player"))
                return; // Ignore player colliders
            else if (other.CompareTag("Interactable"))
            {
                if (other.TryGetComponent(out IInteractable interactable))
                {
                    if (!interactables.Contains(interactable))
                        return;

                    interactables.Remove(interactable);
                }
            }
        }
    }
}