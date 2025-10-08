using System.Collections;
using UnityEngine;

namespace dev.susybaka.TurnBasedGame.Battle.Data
{
    [CreateAssetMenu(menuName = "Turn Based Game/Battles/Effects/Add Attack Modifier")]
    public class AddAttackModifierEffect : EffectData
    {
        [Min(1)] public int amount = 1;
        public string modifierName = string.Empty;

        public override IEnumerator Execute(ActionContext ctx)
        {
            foreach (var t in ctx.targets)
            {
                t.attackPower.Add(string.IsNullOrEmpty(modifierName) ? ctx.ability.name : modifierName, amount);
                // TODO: VFX/SFX hook
            }
            yield break;
        }
    }
}