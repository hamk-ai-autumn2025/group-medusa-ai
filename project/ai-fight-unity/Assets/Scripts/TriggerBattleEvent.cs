using UnityEngine;
using dev.susybaka.TurnBasedGame.Core;
using dev.susybaka.TurnBasedGame.Characters;

[CreateAssetMenu(fileName = "New Trigger Battle Event", menuName = "Turn Based Game/Events/Trigger Battle Event")]
public class TriggerBattleEvent : ScriptableObject
{
    public CharacterData opponent;
    
    public void TriggerEvent()
    {
        if (BattleManager.Instance == null)
        {
            Debug.LogWarning("BattleManager instance not found in the scene.");
            return;
        }

        BattleManager.Instance.StartBattle(opponent);
    }
}
