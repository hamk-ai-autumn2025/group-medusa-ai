using UnityEngine;

namespace dev.susybaka.TurnBasedGame.Battle.Data 
{
    public abstract class ConditionData : ScriptableObject
    {
        public abstract bool Evaluate(ActionContext ctx, out string reason);
    }
}