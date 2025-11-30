using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using dev.susybaka.TurnBasedGame.AI;
using dev.susybaka.TurnBasedGame.Battle.Data;
using dev.susybaka.TurnBasedGame.Characters;
using dev.susybaka.TurnBasedGame.Dialogue;
using dev.susybaka.TurnBasedGame.Dialogue.Data;
using dev.susybaka.TurnBasedGame.Enemies;
using dev.susybaka.TurnBasedGame.Minigame;
using dev.susybaka.TurnBasedGame.UI;
using UnityEngine;
using static dev.susybaka.TurnBasedGame.Battle.Turn;
using static UnityEngine.Rendering.VolumeComponent;

namespace dev.susybaka.TurnBasedGame.Battle
{
    public class TurnSystem : MonoBehaviour
    {
        private BattlePhase phase = BattlePhase.planning;
        public BattlePhase Phase => phase;
        private int currentTurn = 0;
        public int CurrentTurn => currentTurn;

        private readonly List<Intent> playerPlan = new List<Intent>();
        private readonly List<Intent> enemyPlan = new List<Intent>();
        [SerializeField] private Turn previousTurn;

        private GameManager gameManager;
        private BattleHandler battleHandler;
        private HudNavigationHandler nav;
        private MinigameHandler minigameHandler;
        private PartyWindow partyWindow;
        private Party playerParty;
        private bool initialized = false;
        private bool enemyPlanningDone = false;
        private bool mercyWin = false;

        private Dictionary<Character, Dictionary<AbilityData, int>> abilityHistory = new Dictionary<Character, Dictionary<AbilityData, int>>();
        private Dictionary<Character, Dictionary<AbilityData, int>> abilityUseCount = new Dictionary<Character, Dictionary<AbilityData, int>>();
        private bool guardedThisTurn = false;
        private Dictionary<Party, int> guardStreaks = new Dictionary<Party, int>();
        private bool healedThisTurn = false;
        private Dictionary<Party, int> healStreaks = new Dictionary<Party, int>();
        private int lastTurnActed = -1;
        private Dictionary<Character, AbilityData> lastUsedAbility = new Dictionary<Character, AbilityData>();
        private Dictionary<EnemyCharacter, EnemyMood> previousMoods = new Dictionary<EnemyCharacter, EnemyMood>();

        public void Initialize(GameManager gameManager, PartyWindow partyWindow)
        {
            if (initialized)
                return;

            initialized = true;
            this.gameManager = gameManager;
            battleHandler = this.gameManager.BattleHandler;
            nav = this.gameManager.HudNavigationHandler;
            minigameHandler = this.gameManager.MinigameHandler;
            this.partyWindow = partyWindow;
            guardedThisTurn = false;
            healedThisTurn = false;
            playerParty = battleHandler.allies;
            mercyWin = false;
        }

        public void EndCombat()
        {
            mercyWin = true;
        }

        public void StartBattle(FightData data)
        {
            // Add the base action points for both parties
            battleHandler.enemies.ModifyPoints(data.enemyStartingActionPoints);
            battleHandler.allies.ModifyPoints(data.allyStartingActionPoints);

            mercyWin = false;

            StartCoroutine(IE_BattleLoop());
        }

        private IEnumerator IE_BattleLoop()
        {
            while (true)
            {
                currentTurn++;
                battleHandler.UpdateTurnState(currentTurn);

                if (CheckWinLose())
                    break;

                Debug.Log("1.");

                yield return IE_PlayerPlanning();
                if (CheckWinLose())
                    break;

                Debug.Log("2.");

                yield return IE_PlayerExecution();
                if (CheckWinLose())
                    break;

                Debug.Log("3.");

                while (!enemyPlanningDone)
                    yield return null;
                if (CheckWinLose())
                    break;

                Debug.Log("4.");

                yield return IE_EnemyExecution();
                if (CheckWinLose())
                    break;

                Debug.Log("5.");

                // Store the turn data for potential AI use
                previousTurn = new Turn(currentTurn, playerPlan.ToChoices(), enemyPlan.ToChoices());
                playerPlan.Clear();
                enemyPlan.Clear();
            }
            Debug.Log("test 11111");
            // TODO: Add Victory/Defeat handling here
            battleHandler.win = phase == BattlePhase.victory;
            gameManager.dynamicTimeScale = 0.1f;
            yield return new WaitForSecondsRealtime(4f);
            gameManager.dynamicTimeScale = 1f;
            // For now just end immediately
            battleHandler.EndBattle();
            currentTurn = 0;
        }

