using System.Collections;
using System.Collections.Generic;
using dev.susybaka.TurnBasedGame.Characters;
using UnityEngine;

namespace dev.susybaka.TurnBasedGame.Battle.Data
{
    [CreateAssetMenu(menuName = "Turn Based Game/Battles/Effects/Apply Status Effect")]
    public class ApplyStatusEffect : EffectData
    {
        public StatusEffectData statusEffect;
        public int duration = 1;
        public int stacks = 1;

        public override IEnumerator Execute(ActionContext ctx)
        {
            if (ctx.targets == null || ctx.targets.Count < 1)
            {
                List<Character> l = new List<Character>();
                l.Add(ctx.actor);

                ctx.actor.AddStatusEffect(new StatusEffectContext(ctx.game, ctx.battle, statusEffect, ctx.actor, l, ctx.ability, duration, stacks));
            }
            else
            {
                foreach (var t in ctx.targets)
                {
                    //this.LogV(("t", t.gameObject.name));
                    t.AddStatusEffect(new StatusEffectContext(ctx.game, ctx.battle, statusEffect, ctx.actor, ctx.targets, ctx.ability, duration, stacks));
                    // TODO: VFX/SFX hook
                }
            }
            yield break;
        }
    }
}