using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using dev.susybaka.TurnBasedGame;
using dev.susybaka.TurnBasedGame.Globals;

namespace dev.susybaka.Shared.UI
{
    [RequireComponent(typeof(CanvasGroup))]
    public class HudWindow : MonoBehaviour
    {
        protected GameManager gameManager;
        protected CanvasGroup group;
        public CanvasGroup Group { get { return group; } }

        [Header("Base Hud Window")]
        public bool isOpen = false;
        public bool isActive = false;
        public Flag isInteractable = new Flag("isInteractable", new List<Flag.Value> { new Flag.Value("base", true) }, FlagAggregateLogic.AllTrue);

        [HideInInspector]
        public UnityEvent onOpen;
        [HideInInspector]
        public UnityEvent onClose;

        protected bool initialized = false;
        protected bool interactive = true;

        protected virtual void Awake()
        {
            group = GetComponent<CanvasGroup>();
        }

        public virtual void Initialize(GameManager manager)
        {
            if (initialized)
                return;

            initialized = true;
            gameManager = manager;
        }

        public virtual void OpenWindow()
        {
            if (group == null)
            {
                Awake();
            }

            isOpen = true;
            if (interactive)
                isActive = true;
            isInteractable.SetFlag("closeWindow", true);
            UpdateInteractableState();
            group.alpha = 1f;

            onOpen.Invoke();
        }

        public virtual void CloseWindow()
        {
            if (group == null)
            {
                Awake();
            }

            isOpen = false;
            if (interactive)
                isActive = false;
            isInteractable.SetFlag("closeWindow", false);
            UpdateInteractableState();
            group.alpha = 0f;

            onClose.Invoke();
        }

        public void EnableInteractions()
        {
            isInteractable.SetFlag("enableInteractions", true);
            UpdateInteractableState();
        }

        public void DisableInteractions()
        {
            isInteractable.SetFlag("enableInteractions", false);
            UpdateInteractableState();
        }

        private void UpdateInteractableState()
        {
            if (group == null)
                return;

            if (isInteractable.value)
            {
                group.interactable = true;
                group.blocksRaycasts = true;
            }
            else
            {
                group.interactable = false;
                group.blocksRaycasts = false;
            }
        }
    }
}