using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using dev.susybaka.TurnBasedGame.Battle.Data;
using dev.susybaka.TurnBasedGame.Characters;
using UnityEngine.Events;

namespace dev.susybaka.TurnBasedGame.Battle
{
    [System.Serializable]
    public class StatusEffect
    {
        private StatusEffectContext ctx;
        private AbilitySystem abilitySystem;

        public string name;
        public StatusEffectData data;
        [SerializeField] private int duration;
        public int Duration => duration;
        [SerializeField] private int stacks;
        public int Stacks => stacks;

        [HideInInspector] public UnityEvent onApply = new UnityEvent();
        [HideInInspector] public UnityEvent onRemove = new UnityEvent();
        [HideInInspector] public UnityEvent onTick = new UnityEvent();

        public StatusEffect(StatusEffectData data, int duration, int stacks)
        {
            this.data = data;
            name = data.displayName;
            this.duration = data.isInfinite ? 999 : duration;
            this.stacks = stacks;
        }

        public void Tick()
        {
            duration--;
            
            for (int i = 0; i < data.onTick.Count; i++)
            {
                abilitySystem.StartCoroutine(data.onTick[i].Execute(new ActionContext(ctx)));
            }

            onTick?.Invoke();

            if (duration <= 0 && ctx.targets != null)
            {
                for (int i = 0; i < ctx.targets.Count; i++)
                {
                    ctx.targets[i].RemoveStatusEffect(this);
                }
            }
        }

        public void Apply(StatusEffectContext ctx)
        {
            abilitySystem = ctx.battle.AbilitySystem;

            if (ctx.targets == null || ctx.targets.Count < 1)
            {
                this.ctx = new StatusEffectContext(ctx.game, ctx.battle, ctx.data, ctx.sourceActor, new List<Character>() { ctx.sourceActor }, ctx.sourceAbility, duration, stacks);
            }
            else
            {
                this.ctx = ctx;
            }

            for (int i = 0; i < data.onApply.Count; i++)
            {
                var test = new ActionContext(this.ctx);

                abilitySystem.StartCoroutine(data.onApply[i].Execute(test));
            }

            onApply?.Invoke();
        }

        public void Remove()
        {
            for (int i = 0; i < data.onRemove.Count; i++)
            {
                abilitySystem.StartCoroutine(data.onRemove[i].Execute(new ActionContext(ctx)));
            }

            onRemove?.Invoke();
        }

        public void Refresh()
        {
            if (data.allowRefresh)
            {
                duration = data.isInfinite ? 999 : duration + 1;
            }
        }

        public void AddStacks(int amount)
        {
            if (data.maxStacks > 1)
            {
                stacks += amount;
                if (stacks > data.maxStacks)
                {
                    stacks = data.maxStacks;
                }
            }
        }
    }
}