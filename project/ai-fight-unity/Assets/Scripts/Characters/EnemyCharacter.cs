using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using dev.susybaka.TurnBasedGame.Characters;
using dev.susybaka.TurnBasedGame.Interfaces;
using dev.susybaka.TurnBasedGame.Items;

namespace dev.susybaka.TurnBasedGame.Enemies
{
    public class EnemyCharacter : Character, IInteractable
    {
        [Header("Enemy")]
        public List<ItemData> drops;
        
        public bool isWalking = false;
        Animator animator;
        public UnityEvent<Character> onInteract;
        
        protected override void Awake()
        {
            base.Awake();
            animator = GetComponentInChildren<Animator>();
        }

        public void Interact()
        {
            //Debug.Log("Interact");
            onInteract?.Invoke(this);
        }

        public void DropLoot()
        {
            // Implement loot drop logic
        }

        [ContextMenu("Debug Hurt")]
        public void Hurt()
        {
            animator.SetTrigger("hit");
        }
    }
}