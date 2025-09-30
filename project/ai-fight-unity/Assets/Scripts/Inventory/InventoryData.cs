using System.Collections.Generic;
using UnityEngine;

namespace dev.susybaka.TurnBasedGame.Items
{
    [CreateAssetMenu(menuName = "Turn Based Game/Characters/Inventories/Inventory")]
    public class InventoryData : ScriptableObject
    {
        public List<ItemStack> items = new List<ItemStack>();
    }

    [System.Serializable]
    public struct ItemStack
    {
        public ItemData item;
        public int count;

        public ItemStack(ItemData item, int count)
        {
            this.item = item;
            this.count = count;
        }
    }
}