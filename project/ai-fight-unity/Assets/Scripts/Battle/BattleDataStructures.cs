using System.Collections.Generic;
using dev.susybaka.TurnBasedGame.Battle.Data;
using dev.susybaka.TurnBasedGame.Characters;

namespace dev.susybaka.TurnBasedGame.Battle
{
    public enum BattlePhase { planning, playerExec, enemyPlanning, enemyExec, victory, defeat }

    public readonly struct Intent
    {
        public Character actor { get; init; }
        public AbilityData ability { get; init; }
        public IList<Character> targets { get; init; }   // can be empty for self/no-target

        public Intent(Character actor, AbilityData ability, IList<Character> targets)
        {
            this.actor = actor;
            this.ability = ability;
            this.targets = targets;
        }
    }

    public readonly struct Turn
    {
        public int turnNumber { get; init; }
        public IList<Choice> playerPlan { get; init; }
        public IList<Choice> enemyPlan { get; init; }
        public Turn(int turnNumber, IList<Choice> playerIntents, IList<Choice> enemyIntents)
        {
            this.turnNumber = turnNumber;
            this.playerPlan = playerIntents;
            this.enemyPlan = enemyIntents;
        }

        public readonly struct Choice
        {
            public Actor actor { get; init; }
            public AbilityData ability { get; init; }
            public IList<Actor> targets { get; init; }   // can be empty for self/no-target

            public Choice(Actor actor, AbilityData ability, IList<Actor> targets)
            {
                this.actor = actor;
                this.ability = ability;
                this.targets = targets;
            }
        }

        public readonly struct Actor
        {
            public string id { get; init; }
            public int hp { get; init; }
            public int maxHp { get; init; }
            public int actionPoints { get; init; }
            public int maxActionPoints { get; init; }
            public bool isAlive { get; init; }
            public int statusEffectCount { get; init; }
            public Actor(string id, int hp, int maxHp, int actionPoints, int maxActionPoints, bool isAlive, int statusEffectCount)
            {
                this.id = id;
                this.hp = hp;
                this.maxHp = maxHp;
                this.actionPoints = actionPoints;
                this.maxActionPoints = maxActionPoints;
                this.isAlive = isAlive;
                this.statusEffectCount = statusEffectCount;
            }
        }
    }

    public readonly struct EnemyMood
    {
        public string id { get; init; }
        public int aggression { get; init; } // higher = more likely to attack
        public int fear { get; init; }       // higher = more likely to flee or defend
        public int respect { get; init; }    // higher = more likely to accept non-violent options
        public int pity { get; init; }      // higher = more likely to use non-lethal options and end the fight peacefully
        public int curiosity { get; init; }  // higher = more likely to use non-attack abilities
        public int desperation { get; init; } // higher = more likely to use risky abilities
        public string previousDialogue { get; init; }

        public EnemyMood(string id, int aggression, int fear, int respect, int pity, int curiosity, int desperation, string previousDialogue)
        {
            this.id = id;
            this.aggression = aggression;
            this.fear = fear;
            this.respect = respect;
            this.pity = pity;
            this.curiosity = curiosity;
            this.desperation = desperation;
            this.previousDialogue = previousDialogue;
        }
    }

    public enum TargetGroup { self, ally, allies, enemy, enemies, any }

    public readonly struct ActionContext
    {
        public GameManager game { get; init; }
        public BattleHandler battle { get; init; }
        public Character actor { get; init; }
        public IList<Character> targets { get; init; }  // empty for no-target abilities
        public AbilityData ability { get; init; }

        public ActionContext(GameManager game, BattleHandler battle, Character actor, IList<Character> targets, AbilityData ability)
        {
            this.game = game;
            this.battle = battle;
            this.actor = actor;
            this.targets = targets;
            this.ability = ability;
        }
    }

    public static class Extensions
    {
        public static IList<Turn.Choice> ToChoices(this IList<Intent> intents)
        {
            List<Turn.Choice> choices = new List<Turn.Choice>(intents.Count);
            foreach (Intent intent in intents)
            {
                choices.Add(intent.ToChoice());
            }
            return choices;
        }

        public static Turn.Choice ToChoice(this Intent intent)
        {
            return new Turn.Choice(
                new Turn.Actor(
                    intent.actor.data.name,
                    intent.actor.health,
                    intent.actor.maxHealth,
                    intent.actor.ActionPoints,
                    intent.actor.MaxActionPoints,
                    intent.actor.isAlive,
                    intent.actor.GetStatusEffects().Length
                ),
                intent.ability,
                intent.targets != null ? new List<Turn.Actor>(
                    intent.targets.Count > 0
                        ? System.Linq.Enumerable.ToList(
                            System.Linq.Enumerable.Select(
                                intent.targets,
                                t => new Turn.Actor(
                                    t.data.name,
                                    t.health,
                                    t.maxHealth,
                                    t.ActionPoints,
                                    t.MaxActionPoints,
                                    t.isAlive,
                                    t.GetStatusEffects().Length
                                )
                            )
                        )
                        : new List<Turn.Actor>()
                ) : new List<Turn.Actor>()
            );
        }
    }

