using UnityEngine;
using dev.susybaka.TurnBasedGame.Battle;
using dev.susybaka.TurnBasedGame.Battle.Data;

namespace dev.susybaka.TurnBasedGame.Items
{
    public enum ItemType { Weapon, Consumable, Quest, Misc, Drop }

    [CreateAssetMenu(menuName = "Turn Based Game/Characters/Inventories/Item")]
    public class ItemData : ScriptableObject
    {
        // Data
        public ItemType type = ItemType.Misc;
        public string displayName;
        public string description;
        public bool stackable = true;
        public int maxStack = 99;
        public int value = 0;

        // Logic
        public bool canBeUsed = false;
        public bool canBeEquipped = false;
        public bool needsTarget = false;
        public TargetGroup targetGroup = TargetGroup.enemy;
        [Min(0)] public int consumeOnUse = 1;
        public AbilityData useAbility;
    }
}