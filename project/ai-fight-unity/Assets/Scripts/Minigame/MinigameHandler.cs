using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using dev.susybaka.TurnBasedGame.Battle;
using dev.susybaka.TurnBasedGame.Minigame.Data;
using dev.susybaka.TurnBasedGame.Player;


namespace dev.susybaka.TurnBasedGame.Minigame
{
    public class MinigameHandler : MonoBehaviour
    {
        private GameManager gameManager;
        private AbilitySystem abilitySystem;

        public Animator arenaAnimator;
        [SerializeField] private Vector2 initialPosition = Vector2.zero;
        [SerializeField] private Transform dynamicParent;

        private bool initalized = false;
        private PlayerCharacter playerCharacter;
        private MinigameData data;
        private WaitForSeconds waitStart;
        private WaitForSeconds waitEnd;
        private ActionContext ctx;
        private Action onHit;

        private int spawnHash = Animator.StringToHash("spawn");

        private void Start()
        {
            arenaAnimator.SetBool(spawnHash, false);
        }

        public void Initialize(GameManager gameManager)
        {
            if (initalized) 
                return;

            initalized = true;
            this.gameManager = gameManager;
            this.abilitySystem = gameManager.BattleHandler.AbilitySystem;
            playerCharacter = gameManager.Player;
        }

        public void Setup(ActionContext ctx, Action onHit)
        {
            this.ctx = ctx;
            this.data = ctx.ability.minigame;
            this.onHit = onHit;
            waitStart = new WaitForSeconds(data.startDelay);
            waitEnd = new WaitForSeconds(data.finishDelay);
        }

        public IEnumerator IE_StartMinigame()
        {
            arenaAnimator.SetBool(spawnHash, true);
            playerCharacter.battleController.Initialize(initialPosition);
            playerCharacter.battleController.disabled = false;
            yield return waitStart;
            yield return IE_MinigameLoop();
            yield return waitEnd;
            EndMinigame();
        }

        public void EndMinigame()
        {
            arenaAnimator.SetBool(spawnHash, false);
            playerCharacter.battleController.disabled = true;
            playerCharacter.battleController.Deinitialize();
        }

        private IEnumerator IE_MinigameLoop()
        {
            if (data == null)
                yield break;

            for (int i = 0; i < data.events.Length; i++)
            {
                MinigameEvent e = data.events[i];
                
                if (e.prefabs != null && e.prefabs.Length > 0)
                {
                    for (int p = 0; p < e.prefabs.Length; p++)
                    {
                        MinigamePrefabSpawn spawn = e.prefabs[p];
                        Instantiate(spawn.prefab, spawn.spawnLocation, Quaternion.identity, dynamicParent).Initialize(OnHit);
                    }
                }

                yield return new WaitForSeconds(e.duration);
            }
        }

        private void OnHit()
        {
            StartCoroutine(abilitySystem.Run(ctx.ability, ctx.actor, ctx.targets));
            onHit?.Invoke();
        }
    }
}