        private IEnumerator IE_PlayerPlanning()
        {
            phase = BattlePhase.planning;
            playerPlan.Clear();
            //partyWindow.OpenForPlanning(TargetGroup.allies);
            partyWindow.ClearOrdersAndReenable();
            partyWindow.isActive = true;
            if (currentTurn > 1) // Do not referesh on first turn to avoid selecting before dialogue is done
                partyWindow.RefreshUI(0);

            while (!AllEligibleMembersPlanned())
                yield return null;
            
            partyWindow.isActive = false;
            yield break;
        }

        public void CommitIntent(Character actor, AbilityData ability, IList<Character> targets)
        {
            // Prevent duplicates and dead selections
            if (!actor.isAlive || playerPlan.Exists(i => i.actor == actor))
                return;

            playerPlan.Add(new Intent(actor, ability, targets));

            // Handle pre-turn effects immediately
            if (ability.effects != null && ability.effects.Count > 0)
            {
                ActionContext preTurnCtx = new ActionContext(gameManager, battleHandler, actor, targets, ability);
                if (preTurnCtx.ability.effects != null)
                {
                    foreach (EffectData e in preTurnCtx.ability.effects)
                        if (e != null && e.preTurn)
                            battleHandler.AbilitySystem.StartCoroutine(e.Execute(preTurnCtx));
                }
            }

            partyWindow.MarkActorOrder(actor, playerPlan.Count);
            partyWindow.DisableActor(actor);
            nav.ReturnToRoot();
        }

        private bool AllEligibleMembersPlanned()
        {
            int alive = 0;
            foreach (Character ch in battleHandler.allies.members)
            {
                if (ch != null && ch.isAlive)
                    alive++;
            }
            return playerPlan.Count >= alive;
        }
        
