using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace dev.susybaka.TurnBasedGame.Battle.Data
{
    [CreateAssetMenu(menuName = "Turn Based Game/Status Effects/Status Effect")]
    public class StatusEffectData : ScriptableObject
    {
        public string displayName = "New Effect";
        [TextArea] public string description;
        public string[] tags;
        public EffectType type = EffectType.misc;
        public bool isNegative => type == EffectType.debuff;
        public bool isInfinite = false;
        public bool allowRefresh = false;
        [Min(1)] public int maxStacks = 1;
        //public bool preventsAction = false;
        //[NaughtyAttributes.ShowIf(nameof(preventsAction))] public string preventedActionTag = "";
        public List<EffectData> onApply;
        public List<EffectData> onRemove;
        public List<EffectData> onTick;
        [NaughtyAttributes.Button("Refresh Default Effects")]
        public void RefreshDefaults()
        {
            // Implement similar logic as in AbilityData if needed
        }
    }
}
