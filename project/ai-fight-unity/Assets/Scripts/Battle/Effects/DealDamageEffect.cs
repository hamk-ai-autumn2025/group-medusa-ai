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
                bool isHeal = amount >= 0;
                int finalAmount = amount;

                if (!isHeal)
                    // Use a simple scaling formula for attackPower:
                    // finalAmount = amount + Mathf.FloorToInt(Mathf.Pow(ctx.actor.attackPower.Value, 0.7f))
                    // This gives diminishing returns for higher attackPower, but still increases damage meaningfully.
                    finalAmount = amount + Mathf.FloorToInt(Mathf.Pow(ctx.actor.attackPower.Value, 0.7f));

                t.ModifyHealth(amount);

                // Trigger visual effect based on whether damage or healing was applied
                if (!isHeal)
                    t.DamageEffect();
                else
                    t.HealEffect();
            }
            yield break;
        }
    }
}