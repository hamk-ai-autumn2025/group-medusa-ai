using UnityEngine;
using UnityEngine.Events;

namespace dev.susybaka.TurnBasedGame.Dialogue.Data
{
    [CreateAssetMenu(fileName = "New Dialogue Data", menuName = "Turn Based Game/Characters/Dialogue")]
    public class DialogueData : ScriptableObject
    {
        public string dialogueName = "New Dialogue";
        public DialogueString[] dialogue;
        public UnityEvent onComplete;
    }
}