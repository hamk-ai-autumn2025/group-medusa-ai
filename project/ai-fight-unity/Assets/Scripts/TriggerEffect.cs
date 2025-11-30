using System.Collections;
using System.Collections.Generic;
using dev.susybaka.TurnBasedGame.Battle.Data;
using dev.susybaka.TurnBasedGame.Characters;
using UnityEngine;

namespace dev.susybaka.TurnBasedGame.Battle.Effects
{
    public class TriggerEffect : MonoBehaviour
    {
        GameManager gameManager;

        public EffectData[] effects;

        private void Awake()
        {
            gameManager = GameManager.Instance;
        }

        public void Trigger(Character source)
        {
            for (int i = 0; i < effects.Length; i++)
            {
                StartCoroutine(effects[i].Execute(new ActionContext(gameManager, gameManager.BattleHandler, source, null, null)));
            }
        }
    }
}