        private IEnumerator IE_PlayerExecution()
        {
            phase = BattlePhase.playerExec;

            partyWindow.isActive = false;

            int running = 0;
            bool act = false;
            Intent actIntent = new Intent(null, null, new List<Character>());

            for (int i = 0; i < playerPlan.Count; i++)
            {
                Intent intent = playerPlan[i];
                int sliderIndex = i; // assume index matches UI slot

                if (!intent.actor.isAlive)
                    continue;

                ActionContext ctx = new ActionContext(gameManager, battleHandler, intent.actor, intent.targets, intent.ability, null, 0, 1.0f);

                // For now handle the act menu as hardcoded special ability
                // When invoked, store that we need to open it after all other characters have attacked
                // Also hide the minigame slider for that character
                if (intent.ability.opensActMenu)
                {
                    battleHandler.battleWindow.AttackMinigameWindow.HideMinigame(sliderIndex);
                    act = true;
                    actIntent = new Intent(intent);
                    continue;
                }
                else if (!intent.ability.isAttack) // non-attack abilities do not get a minigame
                {
                    battleHandler.battleWindow.AttackMinigameWindow.HideMinigame(sliderIndex);
                }

                running++;

                if (!battleHandler.battleWindow.AttackMinigameWindow.isOpen)
                {
                    battleHandler.battleWindow.AttackMinigameWindow.OpenWindow();
                    partyWindow.isActive = false;
                }

                // Fire one coroutine per intent
                battleHandler.StartCoroutine(
                    IE_PlayerPerformTurn(
                        sliderIndex,
                        ctx,
                        () => running--   // callback when that whole chain is done
                    )
                );

                // update history and increment use count
                UpdateHistory(intent.actor, intent.ability);
                IncrementAbilityUseCount(intent.actor, intent.ability);
                lastUsedAbility[intent.actor] = intent.ability;

                if (intent.ability.isDefensive && !guardedThisTurn)
                {
                    guardedThisTurn = true;
                    guardStreaks[intent.actor.Party] = (guardStreaks.ContainsKey(intent.actor.Party) ? guardStreaks[intent.actor.Party] : 0) + 1;
                }
                if (intent.ability.isHeal && !healedThisTurn)
                {
                    healedThisTurn = true;
                    healStreaks[intent.actor.Party] = (healStreaks.ContainsKey(intent.actor.Party) ? healStreaks[intent.actor.Party] : 0) + 1;
                }
                if (intent.actor.data.isMainPlayerCharacter && intent.targets.Available() && lastTurnActed < currentTurn)
                {
                    for (int t = 0; t < intent.targets.Count; t++)
                    {
                        if (intent.targets[t] is EnemyCharacter ec)
                        {
                            ec.aggressionLevel = Mathf.Clamp(ec.aggressionLevel + 5, 0, 100);
                        }
                    }
                }

                if (CheckWinLose())
                    yield break;
            }

            // Wait until all spawned chains are finished
            while (running > 0)
            {
                partyWindow.isActive = false;
                yield return null;
            }

            partyWindow.isActive = false;

            if (act && actIntent.actor != null)
            {
                yield return new WaitForSeconds(1f); // wait for a moment and close minigame if open
                if (battleHandler.battleWindow.AttackMinigameWindow.isOpen)
                {
                    battleHandler.battleWindow.AttackMinigameWindow.CloseWindow();
                    partyWindow.isActive = false;
                }

                //partyWindow.isActive = false;
                battleHandler.battleWindow.TalkWindow.OpenWindow();
                yield return StartCoroutine(battleHandler.battleWindow.TalkCapture.IE_WaitForTextInput(battleHandler.battleWindow.TalkCapture.DefaultPrefill, false));
                battleHandler.battleWindow.TalkWindow.CloseWindow();
                battleHandler.battleWindow.TalkCapture.ResetText();
                //partyWindow.isActive = true;
                lastTurnActed = currentTurn;

                if (!string.IsNullOrEmpty(battleHandler.battleWindow.TalkCapture.Result))
                {
                    string text = battleHandler.battleWindow.TalkCapture.Result;

                    if (actIntent.targets != null && actIntent.targets.Count > 0)
                    {
                        foreach (Character target in actIntent.targets)
                        {
                            if (target == battleHandler.enemies.leader)
                            {
                                // If any of the targets is the main boss, use the base text
                                text = battleHandler.battleWindow.TalkCapture.Result;
                                break;
                            }
                            else if (target != battleHandler.enemies.leader)
                            {
                                // Otherwise, indicate which enemy is being addressed so the AI who is acting as the main boss knows who is being spoken to
                                text = string.Format("[Add.{0}] {1}", target.Id, battleHandler.battleWindow.TalkCapture.Result);
                                break;
                            }
                        }
                    }

                    AIHandler.ActSnapshot snap = CreateActSnapshot(text, actIntent.targets);

                    AIHandler.Mood result = null;
                    yield return StartCoroutine(gameManager.AIHandler.RunActRequest(snap, m => result = m));

                    if (result != null)
                    {
                        EnemyCharacter ec = actIntent.targets.OfType<EnemyCharacter>().First(e => e.Id == result.boss_id);
                        if (ec != null)
                        {
                            ec.aggressionLevel = result.aggression_level;
                            ec.fearLevel = result.fear_level;
                            ec.respectLevel = result.respect_level;
                            ec.pityLevel = result.pity_level;
                            battleHandler.battleWindow.SpeechWindow.ShowText(result.dialogue, false);
                        }
                    }
                }
            }

            // Enemy planning can be started in parallel since the AI takes a bit to respond
            StartCoroutine(IE_EnemyPlanning());

            yield return new WaitForSeconds(2f);

            if (battleHandler.battleWindow.AttackMinigameWindow.isOpen)
            {
                battleHandler.battleWindow.AttackMinigameWindow.CloseWindow();
                partyWindow.isActive = false;
            }

            // Let allies passively gain points for this turn
            battleHandler.allies.ModifyPoints(battleHandler.data.allyActionPointsPerTurn);

            guardedThisTurn = false;
            healedThisTurn = false;
            partyWindow.RefreshUI();
            yield return new WaitForSeconds(1f); // slight delay for clarity
            //partyWindow.ClearOrders();
        }

