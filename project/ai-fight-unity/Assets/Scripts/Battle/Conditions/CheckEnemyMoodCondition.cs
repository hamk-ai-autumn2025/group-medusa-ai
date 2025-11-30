using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using dev.susybaka.TurnBasedGame.Enemies;

namespace dev.susybaka.TurnBasedGame.Battle.Data
{
    [CreateAssetMenu(menuName = "Turn Based Game/Battles/Conditions/Check Enemy Mood")]
    public class CheckEnemyMoodCondition : ConditionData
    {
        public enum LogicType
        {
            And,
            Or
        }

        public LogicType logicType = LogicType.And;
        public int aggressionLevel = -1;
        public bool checkBelowAggressionThreshold = false;
        public int fearLevel = -1;
        public bool checkBelowFearThreshold = false;
        public int respectLevel = -1;
        public bool checkBelowRespectThreshold = false;
        public int pityLevel = -1;
        public bool checkBelowPityThreshold = false;

        public override bool Evaluate(ActionContext ctx, out string reason)
        {
            if (ctx.actor is EnemyCharacter)
            {
                EnemyCharacter eActor = (EnemyCharacter)ctx.actor;

                // Track each condition's result
                List<bool> conditionResults = new List<bool>();

                if (aggressionLevel >= 0)
                {
                    bool result = !checkBelowAggressionThreshold
                        ? eActor.aggressionLevel >= aggressionLevel
                        : eActor.aggressionLevel < aggressionLevel;
                    conditionResults.Add(result);
                }
                if (fearLevel >= 0)
                {
                    bool result = !checkBelowFearThreshold
                        ? eActor.fearLevel >= fearLevel
                        : eActor.fearLevel < fearLevel;
                    conditionResults.Add(result);
                }
                if (respectLevel >= 0)
                {
                    bool result = !checkBelowRespectThreshold
                        ? eActor.respectLevel >= respectLevel
                        : eActor.respectLevel < respectLevel;
                    conditionResults.Add(result);
                }
                if (pityLevel >= 0)
                {
                    bool result = !checkBelowPityThreshold
                        ? eActor.pityLevel >= pityLevel
                        : eActor.pityLevel < pityLevel;
                    conditionResults.Add(result);
                }

                bool meetsConditions;
                if (conditionResults.Count == 0)
                {
                    // No conditions set, always true
                    meetsConditions = true;
                }
                else if (logicType == LogicType.And)
                {
                    meetsConditions = conditionResults.TrueForAll(r => r);
                }
                else // LogicType.Or
                {
                    meetsConditions = conditionResults.Exists(r => r);
                }

                if (meetsConditions)
                {
                    reason = null;
                    return true;
                }

                reason = "Enemy mood levels do not meet the required thresholds.";
                return false;
            }
            else
            {
                reason = "Actor is not an enemy character.";
                return false;
            }
        }
    }
}