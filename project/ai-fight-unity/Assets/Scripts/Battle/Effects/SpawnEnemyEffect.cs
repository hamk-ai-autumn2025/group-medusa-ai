using System.Collections;
using dev.susybaka.TurnBasedGame.Enemies;
using UnityEngine;

namespace dev.susybaka.TurnBasedGame.Battle.Data
{
    [CreateAssetMenu(menuName = "Turn Based Game/Battles/Effects/Spawn Enemy Effect")]
    public class SpawnEnemyEffect : EffectData
    {
        public EnemyCharacter enemyPrefab;

        public override IEnumerator Execute(ActionContext ctx)
        {
            if (ctx.battle.enemies.HasMember(enemyPrefab.data))
            {
                Debug.Log("Enemy already exists, not spawning another.");
                yield break; // Enemy already exists, do not spawn again
            }

            Vector3 location = Vector3.zero;

            if (ctx.battle.enemies.members != null && ctx.battle.enemies.members.Count > 0 && ctx.battle.battleEnemyLocations.Length >= ctx.battle.enemies.members.Count)
            {
                location = ctx.battle.battleEnemyLocations[ctx.battle.enemies.members.Count].position;
            }

            ctx.battle.enemies.AddMember(enemyPrefab, location);
            
            for (int i = 0; i < ctx.battle.enemies.members.Count; i++)
            {
                ctx.battle.enemies.members[i].Initialize(ctx.battle.enemies);
            }

            yield break;
        }
    }
}