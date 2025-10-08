using System.Collections;
using UnityEngine;

namespace dev.susybaka.TurnBasedGame.Battle.Data
{
    [CreateAssetMenu(menuName = "Turn Based Game/Battles/Effects/Modify Mana")]
    public class ModifyManaEffect : EffectData
    {
        public int amount = 1;

        public override IEnumerator Execute(ActionContext ctx)
        {
            ctx.actor?.ModifyMana(amount);
            yield break;
        }
    }
}