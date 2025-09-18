using System.Collections.Generic;
using UnityEngine;

namespace dev.susybaka.TurnBasedGame.Inventory
{
    [CreateAssetMenu(menuName = "Turn Based Game/Inventory")]
    public class InventoryObject : ScriptableObject
    {
        public enum Type { Weapon, Consumable, Quest, Misc, Drop, All }

        public Type ItemTypeAllowed = Type.All;
        public List<ItemObject> Items = new List<ItemObject>();
        // Add more inventory-related properties or methods as needed
    }
}