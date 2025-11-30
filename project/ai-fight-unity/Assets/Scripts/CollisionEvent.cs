using UnityEngine;
using UnityEngine.Events;
using dev.susybaka.TurnBasedGame.Characters;
using dev.susybaka.TurnBasedGame.Player;

namespace dev.susybaka.TurnBasedGame
{
    public class CollisionEvent : MonoBehaviour
    {
        public UnityEvent<Character> onCollision;

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision.CompareTag("Player") && collision.transform.root.TryGetComponentInChildren(true, out PlayerCharacter pc))
            {
                onCollision?.Invoke(pc);
            }
        }
    }
}