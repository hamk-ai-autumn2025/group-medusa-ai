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

        public List<ConditionData> conditions;
        public List<EffectData> effects;

        public MinigameData minigame;
        public DialogueData dialogueOnUse;

        public string[] GetConditionDescriptions()
        {
            List<string> descriptions = new List<string>();
            if (conditions != null)
            {
                foreach (ConditionData c in conditions)
                {
                    if (c != null)
                        descriptions.Add(c.description);
                }
            }
            return descriptions.ToArray();
        }

        [NaughtyAttributes.Button("Refresh Default Conditions and Effects")]
        public void RefreshDefaults()
        {

            T AddOrUpdateSubAsset<T>(List<EffectData> list, System.Predicate<EffectData> match, System.Action<T> update, string name) where T : EffectData
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

            void RemoveSubAssets<T>(List<EffectData> list, System.Predicate<EffectData> match) where T : EffectData
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
                    e => ((ModifyManaEffect)e).amount = consumedMana,
                    $"ConsumeMana{this.name}"
                );
            }
            else
            {
                RemoveSubAssets<EffectData>(effects, e => e is ModifyManaEffect && e.name.StartsWith("Consume") && e.name.EndsWith(this.name));
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
                    e => ((ModifyActionPointsEffect)e).amount = consumedActionPoints,
                    $"ConsumeActionPoints{this.name}"
                );
            }
            else
            {
                RemoveSubAssets<EffectData>(effects, e => e is ModifyActionPointsEffect && e.name.StartsWith("Consume") && e.name.EndsWith(this.name));
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
    }
}