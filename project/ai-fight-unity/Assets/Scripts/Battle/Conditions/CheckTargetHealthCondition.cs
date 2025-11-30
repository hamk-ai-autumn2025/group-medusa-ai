using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace dev.susybaka.TurnBasedGame.Battle.Data
{
    [CreateAssetMenu(menuName = "Turn Based Game/Battles/Conditions/Check Target Health")]
    public class CheckTargetHealthCondition : ConditionData
    {
        public enum ComparisonType
        {
            LessThan,
            LessThanOrEqual,
            Equal,
            GreaterThanOrEqual,
            GreaterThan
        }

        public ComparisonType comparisonType;
        public float percent = -1f;
        public int health = -1;

        public override bool Evaluate(ActionContext ctx, out string reason)
        {
            if (ctx.targets != null && ctx.targets.Count > 0)
            {
                bool result = false;

                for (int i = 0; i < ctx.targets.Count; i++)
                {
                    var target = ctx.targets[i];
                    int targetHealth = 0;
                    
                    if (health >= 0)
                    {
                        targetHealth = target.health;
                    }
                    else if (percent >= 0f)
                    {
                        targetHealth = Mathf.FloorToInt(target.maxHealth * (percent / 100f));
                    }

                    result = comparisonType switch
                    {
                        ComparisonType.LessThan => targetHealth < health,
                        ComparisonType.LessThanOrEqual => targetHealth <= health,
                        ComparisonType.Equal => targetHealth == health,
                        ComparisonType.GreaterThanOrEqual => targetHealth >= health,
                        ComparisonType.GreaterThan => targetHealth > health,
                        _ => false,
                    };
                }

                if (!result)
                {
                    reason = $"Targets do not meet the health conditions.";
                    return false;
                }
                else
                {
                    reason = null;
                    return true;
                }
            }

            reason = "No targets for condition.";
            return false;
        }

        public void Reset()
        {
            preTurn = false;
            postTurn = true;
            comparisonType = ComparisonType.LessThan;
            health = 1;
        }
    }
}