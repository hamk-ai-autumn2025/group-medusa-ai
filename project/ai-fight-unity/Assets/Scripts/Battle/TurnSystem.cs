using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using dev.susybaka.TurnBasedGame.AI;
using dev.susybaka.TurnBasedGame.Battle.Data;
using dev.susybaka.TurnBasedGame.Characters;
using dev.susybaka.TurnBasedGame.Minigame;
using dev.susybaka.TurnBasedGame.UI;
using dev.susybaka.TurnBasedGame.Enemies;

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

        private Dictionary<Character, Dictionary<AbilityData, int>> abilityHistory = new Dictionary<Character, Dictionary<AbilityData, int>>();
        private Dictionary<Character, Dictionary<AbilityData, int>> abilityUseCount = new Dictionary<Character, Dictionary<AbilityData, int>>();
        private bool guardedThisTurn = false;
        private Dictionary<Party, int> guardStreaks = new Dictionary<Party, int>();
        private bool healedThisTurn = false;
        private Dictionary<Party, int> healStreaks = new Dictionary<Party, int>();
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
        }

        public void StartBattle(FightData data)
        {
            StartCoroutine(IE_BattleLoop());
        }

        private IEnumerator IE_BattleLoop()
        {
            while (true)
            {
                currentTurn++;
                battleHandler.UpdateTurnState(currentTurn);

                yield return IE_PlayerPlanning();
                if (CheckWinLose())
                    break;

                yield return IE_PlayerExecution();
                if (CheckWinLose())
                    break;

                yield return IE_EnemyPlanning();
                if (CheckWinLose())
                    break;

                yield return IE_EnemyExecution();
                if (CheckWinLose())
                    break;

                // Store the turn data for potential AI use
                previousTurn = new Turn(currentTurn, playerPlan.ToChoices(), enemyPlan.ToChoices());
                playerPlan.Clear();
                enemyPlan.Clear();
            }
            // TODO: Add Victory/Defeat handling here
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
            foreach (Intent intent in playerPlan)
            {
                if (!intent.actor.isAlive)
                    continue;

                // float mult = 1f;
                // yield return RhythmRunner.Play(intent.actor, intent.ability, v => mult = v);

                // Skip for now, because it is not used right now
                //ActionContext ctx = new ActionContext(gameManager, battleHandler, intent.actor, intent.targets, intent.ability);
                /*{
                    game = GameManager.Instance,
                    battle = battleHandler,
                    actor = intent.actor,
                    targets = intent.targets,
                    ability = intent.ability,
                    //attackMultiplier = mult,
                    //damageMitigation = 0f
                };*/

                // For now handle the act menu as hardcoded special ability
                if (!intent.ability.opensActMenu)
                {
                    yield return battleHandler.AbilitySystem.Run(intent.ability, intent.actor, intent.targets);
                }
                else
                {   // Open Act menu instead of executing ability directly
                    partyWindow.isActive = false;
                    battleHandler.battleWindow.TalkWindow.OpenWindow();
                    yield return StartCoroutine(battleHandler.battleWindow.TalkCapture.IE_WaitForTextInput(string.Empty, false));
                    battleHandler.battleWindow.TalkWindow.CloseWindow();
                    battleHandler.battleWindow.TalkCapture.ResetText();
                    partyWindow.isActive = true;
                    
                    if (!string.IsNullOrEmpty(battleHandler.battleWindow.TalkCapture.Result))
                    {
                        AIHandler.ActSnapshot snap = CreateActSnapshot(battleHandler.battleWindow.TalkCapture.Result, intent.targets);

                        AIHandler.Mood result = null;
                        yield return StartCoroutine(gameManager.AIHandler.RunActRequest(snap, m => result = m));

                        if (result != null)
                        {
                            EnemyCharacter ec = intent.targets.OfType<EnemyCharacter>().First(e => e.Id == result.boss_id);
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

                if (CheckWinLose())
                    yield break;
            }
            guardedThisTurn = false;
            healedThisTurn = false;
            partyWindow.isActive = false;
            partyWindow.RefreshUI();
            yield return new WaitForSeconds(1f); // slight delay for clarity
            //partyWindow.ClearOrders();
        }
        
        private IEnumerator IE_EnemyPlanning()
        {
            phase = BattlePhase.enemyPlanning;
            enemyPlan.Clear();

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
            }
            yield break;
        }

        private IEnumerator IE_EnemyExecution()
        {
            phase = BattlePhase.enemyExec;
            foreach (Intent intent in enemyPlan)
            {
                if (!intent.actor.isAlive)
                    continue;

                if (intent.ability == null || intent.targets == null || intent.targets.Count < 1)
                    continue;

                if (intent.ability.dialogueOnUse != null)
                {
                    yield return gameManager.DialogueHandler.IE_QueueDialogue(intent.ability.dialogueOnUse);
                }

                ActionContext ctx = new ActionContext(gameManager, battleHandler, intent.actor, intent.targets, intent.ability, null);
                /*{
                    game = GameManager.Instance,
                    battle = battleHandler,
                    actor = intent.actor,
                    targets = intent.targets,
                    ability = intent.ability,
                    attackMultiplier = 1f,
                    damageMitigation = mitigation
                };*/
                
                if (intent.ability.minigame != null)
                {
                    // float mitigation = 0f;
                    minigameHandler.Setup(ctx, () => partyWindow.RefreshUI());
                    yield return minigameHandler.IE_StartMinigame(); //v => mitigation = v
                }
                else
                {
                    yield return battleHandler.AbilitySystem.Run(ctx.ability, ctx.actor, ctx.targets);
                }
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
            guardedThisTurn = false;
            healedThisTurn = false;
            partyWindow.RefreshUI();
        }

        private AIHandler.Snapshot CreateCombatSnapshot(Character enemy, List<AbilityData> abilities)
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
                        if (c != null && !c.Evaluate(new ActionContext(gameManager, battleHandler, enemy, abilities[i].dealsDamage && abilities[i].amountDamage < 0 ? battleHandler.allies.members : battleHandler.enemies.members, abilities[i], null), out string reason))
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
                    /*bossHp: enemy.health,
                    bossMaxHp: enemy.maxHealth,
                    bossAttackPower: enemy.attackPower.Value,
                    bossDefense: enemy.defense.Value,
                    bossStatusEffects: enemy.GetStatusEffects().Select(s =>
                        (id: s.data.name,
                         duration: s.Duration,
                         stacks: s.Stacks,
                         tags: s.data.tags.AsEnumerable()) // force IEnumerable<string>
                    ),*/
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
                    previousBossAbilityId: lastUsedAbility.ContainsKey(enemy) ? lastUsedAbility[enemy].name : string.Empty,
                    previousPlayerParty: (previousTurn.playerPlan != null && previousTurn.playerPlan.Count > 0)
                        ? previousTurn.playerPlan.Select(tc => (id: tc.actor.id, hp: tc.actor.hp, alive: tc.actor.isAlive, statusEffectCount: tc.actor.statusEffectCount))
                        : Enumerable.Empty<(string id, int hp, bool alive, int statusEffectCount)>(),
                    previousEnemyParty: (previousTurn.enemyPlan != null && previousTurn.enemyPlan.Count > 0)
                        ? previousTurn.enemyPlan.Select(tc => (id: tc.actor.id, hp: tc.actor.hp, alive: tc.actor.isAlive, statusEffectCount: tc.actor.statusEffectCount))
                        : Enumerable.Empty<(string id, int hp, bool alive, int statusEffectCount)>()
                /*validTargets: battleHandler.allies.members
                    .Where(p => p.isAlive)
                    .Select(p => p.data.name)*/
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
                int j = Random.Range(0, i + 1);
                (abilityPool[i], abilityPool[j]) = (abilityPool[j], abilityPool[i]);
            }

            // Pick the first that passes conditions
            foreach (AbilityData a in abilityPool)
            {
                if (AbilityCanExecute(actor, a))
                    return a;
            }
            return null;
        }

        // Helper for dumb AI to check if an ability can be executed based on its conditions
        private bool AbilityCanExecute(Character actor, AbilityData ability)
        {
            var ctx = new ActionContext { actor = actor, battle = battleHandler, ability = ability, targets = System.Array.Empty<Character>() };
            if (ability.conditions == null)
                return true;
            foreach (ConditionData c in ability.conditions)
                if (c != null && !c.Evaluate(ctx, out _))
                    return false;
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