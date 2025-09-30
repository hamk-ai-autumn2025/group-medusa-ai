using System.Collections.Generic;
using UnityEngine;

namespace dev.susybaka.TurnBasedGame.Battle.Data
{
    [CreateAssetMenu(menuName = "Turn Based Game/Battles/Command Node")]
    public class CommandNodeData : ScriptableObject
    {
        public string displayName;
        [TextArea] public string description;

        public NodeType type = NodeType.groupStatic;
        public AbilityData ability;
        public List<CommandNodeData> children;
        public NodeProviderData provider;
    }
}

namespace dev.susybaka.TurnBasedGame.Battle
{
    public enum NodeType { groupStatic, groupDynamic, abilityLeaf }
}