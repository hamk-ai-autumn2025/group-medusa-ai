using System.Collections;
using dev.susybaka.TurnBasedGame.Characters.Data;
using UnityEngine;

namespace dev.susybaka.TurnBasedGame.Battle.Data
{
    [CreateAssetMenu(menuName = "Turn Based Game/Battles/Effects/Remove Enemy Effect")]
    public class RemoveEnemyEffect : EffectData
    {
        public CharacterData enemy;

        public override IEnumerator Execute(ActionContext ctx)
        {
            if (!ctx.battle.enemies.HasMember(enemy))
            {
                yield break; // Enemy does not exist
            }

            GameObject e = ctx.battle.enemies.GetMember(enemy).gameObject;
            ctx.battle.enemies.RemoveMember(enemy);
            Destroy(e);
            yield break;
        }
    }
}