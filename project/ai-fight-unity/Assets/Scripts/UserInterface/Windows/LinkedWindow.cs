using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using dev.susybaka.Shared.UI;

namespace dev.susybaka.TurnBasedGame.UI
{
    public class LinkedWindow : HudWindow
    {
        public HudWindow linkedWindow;

        private void OnEnable()
        {
            interactive = false;

            if (linkedWindow == null)
            {
                Debug.LogWarning($"[{nameof(LinkedWindow)}] Linked window is not assigned in the inspector.", this);
                return;
            }

            linkedWindow.onOpen.AddListener(OpenWindow);
            linkedWindow.onClose.AddListener(CloseWindow);

            if (linkedWindow.isOpen)
                OpenWindow();
            else
                CloseWindow();
        }

        private void OnDisable()
        {
            interactive = false;

            if (linkedWindow == null)
                return;

            linkedWindow.onOpen.RemoveListener(OpenWindow);
            linkedWindow.onClose.RemoveListener(CloseWindow);
        }
    }
}

