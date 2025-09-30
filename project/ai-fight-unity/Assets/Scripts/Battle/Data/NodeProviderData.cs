using System.Collections.Generic;
using UnityEngine;
using dev.susybaka.TurnBasedGame.Characters;

namespace dev.susybaka.TurnBasedGame.Battle.Data
{
    public abstract class NodeProviderData : ScriptableObject
    {
        public abstract IEnumerable<AbilityData> BuildAbilities(Character actor);
        public virtual string GetLabel(Character actor, AbilityData ability) => ability.displayName;
    }
}