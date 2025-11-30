using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using dev.susybaka.TurnBasedGame.Characters;
using dev.susybaka.TurnBasedGame.Dialogue;
using dev.susybaka.TurnBasedGame.Dialogue.Data;
using dev.susybaka.TurnBasedGame.Interfaces;

namespace dev.susybaka.TurnBasedGame.Items
{
    public class LootContainer : MonoBehaviour, IInteractable
    {
        DialogueHandler dialogueHandler;

        [SerializeField] private DialogueData lootDialogue;
        [SerializeField] private DialogueData emptyDialogue;
        [SerializeField] private LootEntry[] lootPool;
        [SerializeField] private int maxDrops = 3;
        public UnityEvent<Character> onInteracted;

        private bool looted = false;

        [System.Serializable]
        public struct LootEntry
        {
            public string name;
            public ItemData item;
            public int dropChance;
            public Vector2 quantity;
        }

        private void OnValidate()
        {
            for (int i = 0; i < lootPool.Length; i++)
            {
                LootEntry entry = lootPool[i];

                if (entry.item != null)
                    entry.name = string.Format("{0}{1} {2}%", entry.item.displayName, (entry.quantity.x > 0 && entry.quantity.y > 1) ? string.Format(" {0}-{1}", entry.quantity.x, entry.quantity.y) : "", entry.dropChance);
                if (entry.dropChance < 0)
                    entry.dropChance = 0;
                if (entry.dropChance > 100)
                    entry.dropChance = 100;
                if (entry.quantity.x < 0)
                    entry.quantity = new Vector2(0, entry.quantity.y);
                if (entry.quantity.y < entry.quantity.x)
                    entry.quantity = new Vector2(entry.quantity.x, entry.quantity.x);

                lootPool[i] = entry;
            }
        }

        private void Awake()
        {
            dialogueHandler = GameManager.Instance.DialogueHandler;
            looted = false;
        }

        public void Interact(Character actor)
        {
            if (looted)
            {
                if (emptyDialogue != null)
                {
                    dialogueHandler.StartDialogue(emptyDialogue, new DialogueContext(actor, null, null, null));
                }
                return;
            }

            looted = true;
            List<ItemData> lootedItems = new List<ItemData>();

            // Shuffle lootPool into lootTable
            LootEntry[] lootTable = new LootEntry[lootPool.Length];
            lootPool.CopyTo(lootTable, 0);

            // Fisher-Yates shuffle
            for (int i = lootTable.Length - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                var temp = lootTable[i];
                lootTable[i] = lootTable[j];
                lootTable[j] = temp;
            }

            foreach (LootEntry lootEntry in lootTable)
            {
                int roll = Random.Range(0, 101);
                if (roll <= lootEntry.dropChance)
                {
                    int chance = lootEntry.quantity == Vector2.zero ? 1 : (int)Mathf.RoundToInt((float)UnityEngine.Random.Range(lootEntry.quantity.x, lootEntry.quantity.y));
                    actor.Inventory.Add(lootEntry.item, chance);
                    lootedItems.Add(lootEntry.item);
                }
                if (lootedItems.Count >= maxDrops)
                    break;
            }

            if (lootDialogue != null && lootedItems.Count > 0)
            {
                // Here you would set up the dialogue with looted items information
                dialogueHandler.StartDialogue(lootDialogue, new DialogueContext(actor, null, null, lootedItems));
            } 
            else if (emptyDialogue != null)
            {
                dialogueHandler.StartDialogue(emptyDialogue, new DialogueContext(actor, null, null, null));
            }
        }
    }
}