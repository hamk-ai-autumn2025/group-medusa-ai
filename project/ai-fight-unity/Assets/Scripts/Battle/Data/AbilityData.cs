using System.Collections.Generic;
using UnityEngine;
using dev.susybaka.TurnBasedGame.Minigame.Data;

namespace dev.susybaka.TurnBasedGame.Battle.Data
{
    [CreateAssetMenu(menuName = "Turn Based Game/Battles/Ability")]
    public class AbilityData : ScriptableObject
    {
        public string displayName;
        [TextArea] public string description;

        public bool requiresTarget = true;
        public TargetGroup targetGroup = TargetGroup.enemy;

        public List<ConditionData> conditions;
        public List<EffectData> effects;

        public MinigameData minigame;
    }
}