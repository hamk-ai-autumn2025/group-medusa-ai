using System.Collections;
using UnityEngine;
using dev.susybaka.TurnBasedGame.Characters;

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
            if (ctx.actor == null)
            {
                Debug.LogError("ctx.actor missing!");
            }

            if (isCharactersStats)
            {
                for (int i = 0; i < ctx.targets.Count; i++)
                {
                    Character t = ctx.targets[i];
                    
                    if (ctx.actor.HasKnowledgeBank(t.data.name))
                        ctx.actor.EraseKnowledgeBank(t.data.name); // reset knowledge bank for this character to allow updating info

                    // Learn these stats about the target character for now
                    ctx.actor.LearnKnowledge(t.data.name, new KnowledgeEntry(nameof(t.health), t.health.ToString()));
                    ctx.actor.LearnKnowledge(t.data.name, new KnowledgeEntry(nameof(t.maxHealth), t.maxHealth.ToString()));
                    ctx.actor.LearnKnowledge(t.data.name, new KnowledgeEntry(nameof(t.mana), t.mana.ToString()));
                    ctx.actor.LearnKnowledge(t.data.name, new KnowledgeEntry(nameof(t.maxMana), t.maxMana.ToString()));
                    ctx.actor.LearnKnowledge(t.data.name, new KnowledgeEntry(nameof(t.ActionPoints), t.ActionPoints.ToString()));
                    ctx.actor.LearnKnowledge(t.data.name, new KnowledgeEntry(nameof(t.MaxActionPoints), t.MaxActionPoints.ToString()));
                    ctx.actor.LearnKnowledge(t.data.name, new KnowledgeEntry(nameof(t.attackPower), t.attackPower.Value.ToString()));
                    ctx.actor.LearnKnowledge(t.data.name, new KnowledgeEntry(nameof(t.defense), t.defense.Value.ToString()));
                }
            }
            else if (!string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(knowledge))
            {
                ctx.actor.LearnKnowledge("#Lore", new KnowledgeEntry(id, knowledge));
            }
            yield break;
        }
    }
}