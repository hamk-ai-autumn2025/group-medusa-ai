using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using dev.susybaka.Shared.UI;

namespace dev.susybaka.TurnBasedGame.UI
{
    public class LabelWindow : HudWindow
    {
        [SerializeField] private TextMeshProUGUI label;

        public void SetText(string text)
        {
            if (label == null)
                return;

            label.text = text;
        }

        public void ClearText()
        {
            if (label == null)
                return;
            
            label.text = string.Empty;
        }
    }
}