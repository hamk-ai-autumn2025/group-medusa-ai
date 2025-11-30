using System.Collections;
using UnityEngine;

namespace dev.susybaka.TurnBasedGame.Battle.Data
{
    [CreateAssetMenu(menuName = "Turn Based Game/Battles/Effects/Remove Visual Effect")]
    public class RemoveVisualEffect : EffectData
    {
        public GameObject effectPrefab;

        public override IEnumerator Execute(ActionContext ctx)
        {
            for (int i = 0; i < ctx.targets.Count; i++)
            {
                ctx.targets[i].RemoveVisualEffect(effectPrefab);
            }
            yield break;
        }
    }
}