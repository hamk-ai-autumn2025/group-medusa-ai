using System.Collections;
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
            foreach (var t in ctx.targets)
            {
                //this.LogV(("t", t.gameObject.name));
                t.AddStatusEffect(new StatusEffectContext(ctx.game, ctx.battle, statusEffect, ctx.actor, ctx.targets, ctx.ability, duration, stacks));
                // TODO: VFX/SFX hook
            }
            yield break;
        }
    }
}