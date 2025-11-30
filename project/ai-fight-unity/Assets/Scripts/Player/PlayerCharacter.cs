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

        private Transform cameraFollowTarget;

#if UNITY_EDITOR
        [Header("Editor")]
        [SerializeField] private ItemData giveItem;
        [SerializeField] private int giveItemAmount = 1;
        [NaughtyAttributes.Button("Give Item")]
        public void GiveItem()
        {
            Inventory.Add(giveItem, giveItemAmount);
        }
        [NaughtyAttributes.Button("Log Inventory")]
        public void LogInventory()
        {
            var items = Inventory.NonZeroEntries();
            Debug.Log("Player Inventory has the following items:");
            foreach (var item in items)
            {
                Debug.Log(string.Format("- {0} x{1}", item.item.displayName, item.count));
            }
        }
#endif

        protected override void Awake()
        {
            base.Awake();
            battleController = GetComponentInChildren<PlayerBattleController>();
            overworldController = GetComponentInChildren<PlayerOverworldController>();
            characterTransform = overworldController.transform;
            cameraFollowTarget = characterTransform;
        }

        private void Update()
        {
            if (isFighting && isFighting != wasFighting)
            {
                wasFighting = isFighting;
                battleController.disabled = true;
                overworldController.disabled = true;
                cameraFollowTarget = transform;
            }
            else if (!isFighting && isFighting != wasFighting)
            {
                wasFighting = isFighting;
                battleController.disabled = true;
                overworldController.disabled = false;
                cameraFollowTarget = characterTransform;
            }

            cameraTarget.position = cameraFollowTarget.position;
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