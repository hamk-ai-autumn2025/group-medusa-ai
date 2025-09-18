using UnityEngine;

namespace dev.susybaka.TurnBasedGame.Inventory
{
    [CreateAssetMenu(menuName = "Turn Based Game/Item")]
    public class ItemObject : ScriptableObject
    {
        public enum Type { Weapon, Consumable, Quest, Misc, Drop }

        public Type ItemType = Type.Misc;
        public string Name = string.Empty;
        public string Description = string.Empty;
        public Sprite Icon = null;
        public int Value = 0;
        [TextArea(2,4)]
        public string Data = string.Empty;
        // Add more properties as needed
    }
}