        private IEnumerator IE_PlayerPerformTurn(int index, ActionContext ctx, Action onDone)
        {
            float accuracy01 = 1f;
            int finalDamage = ctx.ability.amountDamage;

            // Run single-char minigame if an attack
            if (ctx.ability.isAttack)
            {
                yield return battleHandler.battleWindow.AttackMinigameWindow.IE_ProcessMinigame(index, ctx.ability, result =>
                {
                    accuracy01 = result.Item1; // 0–1
                    finalDamage = result.Item2;
                });
            }

            // Now run ability immediately for THIS character
            ActionContext finalCtx = new ActionContext(ctx, finalDamage, accuracy01);

            yield return battleHandler.AbilitySystem.Run(finalCtx);

            onDone?.Invoke();
        }

        private IEnumerator IE_EnemyPlanning()
        {
            phase = BattlePhase.enemyPlanning;
            enemyPlan.Clear();
            enemyPlanningDone = false;

            foreach (Character enemy in battleHandler.enemies.members)
            {
                if (enemy == null || !enemy.isAlive)
                    continue;

                AbilityData ability = null;
                IList<Character> targets = null;

                List<AbilityData> abilities = new List<AbilityData>();

                abilities.AddRange(enemy.KnownAbilities);
                abilities.AddRange(enemy.KnownSpells);

                // Build snapshot from live battle data
                // This is what the AI will see and base its decision on
                AIHandler.Snapshot snap = CreateCombatSnapshot(enemy, abilities);

                AIHandler.Decision picked = null;
                yield return StartCoroutine(gameManager.AIHandler.DecideCoroutine(snap, d => picked = d));

                // Fallback to dumb AI: Random first usable ability + a random valid target set
                if (picked == null || picked.rationale.Contains("fallback-"))
                {
                    Debug.LogWarning($"AIHandler returned no decision for {enemy.data.name}, falling back to random usable ability.");
                    ability = PickFirstUsableAbility(enemy);
                    if (ability == null)
                        continue;

                    targets = PickTargetsFor(ability, enemy, battleHandler);

                    Debug.Log($"Dumb AI picked ability '{ability.name}' with target(s): {string.Join(", ", targets.Select(t => t.data.name))}");
                }
                else // Otherwise, parse the decision and map to actual game logic
                {
                    Debug.Log($"AIHandler returned the following:\ntarget_id '{picked.target_id}'\nability_id '{picked.ability_id}'\nrationale '{picked.rationale}'");
                    ability = abilities.Find(a => a.name == picked.ability_id);

                    if (string.IsNullOrEmpty(picked.target_id) || picked.target_id.Contains("none") || picked.target_id.Contains("self"))
                        targets = new List<Character> { enemy };
                    else
                        targets = battleHandler.allies.members.Where(c => c.data.name == picked.target_id).ToList();
                }

                enemyPlan.Add(new Intent { actor = enemy, ability = ability, targets = targets });

                // Handle pre-turn effects immediately
                if (ability.effects != null && ability.effects.Count > 0)
                {
                    ActionContext preTurnCtx = new ActionContext(gameManager, battleHandler, enemy, targets, ability);
                    if (preTurnCtx.ability.effects != null)
                    {
                        foreach (EffectData e in preTurnCtx.ability.effects)
                            if (e != null && e.preTurn)
                                battleHandler.AbilitySystem.StartCoroutine(e.Execute(preTurnCtx));
                    }
                }
            }

            enemyPlanningDone = true;
            yield break;
        }

