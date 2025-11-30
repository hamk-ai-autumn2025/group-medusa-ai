using System.Collections;
using System.Collections.Generic;
using dev.susybaka.Shared.Attributes;
using dev.susybaka.Shared.Audio;
using dev.susybaka.TurnBasedGame.Battle;
using dev.susybaka.TurnBasedGame.Battle.Data;
using dev.susybaka.TurnBasedGame.Characters.Data;
using dev.susybaka.TurnBasedGame.Globals;
using dev.susybaka.TurnBasedGame.Items;
using UnityEngine;

namespace dev.susybaka.TurnBasedGame.Characters
{
    public class Character : MonoBehaviour
    {
        public CharacterData data;
        public string Id => data != null ? data.name : string.Empty;
        private Party party;
        public Party Party => party;
        protected Transform characterTransform;
        public Transform CharacterTransform => characterTransform;
        protected Animator animator;

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
        public bool isSprinting = false;
        public bool isFighting = false;
        protected bool wasFighting = false;
        public Flag isSilenced = new Flag("isSilenced", new List<Flag.Value> { new Flag.Value("base", false) }, FlagAggregateLogic.AllTrue);
        public Flag allowHealing = new Flag("allowHealing", new List<Flag.Value> { new Flag.Value("base", true) }, FlagAggregateLogic.AllTrue);
        public AbilityData[] KnownAbilities;
        public AbilityData[] KnownSpells;
        public InventoryData InventoryData => data.inventory;
        public Inventory Inventory => InventoryHandler.Get(InventoryData);
        [SoundName] public string damageSound = "<None>";
        [SoundName] public string deathSound = "<None>";
        public Transform visualEffectParent;

        protected SpriteRenderer[] renderers;
        private Coroutine ieSpriteHitEffect;
        private List<StatusEffect> statusEffects = new List<StatusEffect>();
        private Dictionary<string, GameObject> activeVisualEffects = new Dictionary<string, GameObject>();

        private List<KnowledgeBank> knowledgeBanks = new List<KnowledgeBank>();
        public List<KnowledgeBank> KnowledgeBanks => knowledgeBanks;
        private bool hasDied = false;

#if UNITY_EDITOR
        [NaughtyAttributes.Button("Log Knowledge Banks")]
        public void LogKnowledgeBank()
        {
            if (knowledgeBanks == null || knowledgeBanks.Count < 1)
            {
                this.LogC("Knowledge Banks are not available");
                return;
            }
            for (int i = 0; i < knowledgeBanks.Count; i++)
            {
                if (knowledgeBanks[i].Count == 0)
                {
                    this.LogC($"Knowledge Bank {i} ({knowledgeBanks[i].id}) is empty");
                    return;
                }
                this.LogV(($"knowledgeBanks[{i}].Count", knowledgeBanks[i].Count));
                foreach (KnowledgeEntry entry in knowledgeBanks[i])
                {
                    this.LogV(($"knowledgeBank[{i}] Entry Name", entry.name), ($"knowledgeBank[{i}] Entry Value", entry.text));
                }
            }
        }
#endif

