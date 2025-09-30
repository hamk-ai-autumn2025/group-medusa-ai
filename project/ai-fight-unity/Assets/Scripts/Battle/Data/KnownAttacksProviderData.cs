using System.Collections.Generic;
using UnityEngine;
using dev.susybaka.TurnBasedGame.Characters;

namespace dev.susybaka.TurnBasedGame.Battle.Data
{
    [CreateAssetMenu(fileName = "New Known Attacks Provider Data", menuName = "Turn Based Game/Battles/Node Providers/Known Attacks Provider")]
    public class KnownAttacksProviderData : NodeProviderData
    {
        public override IEnumerable<AbilityData> BuildAbilities(Character actor)
        {
            return actor.KnownAbilities;
        }
    }
}