        private IEnumerator IE_EnemyExecution()
        {
            phase = BattlePhase.enemyExec;

            List<ActionContext> addIntentContexts = new List<ActionContext>();
            List<Action> addIntentOnHits = new List<Action>();
            List<(DialogueData, DialogueContext)> addIntentDialogueOnUse = new List<(DialogueData, DialogueContext)>();

            foreach (Intent intent in enemyPlan)
            {
                if (intent.ability != null && intent.ability.minigame != null)
                {
                    if (intent.ability.minigame.isAddMinigame)
                    {
                        ActionContext ctx = new ActionContext(gameManager, battleHandler, intent.actor, intent.targets, intent.ability);
                        addIntentContexts.Add(ctx);
                        addIntentOnHits.Add(() => { partyWindow.RefreshUI(); battleHandler.allies.ModifyPoints(5); } );
                        addIntentDialogueOnUse.Add((intent.ability.dialogueOnUse, new Dialogue.DialogueContext(intent.actor, intent.targets, intent.ability, null)) );
                    }
                }
            }

            foreach (Intent intent in enemyPlan)
            {
                if (!intent.actor.isAlive)
                    continue;

                if (intent.ability == null || intent.targets == null || intent.targets.Count < 1)
                    continue;

                bool isAddIntent = intent.ability.minigame != null && intent.ability.minigame.isAddMinigame;

                if (intent.ability.dialogueOnUse != null && !isAddIntent)
                {
                    if (!string.IsNullOrEmpty(intent.ability.description))
                    {
                        battleHandler.battleWindow.DescriptionWindow.SetText(intent.ability.description);
                    }
                    yield return gameManager.DialogueHandler.IE_QueueDialogue(intent.ability.dialogueOnUse, new Dialogue.DialogueContext(intent.actor, intent.targets, intent.ability, null));
                    partyWindow.isActive = false;
                    partyWindow.RefreshUI();
                }

                if (addIntentDialogueOnUse != null && addIntentDialogueOnUse.Count > 0 && !isAddIntent)
                {
                    for (int d = 0; d < addIntentDialogueOnUse.Count; d++)
                    {
                        var (data, diaCtx) = addIntentDialogueOnUse[d];
                        if (!string.IsNullOrEmpty(intent.ability.description))
                        {
                            battleHandler.battleWindow.DescriptionWindow.SetText(intent.ability.description);
                        }
                        yield return gameManager.DialogueHandler.IE_QueueDialogue(data, diaCtx);
                        partyWindow.isActive = false;
                        partyWindow.RefreshUI();
                    }
                }

                ActionContext ctx = new ActionContext(gameManager, battleHandler, intent.actor, intent.targets, intent.ability);
                
                if (intent.ability.minigame != null && !intent.ability.minigame.isAddMinigame)
                {

                    addIntentContexts.Add(ctx);
                    addIntentOnHits.Add(() => { partyWindow.RefreshUI(); battleHandler.allies.ModifyPoints(5); } );

                    // float mitigation = 0f;
                    // Setup the minigame, refresh UI on each hit to show updated points and give enemies points on each hit
                    minigameHandler.Setup(addIntentContexts, addIntentOnHits);
                    yield return minigameHandler.IE_StartMinigame(); //v => mitigation = v
                }
                else if (intent.ability.minigame == null)
                {
                    yield return battleHandler.AbilitySystem.Run(ctx);
                }
                partyWindow.isActive = false;
                //yield return new WaitForSeconds(2f); // slight delay for clarity

                // update history and increment use count
                UpdateHistory(intent.actor, intent.ability);
                IncrementAbilityUseCount(intent.actor, intent.ability);
                lastUsedAbility[intent.actor] = intent.ability;

                if (intent.ability.isDefensive && !guardedThisTurn)
                {
                    guardedThisTurn = true;
                    guardStreaks[intent.actor.Party] = (guardStreaks.ContainsKey(intent.actor.Party) ? guardStreaks[intent.actor.Party] : 0) + 1;
                }
                if (intent.ability.isHeal && !healedThisTurn)
                {
                    healedThisTurn = true;
                    healStreaks[intent.actor.Party] = (healStreaks.ContainsKey(intent.actor.Party) ? healStreaks[intent.actor.Party] : 0) + 1;
                }

                //yield return battleHandler.AbilitySystem.Run(ctx.ability, ctx.actor, ctx.targets);
                if (CheckWinLose())
                    yield break;
            }

            // Let enemies passively gain points for this turn
            battleHandler.enemies.ModifyPoints(battleHandler.data.enemyActionPointsPerTurn);

            guardedThisTurn = false;
            healedThisTurn = false;
            partyWindow.RefreshUI();
        }

