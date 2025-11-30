using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using dev.susybaka.TurnBasedGame.Characters;
using dev.susybaka.TurnBasedGame.Dialogue.Data;

namespace dev.susybaka.TurnBasedGame.Dialogue
{
    public class TriggerDialogue : MonoBehaviour
    {
        private DialogueHandler dialogueHandler;

        public DialogueData data;
        public bool singleUse = false;
        public UnityEvent<Character> onComplete;

        private bool done = false;

        private void Awake()
        {
            done = false;
        }

        public void Trigger()
        {
            Trigger(null);
        }

        public void Trigger(Character character)
        {
            if (done)
                return;

            if (singleUse)
                done = true;

            if (dialogueHandler == null)
            {
                if (GameManager.DialogueHandlerAvailable)
                    dialogueHandler = GameManager.Instance.DialogueHandler;
            }

            if (dialogueHandler != null)
            {
                dialogueHandler.StartDialogue(data, new DialogueContext(null, null, null, null), () => { if (character != null) { onComplete.Invoke(character); } });
            }
        }
    }
}