using System.Collections;
using UnityEngine;

namespace dev.susybaka.TurnBasedGame.Battle.Data
{
    [CreateAssetMenu(menuName = "Turn Based Game/Battles/Effects/Set Healing Flag")]
    public class SetHealingFlagEffect : EffectData
    {
        public bool setTo = true;

        public override IEnumerator Execute(ActionContext ctx)
        {
            ctx.actor?.allowHealing.SetFlag("SetHealingFlagEffect", setTo);
            yield break;
        }
    }
}