        private AIHandler.Snapshot CreateCombatSnapshot(Character enemy, List<AbilityData> abilities, bool postTurn = false)
        {
            // Pre filter abilities to remove unwanted ones
            for (int i = abilities.Count - 1; i >= 0; i--)
            {
                if (abilities.Count <= 1)
                {
                    break;
                }
                // Remove abilities that cannot be executed due to conditions
                if (abilities[i].conditions != null)
                {
                    bool hasInvalidCondition = false;
                    foreach (ConditionData c in abilities[i].conditions)
                    {
                        if (c == null || (c.preTurn && !c.postTurn && postTurn) || (c.postTurn && !c.preTurn && !postTurn))
                        {
                            continue;
                        }

                        if (c != null && !c.Evaluate(new ActionContext(gameManager, battleHandler, enemy, abilities[i].dealsDamage && abilities[i].amountDamage < 0 ? battleHandler.allies.members : battleHandler.enemies.members, abilities[i]), out string reason))
                        {
                            abilities.RemoveAt(i);
                            hasInvalidCondition = true;
                            break;
                        }
                    }
                    if (hasInvalidCondition)
                        continue;
                }
                // Remove abilities that were used last turn (if there are alternatives)
                if (lastUsedAbility.ContainsKey(enemy) && lastUsedAbility[enemy] == abilities[i] && abilities.Count > 1)
                {
                    abilities.RemoveAt(i);
                    continue;
                }
            }

            return AIHandler.BuildSnapshot(
                    turn: currentTurn,
                    bossId: enemy.Id,
                    mainBossId: battleHandler.enemies.leader.Id,
                    playerParty: battleHandler.allies.members.Select(p =>
                        (id: p.Id,
                         hp: p.health,
                         alive: p.isAlive,
                         statusEffects: p.GetStatusEffects().Select(s =>
                             (id: s.Id,
                              duration: s.Duration,
                              stacks: s.Stacks,
                              tags: s.data.tags.AsEnumerable())
                         ),
                         analyzed: enemy.KnowledgeBanks.Any(k => k.id == p.data.name),
                         analyzedStats: enemy.KnowledgeBanks.Where(k => k.id == p.data.name).SelectMany(k => k.Select(entry => (id: entry.name, lore: entry.text))))
                    ),
                    enemyParty: battleHandler.enemies.members.Select(c =>
                        (id: c.Id,
                         hp: c.health,
                         maxHp: c.maxHealth,
                         attackPower: c.attackPower.Value,
                         defense: c.defense.Value,
                         actionPoints: c.ActionPoints,
                         alive: c.isAlive,
                         statusEffects: c.GetStatusEffects().Select(s =>
                             (id: s.Id,
                              duration: s.Duration,
                              stacks: s.Stacks,
                              tags: s.data.tags.AsEnumerable())
                         ))
                    ),
                    elementSpam: "none", // TODO: Track element spam from player actions
                    guardStreak: guardStreaks.ContainsKey(playerParty) ? guardStreaks[playerParty] : 0,
                    healStreak: healStreaks.ContainsKey(playerParty) ? healStreaks[playerParty] : 0,
                    abilities: abilities.Select(a =>
                        (id: a.name,
                        requireTarget: a.requiresTarget,
                        tags: a.tags.AsEnumerable(), // ensure IEnumerable<string>
                        attack: (a.dealsDamage && a.amountDamage < 0),
                        useCount: abilityUseCount.ContainsKey(enemy) ? (abilityUseCount[enemy].ContainsKey(a) ? abilityUseCount[enemy][a] : 0) : 0,
                        lastTurnUsed: abilityHistory.ContainsKey(enemy) ? (abilityHistory[enemy].ContainsKey(a) ? abilityHistory[enemy][a] : -1) : -1)
                    ),
                    bossKnowledge: enemy.KnowledgeBanks.Where(kb => kb.id.StartsWith("#")).SelectMany(kb => kb.Select(entry => (id: entry.name, lore: entry.text))),
                    agressionLevel: (enemy is EnemyCharacter ec) ? ec.aggressionLevel : 50,
                    fearLevel: (enemy is EnemyCharacter ec2) ? ec2.fearLevel : 0,
                    respectLevel: (enemy is EnemyCharacter ec3) ? ec3.respectLevel : 0,
                    pityLevel: (enemy is EnemyCharacter ec4) ? ec4.pityLevel : 0,
                    previousBossAbilityId: lastUsedAbility.ContainsKey(enemy) ? lastUsedAbility[enemy].name : string.Empty,
                    previousPlayerParty: (previousTurn.playerPlan != null && previousTurn.playerPlan.Count > 0)
                        ? previousTurn.playerPlan.Select(tc => (id: tc.actor.id, hp: tc.actor.hp, alive: tc.actor.isAlive, statusEffectCount: tc.actor.statusEffectCount))
                        : Enumerable.Empty<(string id, int hp, bool alive, int statusEffectCount)>(),
                    previousEnemyParty: (previousTurn.enemyPlan != null && previousTurn.enemyPlan.Count > 0)
                        ? previousTurn.enemyPlan.Select(tc => (id: tc.actor.id, hp: tc.actor.hp, alive: tc.actor.isAlive, statusEffectCount: tc.actor.statusEffectCount))
                        : Enumerable.Empty<(string id, int hp, bool alive, int statusEffectCount)>()
                );
        }

