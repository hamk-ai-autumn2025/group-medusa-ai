using UnityEngine;
using dev.susybaka.TurnBasedGame.Items;

namespace dev.susybaka.TurnBasedGame.Battle.Data
{
    [CreateAssetMenu(menuName = "Turn Based Game/Battles/Conditions/Has Item")]
    public class HasItemCondition : ConditionData
    {
        public ItemData item; 
        public int amount = 1;
        
        public override bool Evaluate(ActionContext ctx, out string reason)
        {
            Inventory inv = ctx.actor?.Inventory;

            if (inv != null && inv.CountOf(item) >= amount)
            { reason = null; return true; }
            
            reason = "Not enough items";
            return false;
        }
    }
}