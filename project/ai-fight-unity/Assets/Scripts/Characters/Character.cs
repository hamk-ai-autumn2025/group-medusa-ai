using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using dev.susybaka.TurnBasedGame.Battle.Data;
using dev.susybaka.TurnBasedGame.Characters.Data;
using dev.susybaka.TurnBasedGame.Items;
using dev.susybaka.Shared.Attributes;
using dev.susybaka.Shared.Audio;
using dev.susybaka.TurnBasedGame.Battle;
using dev.susybaka.TurnBasedGame.Globals;

namespace dev.susybaka.TurnBasedGame.Characters
{
    public class Character : MonoBehaviour
    {
        public CharacterData data;

        [Header("Character")]
        [Min(1)] public int level = 1;
        public int health = 100;
        [Min(1)] public int maxHealth = 100;
        public int mana = 100;
        [Min(1)] public int maxMana = 100;
        public int ActionPoints => party != null ? party.Points : 0;
        public int MaxActionPoints => party != null ? party.MaxPoints : 0;
        [Min(1)] public Stat attackPower = new Stat("attackPower", 1, 999);
        [Min(0)] public Stat defense = new Stat("defense", 1, 999);
        public bool isAlive = true;
        public bool isFighting = false;
        protected bool wasFighting = false;
        public Flag isSilenced = new Flag("isSilenced", new List<Flag.Value> { new Flag.Value("base", false) }, FlagAggregateLogic.AllTrue);
        public AbilityData[] KnownAbilities;
        public AbilityData[] KnownSpells;
        public InventoryData InventoryData => data.inventory;
        public Inventory Inventory => InventoryHandler.Get(InventoryData);
        [SoundName] public string damageSound = "<None>";

        private Party party;
        private SpriteRenderer[] renderers;
        private Coroutine ieSpriteHitEffect;
        private List<StatusEffect> statusEffects = new List<StatusEffect>();

        private List<KnowledgeEntry> knowledge = new List<KnowledgeEntry>();
        public List<KnowledgeEntry> Knowledge => knowledge;

        protected virtual void Awake()
        {
            health = maxHealth;
            mana = maxMana;
            isAlive = true;
            renderers = GetComponentsInChildren<SpriteRenderer>(true);
            ieSpriteHitEffect = null;
        }

        public virtual void Initialize(Party party = null)
        {
            if (party == null)
                return;

            this.party = party;
        }

        public virtual void ModifyHealth(int damage)
        {
            // If receiving damage (damage is negative), apply defense-based reduction
            if (damage < 0)
            {
                // Only apply damage if character is alive
                if (!isAlive)
                    return;

                int reducedDamage = damage + defense.Value;
                // Ensure at least 1 damage is taken if damage is still negative after reduction
                if (reducedDamage < 0)
                    damage = reducedDamage;
                else
                    damage = -1;
            }

            this.LogV(("damage", damage));

            health += damage;

            isAlive = health > 0;
            
            if (health > maxHealth)
            {
                health = maxHealth;
            }
        }

        public virtual void ModifyMana(int amount)
        {
            mana += amount;
            if (mana < 0)
            {
                mana = 0;
            }
            else if (mana > maxMana)
            {
                mana = maxMana;
            }
        }

        public virtual void ModifyActionPoints(int amount)
        {
            if (party == null)
                return;

            party.ModifyPoints(amount);
        }

        public virtual void UpdateTurnState(int turn)
        {
            // Tick down status effects in reverse order to allow safe removal during iteration
            for (int i = statusEffects.Count - 1; i >= 0; i--)
            {
                statusEffects[i].Tick();
            }
        }

        public virtual void AddStatusEffect(StatusEffectContext ctx)
        {
            if (ctx.data == null)
                return;

            // Check if the effect already exists
            for (int i = 0; i < statusEffects.Count; i++)
            {
                StatusEffect e = statusEffects[i];

                if (e.data == ctx.data)
                {
                    // If it allows refresh, reset duration
                    if (ctx.data.allowRefresh)
                    {
                        e.Refresh();
                    }
                    // If it allows stacking, increase stacks
                    if (ctx.data.maxStacks > 1)
                    {
                        e.AddStacks(ctx.stacks);
                    }
                    e.Apply(ctx);
                    return;
                }
            }
            // If not found, add new effect
            StatusEffect newEffect = new StatusEffect(ctx.data, ctx.duration + 1, ctx.stacks);
            newEffect.Apply(ctx);
            statusEffects.Add(newEffect);
        }

        public virtual void RemoveStatusEffect(StatusEffect statusEffect)
        {
            RemoveStatusEffect(statusEffect.data);
        }

        public virtual void RemoveStatusEffect(StatusEffectData statusEffectData)
        {
            for (int i = 0; i < statusEffects.Count; i++)
            {
                if (statusEffects[i].data == statusEffectData)
                {
                    statusEffects[i].Remove();
                    statusEffects.RemoveAt(i);
                    return;
                }
            }
        }

        public virtual void ClearStatusEffects(EffectType type = EffectType.none)
        {
            for (int i = statusEffects.Count - 1; i >= 0; i--)
            {
                if (type == EffectType.none || statusEffects[i].data.type == type)
                {
                    RemoveStatusEffect(statusEffects[i].data);
                }
            }
        }

        public virtual bool HasStatusEffect(StatusEffect statusEffect)
        {
            return HasStatusEffect(statusEffect.data);
        }

        public virtual bool HasStatusEffect(StatusEffectData data)
        {
            for (int i = 0; i < statusEffects.Count; i++)
            {
                StatusEffect e = statusEffects[i];
                if (e.data == data)
                    return true;
            }
            return false;
        }

        public StatusEffect[] GetStatusEffects()
        {
            return statusEffects.ToArray();
        }

        public virtual void LearnKnowledge(KnowledgeEntry entry)
        {
            if (string.IsNullOrEmpty(entry.name) || knowledge.Contains(entry))
                return;

            if (knowledge != null && knowledge.Count > 0)
            {
                for (int i = 0; i < knowledge.Count; i++)
                {
                    if (knowledge[i].name == entry.name)
                        return;
                }
            }

            knowledge.Add(entry);
        }

        // Visual and audio effects
        public virtual void DamageEffect()
        {
            if (AudioManager.Instance != null)
                AudioManager.Instance.Play(damageSound);
            
            if (renderers != null && renderers.Length > 0 && ieSpriteHitEffect == null)
                ieSpriteHitEffect = StartCoroutine(IE_SpriteHitEffect());
        }

        public virtual void HealEffect()
        {

        }

        protected IEnumerator IE_SpriteHitEffect()
        {
            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].material.SetFloat("_HitEffectBlend", 1f);
            }
            yield return new WaitForSeconds(0.5f);
            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].material.SetFloat("_HitEffectBlend", 0f);
            }
            ieSpriteHitEffect = null;
        }
    }
}