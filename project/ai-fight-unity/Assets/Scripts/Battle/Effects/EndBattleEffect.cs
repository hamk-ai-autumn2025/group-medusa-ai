using System.Collections;
using UnityEngine;

namespace dev.susybaka.TurnBasedGame.Battle.Data
{
    [CreateAssetMenu(menuName = "Turn Based Game/Battles/Effects/End Battle")]
    public class EndBattleEffect : EffectData
    {
        public override IEnumerator Execute(ActionContext ctx)
        {
            ctx.battle.TurnSystem.EndCombat();

            ctx.battle.battleWindow.PartyMembers.RefreshUI();
            yield break;
        }
    }
}