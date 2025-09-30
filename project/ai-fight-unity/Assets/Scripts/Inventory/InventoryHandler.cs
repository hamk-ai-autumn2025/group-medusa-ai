using System.Collections.Generic;

namespace dev.susybaka.TurnBasedGame.Items
{
    public static class InventoryHandler
    {
        private static readonly Dictionary<InventoryData, Inventory> inventories = new Dictionary<InventoryData, Inventory>();

        public static void Reset() => inventories.Clear();

        public static Inventory Get(InventoryData data)
        {
            if (data == null)
                return null;

            if (!inventories.TryGetValue(data, out var inv))
            {
                inv = Inventory.FromAsset(data);
                inventories[data] = inv;
            }
            return inv;
        }
    }
}