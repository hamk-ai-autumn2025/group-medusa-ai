using UnityEngine;

namespace dev.susybaka.TurnBasedGame.Battle.Data
{
    [CreateAssetMenu(menuName = "Turn Based Game/Battles/Conditions/Has Mana")]
    public class HasManaCondition : ConditionData
    {
        public int manaCost;

        public override bool Evaluate(ActionContext ctx, out string reason)
        {
            if (ctx.actor.mana >= manaCost)
            { reason = null; return true; }
            reason = "Not enough Mana";
            return false;
        }
    }
}