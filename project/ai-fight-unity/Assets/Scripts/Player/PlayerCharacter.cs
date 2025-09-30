using dev.susybaka.TurnBasedGame.Battle.Data;
using dev.susybaka.TurnBasedGame.Characters;
using dev.susybaka.TurnBasedGame.Items;
using UnityEngine;

namespace dev.susybaka.TurnBasedGame.Player
{
    public class PlayerCharacter : Character
    {
        [Header("Player")]
        public int Level;
        public int Experience;

        [Header("Player - Components")]
        public PlayerBattleController battleController;
        public PlayerOverworldController overworldController;
        public Transform cameraTarget;

        protected override void Awake()
        {
            base.Awake();
            battleController = GetComponentInChildren<PlayerBattleController>();
            overworldController = GetComponentInChildren<PlayerOverworldController>();
        }

        private void Update()
        {
            if (isFighting && isFighting != wasFighting)
            {
                wasFighting = isFighting;
                battleController.disabled = true;
                overworldController.disabled = true;
                cameraTarget.position = Vector3.zero;
            }
            else if (!isFighting && isFighting != wasFighting)
            {
                wasFighting = isFighting;
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