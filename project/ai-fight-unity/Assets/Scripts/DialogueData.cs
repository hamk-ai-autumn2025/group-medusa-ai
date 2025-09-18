using UnityEngine;
using UnityEngine.Events;
using static dev.susybaka.TurnBasedGame.Core.GlobalData;

[CreateAssetMenu(fileName = "New Dialogue Data", menuName = "Turn Based Game/Dialogue Data")]
public class DialogueData : ScriptableObject
{
    public string dialogueName = "New Dialogue";
    public DialogueString[] dialogue;
    public UnityEvent onComplete;
}