        private AIHandler.ActSnapshot CreateActSnapshot(string playerAct, IList<Character> targets)
        {
            EnemyCharacter boss = targets != null && targets.Count > 0 ? targets.OfType<EnemyCharacter>().FirstOrDefault() : null;

            return AIHandler.BuildActSnapshot(
                turn: currentTurn,
                bossId: boss != null ? boss.Id : string.Empty,
                elementSpam: "none", // TODO: Track element spam from player actions
                guardStreak: guardStreaks.ContainsKey(playerParty) ? guardStreaks[playerParty] : 0,
                healStreak: healStreaks.ContainsKey(playerParty) ? healStreaks[playerParty] : 0,
                playerAction: playerAct,
                aggressionLevel: boss != null ? boss.aggressionLevel : 0,
                fearLevel: boss != null ? boss.fearLevel : 0,
                respectLevel: boss != null ? boss.respectLevel : 0,
                pityLevel: boss != null ? boss.pityLevel : 0,
                previousBossId: boss != null ? boss.Id : string.Empty, // assume single target for now
                previousAggressionLevel: previousMoods.ContainsKey(boss) ? previousMoods[boss].aggression : 0,
                previousFearLevel: previousMoods.ContainsKey(boss) ? previousMoods[boss].fear : 0,
                previousRespectLevel: previousMoods.ContainsKey(boss) ? previousMoods[boss].respect : 0,
                previousPityLevel: previousMoods.ContainsKey(boss) ? previousMoods[boss].pity : 0,
                previousDialogue: previousMoods.ContainsKey(boss) ? previousMoods[boss].previousDialogue : string.Empty
            );
        }

