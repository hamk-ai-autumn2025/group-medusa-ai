using System.Collections.Generic;
using UnityEngine;
using dev.susybaka.TurnBasedGame.Dialogue.Data;
using dev.susybaka.TurnBasedGame.Minigame.Data;

namespace dev.susybaka.TurnBasedGame.Battle.Data
{
    [CreateAssetMenu(menuName = "Turn Based Game/Battles/Ability")]
    public class AbilityData : ScriptableObject
    {
        [Header("Ability")]
        public string displayName;
        [TextArea] public string description;
        public string[] tags;

        public bool requiresTarget = true;
        public TargetGroup targetGroup = TargetGroup.enemy;
        public float attackSpeed = 3f;
        public float minimumDamageModifier = 0.5f;
        public float critDamageModifier = 1.5f;
        public float minimumAccuracyModifier = 0.8f;

        [Header("Default Conditions and Effects")]
        public bool consumesMana = false;
        [NaughtyAttributes.ShowIf(nameof(consumesMana))] public int consumedMana = 0;
        public bool restoresMana = false;
        [NaughtyAttributes.ShowIf(nameof(restoresMana))] public int restoredMana = 0;

        public bool consumesActionPoints = false;
        [NaughtyAttributes.ShowIf(nameof(consumesActionPoints))] public int consumedActionPoints = 0;
        public bool restoresActionPoints = false;
        [NaughtyAttributes.ShowIf(nameof(restoresActionPoints))] public int restoredActionPoints = 0;

        public bool dealsDamage = false;
        [NaughtyAttributes.ShowIf(nameof(dealsDamage))] public int amountDamage = 0;
        public bool isDefensive = false;
        public bool isSpell = false;
        public bool isHeal = false;
        public bool isAttack = false;
        public bool opensActMenu = false;

        public List<ConditionData> conditions;
        public List<EffectData> effects;

        public MinigameData minigame;
        public DialogueData dialogueOnUse;

#if UNITY_EDITOR
        [NaughtyAttributes.Button("Refresh Default Conditions and Effects")]
        public void RefreshDefaults()
        {
            if (dealsDamage)
            {
                AddOrUpdateSubAsset<DealDamageEffect>(
                    effects,
                    e => e is DealDamageEffect,
                    e => ((DealDamageEffect)e).amount = amountDamage,
                    $"DealDamage{this.name}"
                );
            }
            else
            {
                RemoveSubAssets<EffectData>(effects, e => e is DealDamageEffect && e.name.EndsWith(this.name));
            }

            if (consumesMana)
            {
                AddOrUpdateSubAsset<ModifyManaEffect>(
                    effects,
                    e => e is ModifyManaEffect,
                    e => {
                        ((ModifyManaEffect)e).amount = consumedMana;
                        ((ModifyManaEffect)e).preTurn = true;
                    },
                    $"ConsumeMana{this.name}"
                );
                AddOrUpdateSubAsset<HasManaCondition>(
                    conditions,
                    c => c is HasManaCondition,
                    c => {
                        ((HasManaCondition)c).manaCost = restoredMana;
                        ((HasManaCondition)c).preTurn = true;
                    },
                    $"HasMana{this.name}"
                );
            }
            else
            {
                RemoveSubAssets<EffectData>(effects, e => e is ModifyManaEffect && e.name.StartsWith("Consume") && e.name.EndsWith(this.name));
                RemoveSubAssets<ConditionData>(conditions, c => c is HasManaCondition && c.name.StartsWith("Has") && c.name.EndsWith(this.name));
            }

            if (restoresMana)
            {
                AddOrUpdateSubAsset<ModifyManaEffect>(
                    effects,
                    e => e is ModifyManaEffect,
                    e => ((ModifyManaEffect)e).amount = restoredMana,
                    $"RestoreMana{this.name}"
                );
            }
            else
            {
                RemoveSubAssets<EffectData>(effects, e => e is ModifyManaEffect && e.name.StartsWith("Restore") && e.name.EndsWith(this.name));
            }

            if (consumesActionPoints)
            {
                AddOrUpdateSubAsset<ModifyActionPointsEffect>(
                    effects,
                    e => e is ModifyActionPointsEffect,
                    e => { 
                        ((ModifyActionPointsEffect)e).amount = consumedActionPoints;
                        ((ModifyActionPointsEffect)e).preTurn = true;
                    },
                    $"ConsumeActionPoints{this.name}"
                );
                AddOrUpdateSubAsset<HasActionPointsCondition>(
                    conditions,
                    c => c is HasActionPointsCondition,
                    c => {
                        ((HasActionPointsCondition)c).actionPointCost = consumedActionPoints;
                        ((HasActionPointsCondition)c).preTurn = true;
                    },
                    $"HasActionPoints{this.name}"
                );
            }
            else
            {
                RemoveSubAssets<EffectData>(effects, e => e is ModifyActionPointsEffect && e.name.StartsWith("Consume") && e.name.EndsWith(this.name));
                RemoveSubAssets<ConditionData>(conditions, c => c is HasActionPointsCondition && c.name.StartsWith("Has") && c.name.EndsWith(this.name));
            }

            if (restoresActionPoints)
            {
                AddOrUpdateSubAsset<ModifyActionPointsEffect>(
                    effects,
                    e => e is ModifyActionPointsEffect,
                    e => ((ModifyActionPointsEffect)e).amount = restoredActionPoints,
                    $"RestoreActionPoints{this.name}"
                );
            }
            else
            {
                RemoveSubAssets<EffectData>(effects, e => e is ModifyActionPointsEffect && e.name.StartsWith("Restore") && e.name.EndsWith(this.name));
            }

            UnityEditor.AssetDatabase.SaveAssets();
        }

