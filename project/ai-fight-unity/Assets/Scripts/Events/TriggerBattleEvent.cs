using UnityEngine;
using dev.susybaka.TurnBasedGame.Characters.Data;

namespace dev.susybaka.TurnBasedGame.Events
{
    [CreateAssetMenu(fileName = "New Trigger Battle Event", menuName = "Turn Based Game/Events/Trigger Battle Event")]
    public class TriggerBattleEvent : ScriptableObject
    {
        public CharacterData opponent;

        public void TriggerEvent()
        {
            if (!GameManager.BattleHandlerAvailable)
            {
                Debug.LogWarning("BattleHandler is not currently available in GameManager.");
                return;
            }

            GameManager.Instance.BattleHandler.StartBattle(opponent);
        }
    }
}