using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace dev.susybaka.TurnBasedGame.UI
{
    public class TextPopup : HudPopup
    {
        private TextMeshProUGUI label;

        protected override void Awake()
        {
            base.Awake();
            label = GetComponentInChildren<TextMeshProUGUI>(true);
            label.text = string.Empty;
        }

        public void Show(string s)
        {
            label.text = s;
            base.Show();
        }
    }
}