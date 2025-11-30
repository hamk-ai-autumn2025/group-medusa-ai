using System.Collections;
using UnityEngine;

namespace dev.susybaka.TurnBasedGame.Battle.Data
{
    [CreateAssetMenu(menuName = "Turn Based Game/Battles/Effects/Set Health")]
    public class SetHealthEffect : EffectData
    {
        public bool forceHealth = false;
        public bool fromMax = false;
        public int value = -1;
        public float percentage = 0.5f;

        public override IEnumerator Execute(ActionContext ctx)
        {
            foreach (var t in ctx.targets)
            {
                int targetHealth = t.health;
                int targetMaxHealth = t.maxHealth;
                int newHealth = targetHealth;
                bool miss = false;

                // Determine the intended new health
                if (value >= 0)
                {
                    // Set health to a specific value
                    newHealth = value;
                }
                else if (percentage >= 0f)
                {
                    int baseValue = fromMax ? targetMaxHealth : targetHealth;
                    int percentHealth = Mathf.RoundToInt(baseValue * percentage);

                    if (forceHealth)
                    {
                        newHealth = percentHealth;
                    }
                    else
                    {
                        // Only allow health to increase, not decrease
                        if (percentHealth > targetHealth)
                            newHealth = percentHealth;
                        else
                            newHealth = targetHealth;
                    }
                }

                // Calculate the delta to apply
                int delta = newHealth - targetHealth;

                // Apply accuracy
                if (!Mathf.Approximately(ctx.accuracy, 1f))
                {
                    miss = Random.value > ctx.accuracy;
                    if (miss)
                        delta = 0;
                }

                t.ModifyHealth(delta);

                // Trigger visual effect
                if (delta < 0)
                    t.DamageEffect(-delta);
                else if (delta > 0)
                    t.HealEffect(delta);
            }

            ctx.battle.battleWindow.PartyMembers.RefreshUI();
            yield break;
        }
    }
}