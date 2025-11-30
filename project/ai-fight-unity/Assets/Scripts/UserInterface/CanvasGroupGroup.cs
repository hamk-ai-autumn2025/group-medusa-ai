using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace dev.susybaka.TurnBasedGame.UI
{
    public class CanvasGroupGroup : MonoBehaviour
    {
        public CanvasGroup[] canvasGroups;
        private CanvasGroup mainGroup;

        private void Awake()
        {
            mainGroup = GetComponent<CanvasGroup>();
        }

        public void ToggleGroup(bool state)
        {
            if (state)
            {
                mainGroup.alpha = 1f;
            }
            else
            {
                mainGroup.alpha = 0f;
            }
        }

        public void SetAlpha(float alpha)
        {
            foreach (CanvasGroup group in canvasGroups)
            {
                group.alpha = alpha;
            }
        }
    }
}