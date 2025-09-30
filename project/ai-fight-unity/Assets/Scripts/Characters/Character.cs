using UnityEngine;
using dev.susybaka.TurnBasedGame.Battle.Data;
using dev.susybaka.TurnBasedGame.Characters.Data;
using dev.susybaka.TurnBasedGame.Items;
using dev.susybaka.Shared.Attributes;
using dev.susybaka.Shared.Audio;
using System.Collections;

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
        [Min(1)] public int attackPower = 1;
        [Min(0)] public int defense = 1;
        public bool isAlive = true;
        public bool isFighting = false;
        protected bool wasFighting = false;
        public AbilityData[] KnownAbilities;
        public AbilityData[] KnownSpells;
        public InventoryData InventoryData => data.inventory;
        public Inventory Inventory => InventoryHandler.Get(InventoryData);
        [SoundName] public string damageSound = "<None>";

        private SpriteRenderer[] renderers;
        private Coroutine ieSpriteHitEffect;

        protected virtual void Awake()
        {
            health = maxHealth;
            mana = maxMana;
            isAlive = true;
            renderers = GetComponentsInChildren<SpriteRenderer>(true);
            ieSpriteHitEffect = null;
        }

        public virtual void ModifyHealth(int damage)
        {
            // If receiving damage (damage is negative), apply defense-based reduction
            if (damage < 0)
            {
                // Only apply damage if character is alive
                if (!isAlive)
                    return;

                int reducedDamage = damage + defense;
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