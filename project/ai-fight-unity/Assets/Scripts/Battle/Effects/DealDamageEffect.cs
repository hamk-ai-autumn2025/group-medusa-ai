using System.Collections;
using UnityEngine;

namespace dev.susybaka.TurnBasedGame.Battle.Data
{
    [CreateAssetMenu(menuName = "Turn Based Game/Battles/Effects/Deal Damage")]
    public class DealDamageEffect : EffectData
    {
        public int amount = -10;

        public override IEnumerator Execute(ActionContext ctx)
        {
            foreach (var t in ctx.targets)
            {
                t.ModifyHealth(amount);

                // Trigger visual effect based on whether damage or healing was applied
                if (amount < 0)
                    t.DamageEffect();
                else
                    t.HealEffect();
            }
            yield break;
        }
    }
}