using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
        private List<MinigameContext> minigames = new List<MinigameContext>();

        [Serializable]
        private struct MinigameContext
        {
            public ActionContext context;
            public Action onHit;
            public MinigameData data;
            public WaitForSeconds waitStart;
            public WaitForSeconds waitEnd;

            public MinigameContext(ActionContext context, Action onHit, MinigameData data)
            {
                this.context = context;
                this.onHit = onHit;
                this.data = data;
                this.waitStart = new WaitForSeconds(data.startDelay);
                this.waitEnd = new WaitForSeconds(data.finishDelay);
            }
        }

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

        public void Setup(List<ActionContext> ctx, List<Action> onHit)
        {
            minigames = new List<MinigameContext>(ctx.Count);

            for (int i = 0; i < ctx.Count; i++)
            {
                Action a = null;

                if (i < onHit.Count)
                    a = onHit[i];
                else
                    a = () => { };

                minigames.Add(new MinigameContext(ctx[i], a, ctx[i].ability.minigame));
            }
        }

        public void Setup(ActionContext ctx, Action onHit)
        {
            minigames = new List<MinigameContext>();
            minigames.Add(new MinigameContext(ctx, onHit, ctx.ability.minigame));
        }

        public IEnumerator IE_StartMinigame()
        {
            MinigameContext mainMinigame = new MinigameContext();

            for (int i = 0; i < minigames.Count; i++)
            {
                Debug.Log($"Minigame [{i}]: isAddMinigame = {minigames[i].data.isAddMinigame}");
                if (!minigames[i].data.isAddMinigame)
                {
                    mainMinigame = minigames[i];
                    break;
                }
            }

            arenaAnimator.SetBool(spawnHash, true);
            playerCharacter.battleController.Initialize(initialPosition);
            playerCharacter.battleController.disabled = false;
            playerCharacter.battleController.useGravity = mainMinigame.data.useGravity;

            // Start all minigames in parallel
            List<Coroutine> coroutines = new List<Coroutine>();
            List<IEnumerator> routines = new List<IEnumerator>();

            for (int i = 0; i < minigames.Count; i++)
            {
                MinigameContext ctx = minigames[i];
                routines.Add(IE_RunSingleMinigame(ctx));
            }

            // Track completion
            bool[] finished = new bool[minigames.Count];

            for (int i = 0; i < routines.Count; i++)
            {
                int idx = i;
                IEnumerator wrapper = WrapRoutine(routines[i], () => finished[idx] = true);
                coroutines.Add(StartCoroutine(wrapper));
            }

            // Wait until all routines are finished
            while (!finished.All(f => f))
                yield return null;

            EndMinigame();
        }

        // Helper to run a single minigame sequence
        private IEnumerator IE_RunSingleMinigame(MinigameContext ctx)
        {
            yield return ctx.waitStart;
            yield return StartCoroutine(IE_MinigameLoop(ctx));
            yield return ctx.waitEnd;
        }

        // Helper to mark completion
        private IEnumerator WrapRoutine(IEnumerator routine, Action onComplete)
        {
            yield return StartCoroutine(routine);
            onComplete?.Invoke();
        }

        public void EndMinigame()
        {
            arenaAnimator.SetBool(spawnHash, false);
            playerCharacter.battleController.disabled = true;
            playerCharacter.battleController.Deinitialize();
            playerCharacter.battleController.useGravity = false;
        }

        private IEnumerator IE_MinigameLoop(MinigameContext ctx)
        {
            if (ctx.data == null)
                yield break;

            if (ctx.data.events.Length < 1)
                yield break;

            List<MinigameEvent> events = new List<MinigameEvent>(ctx.data.events);
            int repeats = 0;

            if (!ctx.data.pickRandomEvent)
            {
                // Randomize order once before starting the loop if needed
                if (ctx.data.randomizeEventOrder)
                {
                    // Shuffle events, but keep those with repeatCount > 0 at the end
                    // Very questionable way of doing it, but works for now for this project
                    events.Shuffle();
                    for (int j = 0; j < events.Count; j++)
                    {
                        if (events[j].repeatCount > 0)
                        {
                            MinigameEvent temp = events[j];
                            events.RemoveAt(j);
                            events.Add(temp);
                            break;
                        }
                    }
                }

                for (int i = 0; i < events.Count; i++)
                {
                    //Debug.Log($"Minigame Event [{repeats}]: {events[i].name}");
                    MinigameEvent e = events[i];

                    if (e.prefabs != null && e.prefabs.Length > 0)
                    {
                        for (int p = 0; p < e.prefabs.Length; p++)
                        {
                            MinigamePrefabSpawn spawn = e.prefabs[p];
                            Instantiate(spawn.prefab, spawn.spawnLocation, spawn.rotation != 0 ? Quaternion.Euler(new Vector3(0f, 0f, spawn.rotation)) : Quaternion.identity, dynamicParent).Initialize(() => OnHit(ctx));
                        }
                    }

                    if (e.repeatCount > 0 && repeats < e.repeatCount)
                    {
                        repeats++;
                        i = -1;

                        //Debug.Log("Repeating event: " + e.name + " (" + repeats + "/" + e.repeatCount + ")");

                        // Randomize order between loops if needed
                        if (ctx.data.randomizeOrderBetweenLoops)
                        {
                            // Shuffle events, but keep those with repeatCount > 0 at the end
                            // Very questionable way of doing it, but works for now for this project
                            events.Shuffle();
                            for (int j = 0; j < events.Count; j++)
                            {
                                if (events[j].repeatCount > 0)
                                {
                                    MinigameEvent temp = events[j];
                                    events.RemoveAt(j);
                                    events.Add(temp);
                                    break;
                                }
                            }

                            //for (int j = 0; j < events.Count; j++)
                            //{
                            //    Debug.Log($"Shuffled [{repeats}]: {events[j].name}");
                            //}
                        }
                    }

                    yield return new WaitForSeconds(e.duration);
                }
            }
            else
            {
                MinigameEvent e = events.GetRandomItem();

                if (e.prefabs != null && e.prefabs.Length > 0)
                {
                    for (int p = 0; p < e.prefabs.Length; p++)
                    {
                        MinigamePrefabSpawn spawn = e.prefabs[p];
                        Instantiate(spawn.prefab, spawn.spawnLocation, spawn.rotation != 0 ? Quaternion.Euler(new Vector3(0f, 0f, spawn.rotation)) : Quaternion.identity, dynamicParent).Initialize(() => OnHit(ctx));
                    }
                }

                yield return new WaitForSeconds(e.duration);
            }
        }

        private void OnHit(MinigameContext ctx)
        {
            StartCoroutine(abilitySystem.Run(ctx.context));
            ctx.onHit?.Invoke();
        }
    }
}