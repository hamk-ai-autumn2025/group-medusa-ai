using System.Collections;
using UnityEngine;

namespace dev.susybaka.TurnBasedGame.Battle.Data 
{
    public abstract class EffectData : ScriptableObject
    {
        public bool preTurn = false;

        public abstract IEnumerator Execute(ActionContext ctx);
    }
}