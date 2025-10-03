using System.Collections;
using System.Collections.Generic;
using System.Linq;
using dev.susybaka.TurnBasedGame.AI;
using dev.susybaka.TurnBasedGame.Battle.Data;
using dev.susybaka.TurnBasedGame.Characters;
using dev.susybaka.TurnBasedGame.Minigame;
using dev.susybaka.TurnBasedGame.UI;
using Unity.VisualScripting;
using UnityEngine;

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

        private GameManager gameManager;
        private BattleHandler battleHandler;
        private HudNavigationHandler nav;
        private MinigameHandler minigameHandler;
        private PartyWindow partyWindow;
        private bool initialized = false;

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

                yield return battleHandler.AbilitySystem.Run(intent.ability, intent.actor, intent.targets);
                if (CheckWinLose())
                    yield break;
            }
            playerPlan.Clear();
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
                // Currently only supports single-target abilities and very basic conditions,
                // Relying on the descriptions of abilities to guide the AI
                AIHandler.Snapshot snap = AIHandler.BuildSnapshot(
                    turn: currentTurn,
                    bossHp: enemy.health,
                    bossAttackPower: enemy.attackPower.Value,
                    bossDefense: enemy.defense.Value,
                    bossStatusEffects: enemy.GetStatusEffects().Select(s =>
                        (id: s.data.name,
                         duration: s.Duration,
                         stacks: s.Stacks,
                         description: s.data.description,
                         tags: s.data.tags.AsEnumerable()) // force IEnumerable<string>
                    ),
                    bossCharacterDescription: enemy.data.description,
                    party: battleHandler.allies.members.Select(p =>
                        (id: p.data.name,
                         description: p.data.description,
                         hp: p.health,
                         alive: p.isAlive,
                         statusEffects: p.GetStatusEffects().Select(s =>
                             (id: s.data.name,
                              duration: s.Duration,
                              stacks: s.Stacks,
                              description: s.data.description,
                              tags: s.data.tags.AsEnumerable())
                         ))
                    ),
                    abilities: abilities.Select(a =>
                        (id: a.name,
                        description: a.description,
                        requiresTarget: a.requiresTarget,
                        tags: a.tags.AsEnumerable(), // ensure IEnumerable<string>
                        conditions: a.GetConditionDescriptions().AsEnumerable())
                    ),
                    knowledge: enemy.Knowledge.Select(k => (id: k.name, text: k.text)),

                    validTargets: battleHandler.allies.members
                        .Where(p => p.isAlive)
                        .Select(p => p.data.name)
                );

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
                    Debug.Log($"AIHandler returned the following:\ntarget_id '{picked.target_id}'\nability_id '{picked.ability_id}'\ntarget_id '{picked.target_id}'\nrationale '{picked.rationale}'");
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

                //yield return battleHandler.AbilitySystem.Run(ctx.ability, ctx.actor, ctx.targets);
                if (CheckWinLose())
                    yield break;
            }
            enemyPlan.Clear();
            partyWindow.RefreshUI();
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