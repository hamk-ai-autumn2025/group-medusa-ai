using System.Collections;
using UnityEngine;

namespace dev.susybaka.TurnBasedGame.Battle.Data
{
    [CreateAssetMenu(menuName = "Turn Based Game/Battles/Effects/Add Defense Modifier")]
    public class AddDefenseModifierEffect : EffectData
    {
        [Min(1)] public int amount = 1;
        public string modifierName = string.Empty;

        public override IEnumerator Execute(ActionContext ctx)
        {
            foreach (var t in ctx.targets)
            {
                this.LogV(("t", t.gameObject.name), ("amount", amount), ("modifierName", modifierName));
                t.defense.Add(string.IsNullOrEmpty(modifierName) ? ctx.ability.name : modifierName, amount);
                // TODO: VFX/SFX hook
            }
            yield break;
        }
    }
}