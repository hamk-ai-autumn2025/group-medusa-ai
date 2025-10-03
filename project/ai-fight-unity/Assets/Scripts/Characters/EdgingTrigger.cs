using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using dev.susybaka.TurnBasedGame.Battle;
using dev.susybaka.TurnBasedGame.Battle.Data;

namespace dev.susybaka.TurnBasedGame.Characters
{
    public class EdgingTrigger : MonoBehaviour
    {
        private Character character;
        private GameManager gameManager;
        private BattleHandler battleHandler;

        [SerializeField, NaughtyAttributes.Tag] private string edgingTag = "Edging";
        [SerializeField] private EffectData[] edgingEffects;

        private void Awake()
        {
            character = transform.GetComponentInParents<Character>();
            gameManager = GameManager.Instance;
            battleHandler = gameManager?.BattleHandler;
        }

        private void OnTriggerEnter(Collider other)
        {
            this.LogV(("other", other.gameObject.name));

            if (other.CompareTag(edgingTag))
            {
                this.LogV(("other.tag", other.tag));

                ExecuteEffects();
            }
        }

        private void ExecuteEffects()
        {
            if (gameManager == null || battleHandler == null)
                return;

            for (int i = 0; i < edgingEffects.Length; i++)
            {
                edgingEffects[i].Execute(new Battle.ActionContext(gameManager, battleHandler, character, new Character[] { character }, null, null));
            }
        }
    }
}