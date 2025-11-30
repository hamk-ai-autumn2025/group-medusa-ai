using System.Collections;
using dev.susybaka.TurnBasedGame.Battle.Data;
using UnityEngine;

namespace dev.susybaka.TurnBasedGame.Battle
{
    public class AbilitySystem : MonoBehaviour
    {
        [SerializeField] private bool log = false;

        public IEnumerator Run(ActionContext ctx)
        {
            if (!Conditions(ctx, true))
                yield break;

            if (ctx.ability.effects != null)
            {
                foreach (EffectData e in ctx.ability.effects)
                    if (e != null && !e.preTurn)
                        yield return StartCoroutine(e.Execute(ctx));
            }
        }

        public bool Conditions(ActionContext ctx, bool postTurn = false)
        {
            if (ctx.ability.conditions != null && ctx.ability.conditions.Count > 0)
            {
                if (log)
                    Debug.Log($"Checking {ctx.ability.conditions.Count} conditions for '{ctx.ability.displayName}'");
                foreach (ConditionData c in ctx.ability.conditions)
                {
                    string cname = c != null ? c.name : "null";

                    if (c == null || (c.preTurn && !c.postTurn && postTurn) || (c.postTurn && !c.preTurn && !postTurn))
                    {
                        if (log)
                        {
                            string cPhase = c != null ? (c.preTurn ? "pre-turn" : "post-turn") : "null";
                            string phase = postTurn ? "post-turn" : "pre-turn";
                            Debug.Log($"{cPhase} condition '{cname}' skipped for '{ctx.ability.displayName}' during {phase}");
                        }
                        continue;
                    }
                    if (log)
                        Debug.Log($"Evaluating condition '{cname}' for '{ctx.ability.displayName}'");
                    if (c != null && !c.Evaluate(ctx, out string reason))
                    {
                        if (log)
                            Debug.Log($"Ability '{ctx.ability.displayName}' blocked: {reason}");
                        return false;
                    }
                }
            }
            if (log)
                Debug.Log($"All conditions met for '{ctx.ability.displayName}'");
            return true;
        }
    }
}