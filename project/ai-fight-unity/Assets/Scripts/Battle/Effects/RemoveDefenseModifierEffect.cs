using System.Collections;
using UnityEngine;

namespace dev.susybaka.TurnBasedGame.Battle.Data
{
    [CreateAssetMenu(menuName = "Turn Based Game/Battles/Effects/Remove Defense Modifier")]
    public class RemoveDefenseModifierEffect : EffectData
    {
        public string modifierName = string.Empty;

        public override IEnumerator Execute(ActionContext ctx)
        {
            foreach (var t in ctx.targets)
            {
                t.defense.Remove(string.IsNullOrEmpty(modifierName) ? ctx.ability.name : modifierName);
                // TODO: VFX/SFX hook
            }
            yield break;
        }
    }
}