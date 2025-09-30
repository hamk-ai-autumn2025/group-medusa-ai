using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using dev.susybaka.Shared.UI;

namespace dev.susybaka.TurnBasedGame.UI
{
    public class DialogueWindow : HudWindow
    {
        public TextMeshProUGUI DialogueContents
        {
            get
            {
                if (dialogueContents == null)
                    dialogueContents = transform.Find("DialogueContentBox").GetChild(0).GetComponent<TextMeshProUGUI>();
                return dialogueContents;
            }
        }

        public TextMeshProUGUI DialogueNewLineIndicators
        {
            get
            {
                if (dialogueNewLineIndicators == null)
                    dialogueNewLineIndicators = transform.Find("DialogueContentBox").GetChild(1).GetComponent<TextMeshProUGUI>();
                return dialogueNewLineIndicators;
            }
        }

        public Image DialoguePortrait
        {
            get
            {
                if (dialoguePortrait == null)
                    dialoguePortrait = transform.Find("DialoguePortrait").GetComponent<Image>();
                return dialoguePortrait;
            }
        }

        private TextMeshProUGUI dialogueContents;
        private TextMeshProUGUI dialogueNewLineIndicators;
        private Image dialoguePortrait;
    }
}