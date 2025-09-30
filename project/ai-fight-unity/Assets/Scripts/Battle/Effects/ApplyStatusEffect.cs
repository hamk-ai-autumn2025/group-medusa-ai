using System.Collections;
using UnityEngine;

namespace dev.susybaka.TurnBasedGame.Battle.Data
{
    [CreateAssetMenu(menuName = "Turn Based Game/Battles/Effects/Apply Status")]
    public class ApplyStatusEffect : EffectData
    {
        //public StatusData status;
        public int length = 3;

        public override IEnumerator Execute(ActionContext ctx)
        {
            foreach (var t in ctx.targets)
            {
                // t.ApplyStatus(status, length);
                // TODO: VFX/SFX hook
            }
            yield break;
        }
    }
}