using System.Collections.Generic;
using UnityEngine;
using dev.susybaka.TurnBasedGame.Characters;

namespace dev.susybaka.TurnBasedGame.Battle.Data
{
    [CreateAssetMenu(fileName = "New Known Spells Provider Data", menuName = "Turn Based Game/Battles/Node Providers/Known Spells Provider")]
    public class KnownSpellsProviderData : NodeProviderData
    {
        public override IEnumerable<AbilityData> BuildAbilities(Character actor)
        {
            return actor.KnownSpells;
        }
    }
}