    public static class Extensions
    {
        public static IList<Turn.Choice> ToChoices(this IList<Intent> intents)
        {
            List<Turn.Choice> choices = new List<Turn.Choice>(intents.Count);
            foreach (Intent intent in intents)
            {
                choices.Add(intent.ToChoice());
            }
            return choices;
        }

        public static Turn.Choice ToChoice(this Intent intent)
        {
            return new Turn.Choice(
                new Turn.Actor(
                    intent.actor.data.name,
                    intent.actor.health,
                    intent.actor.maxHealth,
                    intent.actor.ActionPoints,
                    intent.actor.MaxActionPoints,
                    intent.actor.isAlive,
                    intent.actor.GetStatusEffects().Length
                ),
                intent.ability,
                intent.targets != null ? new List<Turn.Actor>(
                    intent.targets.Count > 0
                        ? System.Linq.Enumerable.ToList(
                            System.Linq.Enumerable.Select(
                                intent.targets,
                                t => new Turn.Actor(
                                    t.data.name,
                                    t.health,
                                    t.maxHealth,
                                    t.ActionPoints,
                                    t.MaxActionPoints,
                                    t.isAlive,
                                    t.GetStatusEffects().Length
                                )
                            )
                        )
                        : new List<Turn.Actor>()
                ) : new List<Turn.Actor>()
            );
        }
    }

    public static class Extensions
    {
        public static IList<Turn.Choice> ToChoices(this IList<Intent> intents)
        {
            List<Turn.Choice> choices = new List<Turn.Choice>(intents.Count);
            foreach (Intent intent in intents)
            {
                choices.Add(intent.ToChoice());
            }
            return choices;
        }

        public static Turn.Choice ToChoice(this Intent intent)
        {
            return new Turn.Choice(
                new Turn.Actor(
                    intent.actor.data.name,
                    intent.actor.health,
                    intent.actor.maxHealth,
                    intent.actor.ActionPoints,
                    intent.actor.MaxActionPoints,
                    intent.actor.isAlive,
                    intent.actor.GetStatusEffects().Length
                ),
                intent.ability,
                intent.targets != null ? new List<Turn.Actor>(
                    intent.targets.Count > 0
                        ? System.Linq.Enumerable.ToList(
                            System.Linq.Enumerable.Select(
                                intent.targets,
                                t => new Turn.Actor(
                                    t.data.name,
                                    t.health,
                                    t.maxHealth,
                                    t.ActionPoints,
                                    t.MaxActionPoints,
                                    t.isAlive,
                                    t.GetStatusEffects().Length
                                )
                            )
                        )
                        : new List<Turn.Actor>()
                ) : new List<Turn.Actor>()
            );
        }
    }

    public static class Extensions
    {
        public static IList<Turn.Choice> ToChoices(this IList<Intent> intents)
        {
            List<Turn.Choice> choices = new List<Turn.Choice>(intents.Count);
            foreach (Intent intent in intents)
            {
                choices.Add(intent.ToChoice());
            }
            return choices;
        }

        public static Turn.Choice ToChoice(this Intent intent)
        {
            return new Turn.Choice(
                new Turn.Actor(
                    intent.actor.data.name,
                    intent.actor.health,
                    intent.actor.maxHealth,
                    intent.actor.ActionPoints,
                    intent.actor.MaxActionPoints,
                    intent.actor.isAlive,
                    intent.actor.GetStatusEffects().Length
                ),
                intent.ability,
                intent.targets != null ? new List<Turn.Actor>(
                    intent.targets.Count > 0
                        ? System.Linq.Enumerable.ToList(
                            System.Linq.Enumerable.Select(
                                intent.targets,
                                t => new Turn.Actor(
                                    t.data.name,
                                    t.health,
                                    t.maxHealth,
                                    t.ActionPoints,
                                    t.MaxActionPoints,
                                    t.isAlive,
                                    t.GetStatusEffects().Length
                                )
                            )
                        )
                        : new List<Turn.Actor>()
                ) : new List<Turn.Actor>()
            );
        }
    }

    public static class Extensions
    {
        public static IList<Turn.Choice> ToChoices(this IList<Intent> intents)
        {
            List<Turn.Choice> choices = new List<Turn.Choice>(intents.Count);
            foreach (Intent intent in intents)
            {
                choices.Add(intent.ToChoice());
            }
            return choices;
        }

        public static Turn.Choice ToChoice(this Intent intent)
        {
            return new Turn.Choice(
                new Turn.Actor(
                    intent.actor.data.name,
                    intent.actor.health,
                    intent.actor.maxHealth,
                    intent.actor.ActionPoints,
                    intent.actor.MaxActionPoints,
                    intent.actor.isAlive,
                    intent.actor.GetStatusEffects().Length
                ),
                intent.ability,
                intent.targets != null ? new List<Turn.Actor>(
                    intent.targets.Count > 0
                        ? System.Linq.Enumerable.ToList(
                            System.Linq.Enumerable.Select(
                                intent.targets,
                                t => new Turn.Actor(
                                    t.data.name,
                                    t.health,
                                    t.maxHealth,
                                    t.ActionPoints,
                                    t.MaxActionPoints,
                                    t.isAlive,
                                    t.GetStatusEffects().Length
                                )
                            )
                        )
                        : new List<Turn.Actor>()
                ) : new List<Turn.Actor>()
            );
        }
    }
}