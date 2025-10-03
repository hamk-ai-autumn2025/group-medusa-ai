using System.Collections;
using UnityEngine;

namespace dev.susybaka.TurnBasedGame.Battle.Data
{
    [CreateAssetMenu(menuName = "Turn Based Game/Battles/Effects/Set Silence Flag")]
    public class SetSilenceFlagEffect : EffectData
    {
        public bool setTo = true;

        public override IEnumerator Execute(ActionContext ctx)
        {
            ctx.actor?.isSilenced.SetFlag("SetSilenceFlagEffect", setTo);
            yield break;
        }
    }
}