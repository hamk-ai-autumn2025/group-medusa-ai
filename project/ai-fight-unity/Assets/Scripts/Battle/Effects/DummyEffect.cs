using System.Collections;
using UnityEngine;

namespace dev.susybaka.TurnBasedGame.Battle.Data
{
    [CreateAssetMenu(menuName = "Turn Based Game/Battles/Effects/Dummy")]
    public class DummyEffect : EffectData
    {
        public string log = "Dummy Effect";

        public override IEnumerator Execute(ActionContext ctx)
        {
            Debug.Log($"Triggered {log}");
            yield break;
        }
    }
}