using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using dev.susybaka.TurnBasedGame.Battle.Data;
using dev.susybaka.TurnBasedGame.Input;
using dev.susybaka.Shared.UI;

namespace dev.susybaka.TurnBasedGame.UI
{
    public class AttackMinigameWindow : HudWindow
    {
        InputHandler input;

        [SerializeField] private Slider[] attackSliders;
        [SerializeField] private Vector2 minigameRange = new Vector2(0, 100);
        [SerializeField] private Vector2 targetRange = new Vector2(0, 50);
        [SerializeField] private Vector2 attackDamageRange = new Vector2(2, 50);

        private Slider[] targetSliders;
        private CanvasGroupGroup[] groups;
        private List<MinigameInstance> activeMinigames = new List<MinigameInstance>();

        class MinigameInstance
        {
            public int index;
            public Slider slider;
            public CanvasGroupGroup group;
            public bool isRunning;
            public bool isFinished;
            public float startTime;
            public float targetValue;
            public Action Stop; // callback that cancels the tween + marks done

            public MinigameInstance(int index, Slider slider, CanvasGroupGroup group, bool isRunning, bool isFinished, float startTime, float targetValue, Action stop)
            {
                this.index = index;
                this.slider = slider;
                this.group = group;
                this.isRunning = isRunning;
                this.isFinished = isFinished;
                this.startTime = startTime;
                this.targetValue = targetValue;
                Stop = stop;
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (targetRange.x < minigameRange.x)
                targetRange.x = minigameRange.x;
            if (targetRange.x > minigameRange.y)
                targetRange.x = minigameRange.y;
            if (targetRange.y < minigameRange.x)
                targetRange.y = minigameRange.x;
            if (targetRange.y > minigameRange.y)
                targetRange.y = minigameRange.y;

            if (attackDamageRange.x < minigameRange.x)
                attackDamageRange.x = minigameRange.x;
            if (attackDamageRange.x > minigameRange.y)
                attackDamageRange.x = minigameRange.y;
            if (attackDamageRange.y < minigameRange.x)
                attackDamageRange.y = minigameRange.x;
            if (attackDamageRange.y > minigameRange.y)
                attackDamageRange.y = minigameRange.y;
        }
#endif

        protected override void Awake()
        {
            base.Awake();

            targetSliders = new Slider[attackSliders.Length];
            groups = new CanvasGroupGroup[attackSliders.Length];
            for (int i = 0; i < attackSliders.Length; i++)
            {
                attackSliders[i].minValue = minigameRange.x;
                attackSliders[i].maxValue = minigameRange.y;

                if (attackSliders[i].TryGetComponent(out CanvasGroupGroup g))
                {
                    groups[i] = g;
                }

                foreach (Transform child in attackSliders[i].transform)
                {
                    if (child.TryGetComponent(out Slider s))
                    {
                        targetSliders[i] = s;
                        targetSliders[i].minValue = attackSliders[i].minValue;
                        targetSliders[i].maxValue = attackSliders[i].maxValue;
                    }
                }
            }
        }

        private void Update()
        {
            if (!input.ConfirmInput && !input.JumpInput)
                return;

            // Choose one active minigame:
            var candidates = activeMinigames
                .Where(m => m.isRunning && !m.isFinished)
                .ToList();

            if (candidates.Count == 0)
                return;

            // Pick the instance closest to its own target.
            // Tiebreakers: earlier startTime, then “furthest along” (lower slider.value).
            var chosen = candidates
                .OrderBy(m => Mathf.Abs(m.slider.value - m.targetValue))
                .ThenBy(m => m.startTime)
                .ThenBy(m => m.slider.value)
                .First();

            // Consume press for this frame
            //input.ConfirmInput = false;

            // Tell that minigame to stop
            chosen.isFinished = true;
            chosen.Stop();
            activeMinigames.Remove(chosen);
        }

        public override void Initialize(GameManager manager)
        {
            base.Initialize(manager);

            input = manager.Input;
        }

        public void HideMinigame(int index)
        {
            CanvasGroupGroup group = groups[index];
            group.ToggleGroup(false);
        }

        public IEnumerator IE_ProcessMinigame(int index, AbilityData ability, Action<(float, int)> onFinish)
        {
            Slider slider = attackSliders[index];
            Slider targetSlider = targetSliders[index];
            CanvasGroupGroup group = groups[index];
            bool done = false;
            int targetValue = Mathf.RoundToInt(UnityEngine.Random.Range(targetRange.x, targetRange.y));
            MinigameInstance instance = new MinigameInstance(index, slider, group, false, false, Time.time, targetValue, () => done = true);

            // Register active minigame
            activeMinigames.Add(instance);

            // Hide sliders until start
            group.SetAlpha(0f);
            group.ToggleGroup(true);

            // Wait before starting for order of characters
            // This should be replaced with the actual turn order later
            yield return new WaitForSeconds(index);

            // Show sliders after initial wait
            group.SetAlpha(1f);

            targetSlider.value = targetValue;
            slider.value = slider.maxValue;

            // Small delay before after showing slider to let player prepare
            yield return new WaitForSeconds(1f);

            // Set slider active
            instance.isRunning = true;

            LeanTween.value(slider.gameObject, slider.maxValue, 0, ability.attackSpeed).setOnUpdate((float value) => slider.value = value).setEase(LeanTweenType.linear).setOnComplete(() => done = true);

            while (!done)
                yield return null;

            if (instance.isRunning && !instance.isFinished)
            {
                // If we exited because of time running out, remove from active minigames
                instance.isFinished = true;
                instance.Stop();
                activeMinigames.Remove(instance);
            }
            else if (instance.isFinished)
            {
                // Stopped by player, already removed from active minigames
                // Just stop the tween to freeze the slider on spot
                LeanTween.cancel(slider.gameObject);
            }

            float distance = Mathf.Abs(slider.value - targetSlider.value);

            // Interpret attackDamageRange.x = best distance threshold (e.g. 2)
            // and attackDamageRange.y = worst distance threshold (e.g. 50)
            float bestDist = attackDamageRange.x;
            float worstDist = attackDamageRange.y;

            // Safety to avoid divide-by-zero if someone misconfigures it
            if (worstDist <= bestDist)
            {
                worstDist = bestDist + 0.0001f;
            }

            // t = 0 when distance <= bestDist
            // t = 1 when distance >= worstDist
            // smooth 0–1 in between
            float t = Mathf.InverseLerp(bestDist, worstDist, distance);

            // goodness: 1 at best, 0 at worst
            float goodness = 1f - t;

            // Damage modifier
            float damageModifier = Mathf.Lerp(ability.minimumDamageModifier, 1.0f, goodness);

            // Accuracy modifier
            float accuracy01 = Mathf.Lerp(ability.minimumAccuracyModifier, 1.0f, goodness);
            //int accuracyPercent = Mathf.RoundToInt(accuracy01 * 100.0f);

            // Final damage
            int baseDamage = ability.amountDamage;
            int finalDamage = Mathf.RoundToInt(baseDamage * damageModifier);

            // Callback with (accuracy, finalDamage)
            onFinish?.Invoke((accuracy01, finalDamage));

            yield return null;
        }
    }
}