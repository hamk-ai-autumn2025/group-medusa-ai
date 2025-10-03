using System.Collections;
using UnityEngine;

namespace dev.susybaka.TurnBasedGame.Battle.Data
{
    [CreateAssetMenu(menuName = "Turn Based Game/Battles/Effects/Learn Knowledge")]
    public class LearnKnowledgeEffect : EffectData
    {
        public bool isCharactersStats = false;
        [NaughtyAttributes.HideIf(nameof(isCharactersStats))] public string id = string.Empty;
        [NaughtyAttributes.HideIf(nameof(isCharactersStats))] public string knowledge = string.Empty;

        public override IEnumerator Execute(ActionContext ctx)
        {
            if (isCharactersStats)
            {
                for (int i = 0; i < ctx.targets.Count; i++)
                {
                    ctx.actor?.LearnKnowledge(new Characters.KnowledgeEntry($"Character_{ctx.targets[i].data.name}_Stats", $"maxHealth={ctx.targets[i].maxHealth}, currentHealth={ctx.targets[i].health}, maxMana={ctx.targets[i].maxMana}, currentMana={ctx.targets[i].mana}, maxSpecialActionPoints={ctx.targets[i].MaxActionPoints}, currentSpecialActionPoints={ctx.targets[i].ActionPoints}, currentDefense={ctx.targets[i].defense.Value}, currentAttackPower={ctx.targets[i].attackPower.Value}"));
                }
            }
            else if (!string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(knowledge))
            {
                ctx.actor?.LearnKnowledge(new Characters.KnowledgeEntry(id, knowledge));
            }
            yield break;
        }
    }
}