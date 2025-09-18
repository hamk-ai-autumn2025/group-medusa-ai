using dev.susybaka.TurnBasedGame.Characters;
using dev.susybaka.TurnBasedGame.Inventory;
using UnityEngine;

namespace dev.susybaka.TurnBasedGame.Player
{
    public class PlayerCharacter : Character
    {
        [Header("Player")]
        public int Level;
        public int Experience;
        public InventoryObject Inventory;

        [Header("Player - Components")]
        public PlayerBattleController battleController;
        public PlayerOverworldController overworldController;
        public Transform cameraTarget;

        private void Awake()
        {
            battleController = GetComponentInChildren<PlayerBattleController>();
            overworldController = GetComponentInChildren<PlayerOverworldController>();
        }

        private void Update()
        {
            if (isFighting)
            {
                battleController.disabled = false;
                overworldController.disabled = true;
                cameraTarget.position = Vector3.zero;
            }
            else
            {
                battleController.disabled = true;
                overworldController.disabled = false;
                cameraTarget.position = overworldController.transform.position;
            }
        }

        public void LevelUp()
        {
            // Implement level up logic
        }

        public void GainExperience(int amount)
        {
            // Update experience and check for level up
        }
    }
}