using UnityEngine;

namespace dev.susybaka.TurnBasedGame.Battle.Data
{
    [CreateAssetMenu(menuName = "Turn Based Game/Battles/Conditions/Has Action Points")]
    public class HasActionPointsCondition : ConditionData
    {
        public int actionPointCost;

        public override bool Evaluate(ActionContext ctx, out string reason)
        {
            if (ctx.actor.ActionPoints >= actionPointCost)
            { reason = null; return true; }
            reason = "Not enough Action Points";
            return false;
        }
    }
}