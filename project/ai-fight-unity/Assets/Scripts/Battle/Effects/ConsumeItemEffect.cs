using System.Collections;
using UnityEngine;
using dev.susybaka.TurnBasedGame.Items;

namespace dev.susybaka.TurnBasedGame.Battle.Data
{
    [CreateAssetMenu(menuName = "Turn Based Game/Battles/Effects/Consume Item")]
    public class ConsumeItemEffect : EffectData
    {
        public ItemData item; 
        public int amount = 1;
        
        public override IEnumerator Execute(ActionContext ctx)
        {
            ctx.actor?.Inventory?.TryConsume(item, amount);
            yield break;
        }
    }
}