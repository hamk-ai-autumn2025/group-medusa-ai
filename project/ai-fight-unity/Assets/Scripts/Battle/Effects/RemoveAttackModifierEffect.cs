using System.Collections;
using UnityEngine;

namespace dev.susybaka.TurnBasedGame.Battle.Data
{
    [CreateAssetMenu(menuName = "Turn Based Game/Battles/Effects/Remove Attack Modifier")]
    public class RemoveAttackModifierEffect : EffectData
    {
        public string modifierName = string.Empty;

        public override IEnumerator Execute(ActionContext ctx)
        {
            foreach (var t in ctx.targets)
            {
                t.attackPower.Remove(string.IsNullOrEmpty(modifierName) ? ctx.ability.name : modifierName);
                // TODO: VFX/SFX hook
            }
            yield break;
        }
    }
}