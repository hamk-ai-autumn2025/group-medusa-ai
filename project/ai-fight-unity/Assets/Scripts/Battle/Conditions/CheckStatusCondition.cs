using UnityEngine;

namespace dev.susybaka.TurnBasedGame.Battle.Data
{
    [CreateAssetMenu(menuName = "Turn Based Game/Battles/Conditions/Check Status")]
    public class CheckStatusCondition : ConditionData
    {
        public string statusEffect;
        public bool checkForPresence = true;

        public override bool Evaluate(ActionContext ctx, out string reason)
        {
            //if ((ctx.actor.HasEffect(statusEffect) && checkForPresence) || (!ctx.actor.HasEffect(statusEffect) && !checkForPresence))
            //{ reason = null; return true; }
            reason = checkForPresence ? "Status effect not found" : "Status effect was found";
            return false;
        }
    }
}