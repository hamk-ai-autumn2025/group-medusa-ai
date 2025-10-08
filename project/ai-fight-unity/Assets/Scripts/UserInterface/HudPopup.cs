using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace dev.susybaka.TurnBasedGame.UI
{
    [RequireComponent(typeof(CanvasGroup))]
    public class HudPopup : MonoBehaviour
    {
        protected CanvasGroup group;

        [SerializeField] private float lifetime = 1f;
        [SerializeField] private float fadeIn = 0f;
        [SerializeField] private float fadeOut = 1f;
        [SerializeField] private Vector2 movement;
        [SerializeField] private bool destroyAfterDone = false;

        private Vector3 initialPosition;
        private bool isActive = false;
        private bool busy = false;
        private float timer = 0f;
        private LTDescr currentTween;

        protected virtual void Awake()
        {
            group = GetComponent<CanvasGroup>();
            timer = lifetime;
            initialPosition = transform.position;
        }

        protected virtual void Update()
        {
            if (isActive)
            {
                timer -= Time.deltaTime;
                if (movement != Vector2.zero)
                    transform.Translate(movement * Time.deltaTime);
                if (timer <= 0f && !busy)
                {
                    Hide();
                }
            }
        }

        public virtual void Refresh()
        {
            if (!isActive)
                Show();

            timer = lifetime;
            busy = false;
            if (currentTween != null)
                LeanTween.cancel(currentTween.id);
            group.alpha = 1f;
        }

        public virtual void Show()
        {
            busy = true;
            isActive = true;
            timer = lifetime;
            if (fadeIn > 0f)
            {
                currentTween = group.LeanAlpha(1f, fadeIn).setOnComplete(() => { currentTween = null; busy = false; });
            }
            else
            {
                group.alpha = 1f;
                busy = false;
            }
        }

        public virtual void Hide()
        {
            busy = true;
            if (fadeOut > 0f)
            {
                currentTween = group.LeanAlpha(0f, fadeOut).setOnComplete(() =>
                {
                    currentTween = null;
                    isActive = false;
                    busy = false;
                    if (destroyAfterDone) 
                        Destroy(gameObject);
                    else 
                        transform.position = initialPosition;
                });
            }
            else
            {
                group.alpha = 0f;
                isActive = false;
                busy = false;
                if (destroyAfterDone) 
                    Destroy(gameObject);
                else
                    transform.position = initialPosition;
            }
        }
    }
}