        protected virtual void Awake()
        {
            health = maxHealth;
            mana = maxMana;
            isAlive = true;
            renderers = GetComponentsInChildren<SpriteRenderer>(true);
            ieSpriteHitEffect = null;
            characterTransform = this.transform;
            animator = characterTransform.GetComponentInChildren<Animator>(true);

            //Debug.Log("Character Awake: " + Id);
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
            else if (damage > 0) // If receiving healing (damage is positive), prevent healing if not allowed
            {
                if (!allowHealing.value)
                    return;
            }

            health += damage;

            isAlive = health > 0;
            
            if (animator != null)
            {
                animator.SetBool("isAlive", isAlive);
            }

            if (health > maxHealth)
            {
                health = maxHealth;
            }

            if (!isAlive)
            {
                DeathEffect();
            }
            else
            {
                hasDied = false;
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

        public virtual bool HasKnowledgeBank(string bankId)
        {
            if (string.IsNullOrEmpty(bankId))
                return false;

            for (int i = 0; i < knowledgeBanks.Count; i++)
            {
                KnowledgeBank bank = knowledgeBanks[i];
                if (bank.id == bankId)
                    return true;
            }
            return false;
        }

        public virtual void EraseKnowledgeBank(string bankId)
        {
            if (string.IsNullOrEmpty(bankId))
                return;

            for (int i = knowledgeBanks.Count - 1; i >= 0; i--)
            {
                KnowledgeBank bank = knowledgeBanks[i];
                if (bank.id != bankId)
                    continue;
                bank.Clear();
                knowledgeBanks.RemoveAt(i);
                return;
            }
        }

        public virtual void LearnKnowledge(string bankId, KnowledgeEntry entry)
        {
            if (string.IsNullOrEmpty(bankId))
                return;

            for (int i = 0; i < knowledgeBanks.Count; i++)
            {
                KnowledgeBank bank = knowledgeBanks[i];

                if (bank.id != bankId)
                    continue;

                if (string.IsNullOrEmpty(entry.name) || bank.Contains(entry))
                    return;

                // Check for duplicate by name
                foreach (KnowledgeEntry knownEntry in bank)
                {
                    if (knownEntry.name == entry.name)
                        return;
                }

                bank.Add(entry);
            }
        }

        // Visual and audio effects
        public virtual void DamageEffect(int value)
        {
            if (characterTransform == null)
            {
                characterTransform = this.transform;
            }

            if (value != 0)
            {
                if (AudioManager.Instance != null)
                    AudioManager.Instance.Play(damageSound);

                if (renderers != null && renderers.Length > 0 && ieSpriteHitEffect == null)
                    ieSpriteHitEffect = StartCoroutine(IE_SpriteHitEffect());

                GameManager.Instance.BattleHandler.battleWindow.PopupWindow.SpawnTextPopup(characterTransform, Vector2.zero, $"<color=red>{Mathf.Abs(value)}</color>");
                GameManager.Instance.BattleHandler.battleWindow.PopupWindow.SpawnSliderPopup(characterTransform, Vector2.zero, health, maxHealth);

                // Trigger hit animation
                if (animator != null)
                {
                    animator.SetTrigger("hit");
                }
            }
            else
            {
                // For now we just show a "MISS" popup
                // Additional miss effects can be added here later
                GameManager.Instance.BattleHandler.battleWindow.PopupWindow.SpawnTextPopup(characterTransform, Vector2.zero, $"<color=white>MISS</color>");
            }
        }

        public virtual void HealEffect(int value)
        {
            if (characterTransform == null)
            {
                characterTransform = this.transform;
            }

            if (value != 0)
            {
                GameManager.Instance.BattleHandler.battleWindow.PopupWindow.SpawnTextPopup(characterTransform, Vector2.zero, $"<color=green>{value}</color>");
                GameManager.Instance.BattleHandler.battleWindow.PopupWindow.SpawnSliderPopup(characterTransform, Vector2.zero, health, maxHealth);
            }
            else
            {
                // For now we just show a "MISS" popup
                // Additional miss effects can be added here later
                GameManager.Instance.BattleHandler.battleWindow.PopupWindow.SpawnTextPopup(characterTransform, Vector2.zero, $"<color=white>MISS</color>");
            }
        }

        public virtual void DeathEffect()
        {
            if (characterTransform == null)
            {
                characterTransform = this.transform;
            }

            if (hasDied)
                return;

            if (AudioManager.Instance != null)
                AudioManager.Instance.Play(deathSound);

            hasDied = true;
        }

        public virtual void SpawnVisualEffect(GameObject effectPrefab)
        {
            if (effectPrefab == null || visualEffectParent == null)
                return;

            if (!activeVisualEffects.ContainsKey(effectPrefab.name))
            {
                GameObject spawned = Instantiate(effectPrefab, visualEffectParent);
                activeVisualEffects.Add(effectPrefab.name, spawned);
            }
        }

        public virtual void RemoveVisualEffect(GameObject effectPrefab)
        {
            if (effectPrefab == null || visualEffectParent == null)
                return;

            if (activeVisualEffects.ContainsKey(effectPrefab.name))
            {
                GameObject toRemove = activeVisualEffects[effectPrefab.name];
                activeVisualEffects.Remove(effectPrefab.name);
                Destroy(toRemove);
            }
        }

        protected IEnumerator IE_SpriteHitEffect()
        {
            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].material.SetFloat("_HitEffectBlend", 1f);
                renderers[i].material.SetFloat("_ShakeAmount", 1f);
            }
            yield return new WaitForSeconds(0.5f);
            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].material.SetFloat("_HitEffectBlend", 0f);
                renderers[i].material.SetFloat("_ShakeAmount", 0f);
            }
            ieSpriteHitEffect = null;
        }
    }
}