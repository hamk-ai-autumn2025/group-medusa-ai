using UnityEngine;
using UnityEngine.Audio;
using dev.susybaka.TurnBasedGame.Combat;
using dev.susybaka.TurnBasedGame.Characters;
using dev.susybaka.TurnBasedGame.Player;
using dev.susybaka.Shared.Audio;
using dev.susybaka.Shared.Attributes;
using dev.susybaka.Shared.UI;
#if UNITY_EDITOR
using NaughtyAttributes;
#endif

namespace dev.susybaka.TurnBasedGame.Core
{
    public class BattleManager : MonoBehaviour
    {
        public static BattleManager Instance;

        public bool active = false;

        [Header("Battle Settings")]
        public GameObject overworldEnvironment;
        public GameObject battleEnvironment;
        public PlayerCharacter playerCharacter;
        public Vector3 playerBattlePosition;
        public Vector3 playerHeartPosition;
        public AudioMixerGroup musicMixerGroup;
        [SoundName] public string overworldMusic = "bgm_owl_dev";
        [SoundName] public string battleMusic = "bgm_btl_dev";
        public Transform m_camera;
        public DialogueBoxWindow dialogueBox;
        public DialogueData battleStartDialogue;
        public HudWindow partyMembers;
        public HudWindow ultimateBar;

        private TurnHandler turnHandler;
        private InputHandler input;

#if UNITY_EDITOR
        [Button("Start Battle")]
        void StartBattleEditor()
        {
            StartBattle(null);
        }
        [Button("End Battle")]
        void EndBattleEditor()
        {
            EndBattle();
        }
#endif

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            input = InputHandler.instance;
            m_camera.gameObject.SetActive(false);
            EndBattle();
        }

        public void StartBattle(CharacterData opponent)
        {
            // Initialize the turn handler and start the battle
            turnHandler = new TurnHandler();

            input.SetInputLayer("Battle");

            dialogueBox.SetHorizontalScale(dialogueBox.horizontalScale / 1.5f);
            dialogueBox.SetPortraitVisibility(false);
            dialogueBox.OpenWindow();
            dialogueBox.DialogueHandler.ResetDialogue();
            dialogueBox.DialogueHandler.StartDialogue(battleStartDialogue);

            ultimateBar.OpenWindow();
            partyMembers.OpenWindow();

            overworldEnvironment.SetActive(false);
            battleEnvironment.SetActive(true);
            playerCharacter.overworldController.transform.position = playerBattlePosition;
            playerCharacter.battleController.Initialize(playerHeartPosition);
            playerCharacter.isFighting = true;
            
            m_camera.gameObject.SetActive(true);

            if (AudioManager.Instance != null)
            {
                if (musicMixerGroup != null)
                    AudioManager.Instance.StopPlaying(musicMixerGroup);

                AudioManager.Instance.Play(battleMusic);
            }          

            active = true;
            Debug.Log("Battle Started!");
        }
        
        public void EndBattle()
        {
            // Clean up and return to the overworld
            overworldEnvironment.SetActive(true);
            battleEnvironment.SetActive(false);
            playerCharacter.isFighting = false;

            m_camera.gameObject.SetActive(false);

            if (AudioManager.Instance != null)
            {
                if (musicMixerGroup != null)
                    AudioManager.Instance.StopPlaying(musicMixerGroup);

                AudioManager.Instance.Play(overworldMusic);
            }

            input.SetInputLayer("Overworld");
            dialogueBox.CloseWindow();
            dialogueBox.SetHorizontalScale(dialogueBox.horizontalScale);
            dialogueBox.SetPortraitVisibility(true);

            ultimateBar.CloseWindow();
            partyMembers.CloseWindow();

            active = false;
            Debug.Log("Battle Ended!");
        }

        public void PerformPlayerTurn()
        {
            // Get the player's input for the action to perform
            // Perform the selected action (e.g., Attack, Use Item, etc.)
            // Call EndTurn() in the turn handler to proceed to the next turn
        }

        public void PerformEnemyTurn()
        {
            // Determine the action for the enemy character (e.g., AI logic)
            // Perform the selected action
            // Call EndTurn() in the turn handler to proceed to the next turn
        }
    }
}