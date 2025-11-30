using UnityEngine;

namespace dev.susybaka.TurnBasedGame.Battle.Data 
{
    public abstract class ConditionData : ScriptableObject
    {
        public bool preTurn = false;
        public bool postTurn = true;

        public abstract bool Evaluate(ActionContext ctx, out string reason);
    }
}