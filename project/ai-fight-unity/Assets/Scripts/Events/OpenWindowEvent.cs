using UnityEngine;

namespace dev.susybaka.TurnBasedGame.Events
{
    [CreateAssetMenu(fileName = "New Open Window Event", menuName = "Turn Based Game/Events/Open Window Event")]
    public class OpenWindowEvent : ScriptableObject
    {
        public GameStateWindowType window = GameStateWindowType.none;

        public void TriggerEvent()
        {
            if (!GameManager.Available)
            {
                Debug.LogWarning("GameManager not currently available.");
                return;
            }

            GameManager.Instance.OpenWindow(window);
        }
    }
}