        private T AddOrUpdateSubAsset<T>(List<EffectData> list, System.Predicate<EffectData> match, System.Action<T> update, string name) where T : EffectData
        {
            EffectData item = list.Find(match);
            if (item == null)
            {
                item = ScriptableObject.CreateInstance<T>();
                item.name = name;
                update((T)item);
                list.Add(item);
                UnityEditor.AssetDatabase.AddObjectToAsset(item, this);
            }
            else
            {
                update((T)item);
                UnityEditor.EditorUtility.SetDirty(item);
            }
            return (T)item;
        }

        private T AddOrUpdateSubAsset<T>(List<ConditionData> list, System.Predicate<ConditionData> match, System.Action<T> update, string name) where T : ConditionData
        {
            ConditionData item = list.Find(match);
            if (item == null)
            {
                item = ScriptableObject.CreateInstance<T>();
                item.name = name;
                update((T)item);
                list.Add(item);
                UnityEditor.AssetDatabase.AddObjectToAsset(item, this);
            }
            else
            {
                update((T)item);
                UnityEditor.EditorUtility.SetDirty(item);
            }
            return (T)item;
        }

        private void RemoveSubAssets<T>(List<EffectData> list, System.Predicate<EffectData> match) where T : EffectData
        {
            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (match(list[i]))
                {
                    UnityEditor.AssetDatabase.RemoveObjectFromAsset(list[i]);
                    list.RemoveAt(i);
                }
            }
        }

        private void RemoveSubAssets<T>(List<ConditionData> list, System.Predicate<ConditionData> match) where T : ConditionData
        {
            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (match(list[i]))
                {
                    UnityEditor.AssetDatabase.RemoveObjectFromAsset(list[i]);
                    list.RemoveAt(i);
                }
            }
        }

        [NaughtyAttributes.Button("Delete All Sub-Assets")]
        public void DeleteAllSubAssets()
        {
            // Helper to check if an object is a sub-asset of this ScriptableObject
            bool IsSubAsset(Object obj)
            {
                string assetPath = UnityEditor.AssetDatabase.GetAssetPath(this);
                Object[] subAssets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(assetPath);
                foreach (var subAsset in subAssets)
                {
                    if (subAsset == obj && subAsset != this)
                        return true;
                }
                return false;
            }

            // Remove sub-assets from effects
            for (int i = effects.Count - 1; i >= 0; i--)
            {
                var effect = effects[i];
                if (effect != null && IsSubAsset(effect))
                {
                    UnityEditor.AssetDatabase.RemoveObjectFromAsset(effect);
                    effects.RemoveAt(i);
                }
            }

            // Remove sub-assets from conditions
            for (int i = conditions.Count - 1; i >= 0; i--)
            {
                var condition = conditions[i];
                if (condition != null && IsSubAsset(condition))
                {
                    UnityEditor.AssetDatabase.RemoveObjectFromAsset(condition);
                    conditions.RemoveAt(i);
                }
            }

            UnityEditor.AssetDatabase.SaveAssets();
        }
#endif
    }
}