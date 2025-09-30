using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using dev.susybaka.Shared.UI;

namespace dev.susybaka.TurnBasedGame.UI
{
    public class GameStateWindow : HudWindow
    {
        [Header("Game State Window")]
        [SerializeField] private DialogueWindow dialogueBox;

        public DialogueWindow DialogueBox => dialogueBox;
    }
}