using System;
using System.Collections.Generic;

namespace dev.susybaka.TurnBasedGame.Items
{
    public sealed class Inventory
    {
        private readonly List<ItemStack> items = new List<ItemStack>();
        public event Action OnChanged;

        private int _version = 0;
        public int Version => _version;

        public static Inventory FromAsset(InventoryData data)
        {
            Inventory inv = new Inventory();
            if (data != null)
            {
                foreach (ItemStack s in data.items)
                {
                    if (s.item && s.count > 0)
                        inv.Add(s.item, s.count);
                }
            }
            return inv;
        }

        public IEnumerable<ItemStack> NonZeroEntries()
        {
            foreach (ItemStack s in items)
            {
                if (s.item && s.count > 0)
                    yield return s;
            }
        }

        public int CountOf(ItemData item)
        {
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i].item == item)
                    return items[i].count;
            }
            return 0;
        }

        public void Add(ItemData item, int amount = 1)
        {
            if (!item || amount <= 0)
                return;

            for (int i = 0; i < items.Count; i++)
            {
                if (items[i].item == item)
                {
                    items[i] = new ItemStack(item, items[i].count + amount); //{ item = item, count = items[i].count + amount }; 
                    _version++;
                    OnChanged?.Invoke();
                    return; 
                }
            }
            items.Add(new ItemStack(item, amount)); //{ item = item, count = amount });
            _version++;
            OnChanged?.Invoke();
        }

        public bool TryConsume(ItemData item, int amount = 1)
        {
            if (!item || amount <= 0)
                return false;

            for (int i = 0; i < items.Count; i++)
            {
                if (items[i].item != item)
                    continue;
                if (items[i].count < amount)
                    return false;
                var newCount = items[i].count - amount;
                items[i] = new ItemStack(item, newCount); //{ item = item, count = newCount };
                _version++;
                OnChanged?.Invoke();
                return true;
            }
            return false;
        }
    }
}