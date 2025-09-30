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
}