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
        public AbilityData ability { get; init; } // can be null
        public StatusEffectData status { get; init; } // can be null

        public ActionContext(GameManager game, BattleHandler battle, Character actor, IList<Character> targets, AbilityData ability, StatusEffectData status)
        {
            this.game = game;
            this.battle = battle;
            this.actor = actor;
            this.targets = targets;
            this.ability = ability;
            this.status = status;
        }

        public ActionContext(StatusEffectContext ctx)
        {
            this.game = ctx.game;
            this.battle = ctx.battle;
            this.actor = ctx.sourceActor;
            this.targets = ctx.targets;
            this.ability = ctx.sourceAbility;
            this.status = ctx.data;
        }
    }

    public enum EffectType { misc, buff, debuff, none }

    public readonly struct StatusEffectContext 
    {
        public GameManager game { get; init; }
        public BattleHandler battle { get; init; }
        public StatusEffectData data { get; init; }
        public Character sourceActor { get; init; } // can be identical to target
        public IList<Character> targets { get; init; }
        public AbilityData sourceAbility { get; init; } // can be null
        public int duration { get; init; }
        public int stacks { get; init; }

        public StatusEffectContext(GameManager game, BattleHandler battle, StatusEffectData data, Character sourceActor, IList<Character> targets, AbilityData sourceAbility, int duration, int stacks)
        {
            this.game = game;
            this.battle = battle;
            this.data = data;
            this.sourceActor = sourceActor;
            this.targets = targets;
            this.sourceAbility = sourceAbility;
            this.duration = duration;
            this.stacks = stacks;
        }
    }
}