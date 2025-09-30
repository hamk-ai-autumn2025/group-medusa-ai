using System.Collections;
using System.Collections.Generic;
using dev.susybaka.TurnBasedGame.Dialogue.Data;
using UnityEngine;

namespace dev.susybaka.TurnBasedGame.Dialogue
{
    public class TriggerDialogue : MonoBehaviour
    {
        private DialogueHandler dialogueHandler;

        public DialogueData data;

        public void Trigger()
        {
            if (dialogueHandler == null)
            {
                if (GameManager.DialogueHandlerAvailable)
                    dialogueHandler = GameManager.Instance.DialogueHandler;
            }

            if (dialogueHandler != null)
            {
                dialogueHandler.StartDialogue(data);
            }
        }
    }
}