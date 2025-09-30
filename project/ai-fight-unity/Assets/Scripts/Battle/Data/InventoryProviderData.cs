using System.Collections.Generic;
using UnityEngine;
using dev.susybaka.TurnBasedGame.Characters;
using dev.susybaka.TurnBasedGame.Items;

namespace dev.susybaka.TurnBasedGame.Battle.Data
{
    [CreateAssetMenu(fileName = "New Inventory Provider Data", menuName = "Turn Based Game/Battles/Node Providers/Inventory Provider")]
    public class InventoryProviderData : NodeProviderData
    {
        // cache per Inventory instance
        private class Cache { public int version; public Dictionary<AbilityData, ItemData> map = new(); }
        private static readonly Dictionary<Inventory, Cache> _cache = new();

        public override IEnumerable<AbilityData> BuildAbilities(Character actor)
        {
            Inventory inv = actor?.Inventory;

            if (inv == null)
                yield break;

            EnsureCache(inv);

            // Build from live entries (not cache) so ordering reflects inventory order
            foreach (ItemStack s in inv.NonZeroEntries())
            {
                if (s.item && s.item.useAbility)
                    yield return s.item.useAbility;
            }
        }

        public override string GetLabel(Character actor, AbilityData ability)
        {
            Inventory inv = actor?.Inventory;

            if (inv == null || ability == null)
                return ability ? ability.displayName : "<null>";
            
            EnsureCache(inv);

            Cache cache = _cache[inv];
            if (cache.map.TryGetValue(ability, out ItemData item))
                return $"{item.displayName} x{inv.CountOf(item)}";

            return ability.displayName;
        }

        private static void EnsureCache(Inventory inv)
        {
            if (!_cache.TryGetValue(inv, out Cache c))
            {
                c = new Cache();
                _cache[inv] = c;
                c.version = -1; // force build
            }
            if (c.version == inv.Version)
                return;

            c.map.Clear();
            foreach (ItemStack s in inv.NonZeroEntries())
            {
                if (s.item && s.item.useAbility)
                    c.map[s.item.useAbility] = s.item;
            }

            c.version = inv.Version;
        }
    }
}