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
        public int aggressionLevel = 50;
        public int fearLevel = 0;
        public int respectLevel = 0;
        public int pityLevel = 0;
        public int curiosityLevel = 0;
        public int desperationLevel = 0;
        public List<ItemData> drops;
        public UnityEvent<Character> onInteract;

        public void Interact()
        {
            //Debug.Log("Interact");
            onInteract?.Invoke(this);
        }

        public void DropLoot()
        {
            // Implement loot drop logic
        }
    }
}