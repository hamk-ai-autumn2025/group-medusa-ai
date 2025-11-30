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
            // Use ctx.damage if it's set (non-zero), otherwise use the predefined amount
            int damage = ctx.damage != 0 ? ctx.damage : amount;

            foreach (var t in ctx.targets)
            {
                bool isHeal = damage >= 0;
                int finalAmount = damage;
                bool miss = false;

                if (!isHeal)
                {
                    // Use a simple scaling formula for attackPower:
                    // finalAmount = amount + Mathf.FloorToInt(Mathf.Pow(ctx.actor.attackPower.Value, 0.7f))
                    // This gives diminishing returns for higher attackPower, but still increases damage meaningfully.
                    finalAmount = damage + Mathf.FloorToInt(Mathf.Pow(ctx.actor.attackPower.Value, 0.7f));
                    // Apply accuracy miss chance if it's not approximately 1
                    if (!Mathf.Approximately(ctx.accuracy, 1f))
                    {
                        miss = Random.value > ctx.accuracy;
                        if (miss)
                            finalAmount = 0;
                    }
                }

                t.ModifyHealth(damage);

                // Trigger visual effect based on whether damage or healing was applied
                if (!isHeal)
                    t.DamageEffect(finalAmount);
                else
                    t.HealEffect(finalAmount);
            }

            // Refresh the party members UI after applying damage/healing
            ctx.battle.battleWindow.PartyMembers.RefreshUI();
            yield break;
        }
    }
}