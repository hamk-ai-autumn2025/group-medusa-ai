using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using dev.susybaka.TurnBasedGame.Battle;
using dev.susybaka.TurnBasedGame.Battle.Data;
using dev.susybaka.Shared.Attributes;
using dev.susybaka.Shared.Audio;

namespace dev.susybaka.TurnBasedGame.Characters
{
    public class EdgingTrigger : MonoBehaviour
    {
        private Character character;
        private GameManager gameManager;
        private BattleHandler battleHandler;

        [SerializeField, NaughtyAttributes.Tag] private string edgingTag = "Edging";
        [SerializeField] private EffectData[] edgingEffects;
        [SerializeField] private SpriteRenderer edgeVisuals;
        [SerializeField, SoundName] private string edgeSound;

        private float edgeVisibleDuration = 0.2f; // Time in seconds the sprite stays fully visible before fading
        private float edgeFadeDuration = 0.5f;    // Time in seconds for the fade out
        private LTDescr edgeTween;                // Reference to the current tween

        private void Awake()
        {
            character = transform.GetComponentInParents<Character>();
            gameManager = GameManager.Instance;
            battleHandler = gameManager?.BattleHandler;

            if (edgeVisuals != null)
            {
                var color = edgeVisuals.color;
                color.a = 0f; // Start fully transparent
                edgeVisuals.color = color;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag(edgingTag))
            {
                ExecuteEffects();
                ShowVisual();
            }
        }

        private void ExecuteEffects()
        {
            if (gameManager == null || battleHandler == null)
                return;

            for (int i = 0; i < edgingEffects.Length; i++)
            {
                battleHandler.AbilitySystem.StartCoroutine(edgingEffects[i].Execute(new ActionContext(gameManager, battleHandler, character, new Character[] { character }, null, null, 0, 1.0f)));
            }
        }

        private void ShowVisual()
        {
            if (edgeVisuals == null)
                return;

            // Stop any running tween on this sprite
            if (edgeTween != null && LeanTween.isTweening(edgeVisuals.gameObject))
            {
                LeanTween.cancel(edgeVisuals.gameObject);
                edgeTween = null;
            }

            // Instantly set alpha to 1 (fully visible)
            var color = edgeVisuals.color;
            color.a = 1f;
            edgeVisuals.color = color;

            // After visible duration, start fading out
            edgeTween = LeanTween.value(edgeVisuals.gameObject, 1f, 0f, edgeFadeDuration)
                .setDelay(edgeVisibleDuration)
                .setOnUpdate((float val) =>
                {
                    var c = edgeVisuals.color;
                    c.a = val;
                    edgeVisuals.color = c;
                })
                .setOnComplete(() =>
                {
                    // Optionally, ensure alpha is 0 at the end
                    var c = edgeVisuals.color;
                    c.a = 0f;
                    edgeVisuals.color = c;
                    edgeTween = null;
                });

            if (!string.IsNullOrEmpty(edgeSound))
                AudioManager.Instance?.Play(edgeSound);
        }
    }
}