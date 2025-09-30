using System.Collections;
using UnityEngine;

namespace dev.susybaka.TurnBasedGame.Battle.Data
{
    [CreateAssetMenu(menuName = "Turn Based Game/Battles/Effects/Open Window")]
    public class OpenWindowEffect : EffectData
    {
        public GameStateWindowType window = GameStateWindowType.none;

        public override IEnumerator Execute(ActionContext ctx)
        {
            ctx.game.OpenWindow(window);
            yield break;
        }
    }
}