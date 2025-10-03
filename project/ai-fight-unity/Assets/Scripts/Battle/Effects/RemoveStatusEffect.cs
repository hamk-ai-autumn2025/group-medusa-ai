using System.Collections;
using UnityEngine;

namespace dev.susybaka.TurnBasedGame.Battle.Data
{
    [CreateAssetMenu(menuName = "Turn Based Game/Battles/Effects/Remove Status Effect")]
    public class RemoveStatusEffect : EffectData
    {
        public bool removeAll = false;
        [NaughtyAttributes.ShowIf(nameof(removeAll))] public EffectType removeType = EffectType.none;
        [NaughtyAttributes.HideIf(nameof(removeAll))] public StatusEffectData statusEffect;

        public override IEnumerator Execute(ActionContext ctx)
        {
            foreach (var t in ctx.targets)
            {
                if (removeAll)
                    t.ClearStatusEffects(removeType);
                else if (statusEffect != null)
                    t.RemoveStatusEffect(statusEffect);
                // TODO: VFX/SFX hook
            }
            yield break;
        }
    }
}