        private void UpdateHistory(Character actor, AbilityData ability)
        {
            if (actor != null && ability != null)
            {
                if (!abilityHistory.ContainsKey(actor))
                    abilityHistory[actor] = new Dictionary<AbilityData, int>();
                if (!abilityHistory[actor].ContainsKey(ability))
                    abilityHistory[actor][ability] = currentTurn;
                abilityHistory[actor][ability] = currentTurn;
            }
        }

        private void IncrementAbilityUseCount(Character actor, AbilityData ability)
        {
            if (actor != null && ability != null)
            {
                if (!abilityUseCount.ContainsKey(actor))
                    abilityUseCount[actor] = new Dictionary<AbilityData, int>();
                if (!abilityUseCount[actor].ContainsKey(ability))
                    abilityUseCount[actor][ability] = 1;
                abilityUseCount[actor][ability]++;
            }
        }

        private void DecrementAbilityUseCount(Character actor, AbilityData ability)
        {
            if (actor != null && ability != null && abilityUseCount.ContainsKey(actor) && abilityUseCount[actor].ContainsKey(ability))
            {
                abilityUseCount[actor][ability] = Mathf.Max(0, abilityUseCount[actor][ability] - 1);
            }
        }

        private bool CheckWinLose()
        {
            Debug.Log("Checking win/lose conditions...");
            bool playersDead = battleHandler.allies.members.TrueForAll(c => c == null || !c.isAlive);
            bool enemiesDead = battleHandler.enemies.members.TrueForAll(c => c == null || !c.isAlive);
            if (playersDead)
            { 
                phase = BattlePhase.defeat; 
                return true; 
            }
            if (enemiesDead)
            { 
                phase = BattlePhase.victory; 
                return true; 
            }
            if (mercyWin)
            {
                phase = BattlePhase.victory;
                return true;
            }
            return false;
        }

        // For dumb AI: pick the first usable ability from a shuffled pool of all known abilities and spells
        private AbilityData PickFirstUsableAbility(Character actor)
        {
            // Build a pool of all known abilities and spells
            AbilityData[] abilityPool = new AbilityData[actor.KnownAbilities.Length + actor.KnownSpells.Length];
            actor.KnownAbilities.CopyTo(abilityPool, 0);
            actor.KnownSpells.CopyTo(abilityPool, actor.KnownAbilities.Length);

            // Shuffle the ability pool
            for (int i = abilityPool.Length - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (abilityPool[i], abilityPool[j]) = (abilityPool[j], abilityPool[i]);
            }

            // Pick the first that passes conditions
            foreach (AbilityData a in abilityPool)
            {
                if (AbilityCanExecute(actor, a, System.Array.Empty<Character>()))
                    return a;
            }
            return null;
        }

        // Helper for dumb AI to check if an ability can be executed based on its conditions
        private bool AbilityCanExecute(Character actor, AbilityData ability, IList<Character> targets, bool postTurn = false)
        {
            var ctx = new ActionContext { actor = actor, battle = battleHandler, ability = ability, targets = targets };
            if (ability.conditions == null || ability.conditions.Count < 1)
                return true;
            foreach (ConditionData c in ability.conditions)
            {
                if (c == null || (c.preTurn && !c.postTurn && postTurn) || (c.postTurn && !c.preTurn && !postTurn))
                {
                    continue;
                }

                if (c != null && !c.Evaluate(ctx, out _))
                {
                    return false;
                }
            }
            return true;
        }

        private IList<Character> PickTargetsFor(AbilityData ability, Character actor, BattleHandler bh)
        {
            switch (ability.targetGroup)
            {
                case TargetGroup.enemy:
                    return new List<Character> { bh.allies.GetFirstAliveMember() };
                case TargetGroup.enemies:
                    return bh.allies.GetAllAliveMembers();
                case TargetGroup.ally:
                    return new List<Character> { bh.enemies.GetFirstWoundedOrAliveMember() };
                case TargetGroup.allies:
                    return bh.enemies.GetAllAliveMembers(); // adjust accordingly
                default:
                    return new List<Character> { actor };
            }
        }
    }
}