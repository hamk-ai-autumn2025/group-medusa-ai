using System.Collections.Generic;
using UnityEngine;
using dev.susybaka.TurnBasedGame.Characters;

namespace dev.susybaka.TurnBasedGame.Enemies
{
    public class EnemyCharacter : Character
    {
        [Header("Enemy")]
        public string AIType;
        public List<int> LootTable;

        public bool inCombat = false;
        public bool isWalking = false;
        Animator animator;

        protected override void Awake()
        {
            base.Awake();
            animator = GetComponentInChildren<Animator>();
        }

        private void Update()
        {
            if (animator != null)
            {
                animator.SetBool("inCombat", inCombat);
                animator.SetBool("isWalking", isWalking);
            }
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