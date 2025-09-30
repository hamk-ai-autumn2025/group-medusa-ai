using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using dev.susybaka.TurnBasedGame.Battle.Data;
using dev.susybaka.TurnBasedGame.Characters;

namespace dev.susybaka.TurnBasedGame.Battle
{
    public class AbilitySystem : MonoBehaviour
    {
        public IEnumerator Run(AbilityData ability, Character actor, IList<Character> targets)
        {
            var gm = GameManager.Instance;
            var ctx = new ActionContext
            {
                game = gm,
                battle = gm.BattleHandler,
                actor = actor,
                targets = targets ?? new List<Character>(),
                ability = ability
            };

            if (ability.conditions != null)
            {
                foreach (ConditionData c in ability.conditions)
                {
                    if (c != null && !c.Evaluate(ctx, out string reason))
                    {
                        Debug.Log($"Ability blocked: {reason}");
                        yield break;
                    }
                }
            }

            if (ability.effects != null)
            {
                foreach (EffectData e in ability.effects)
                    if (e != null)
                        yield return StartCoroutine(e.Execute(ctx));
            }
        }
    }
}