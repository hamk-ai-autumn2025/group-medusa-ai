using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using dev.susybaka.TurnBasedGame.Battle.Data;
using dev.susybaka.TurnBasedGame.Characters;
using dev.susybaka.TurnBasedGame.Characters.Data;
using dev.susybaka.TurnBasedGame.Input;
using dev.susybaka.TurnBasedGame.Player;
using dev.susybaka.TurnBasedGame.UI;
using dev.susybaka.Shared.Audio;
using dev.susybaka.Shared.Attributes;
#if UNITY_EDITOR
using NaughtyAttributes;
#endif

namespace dev.susybaka.TurnBasedGame.Battle
{
    public class BattleHandler : MonoBehaviour
    {
        private GameManager gameManager;
        private PlayerCharacter playerCharacter;
        public PlayerCharacter PlayerCharacter => playerCharacter;
        private AbilitySystem abilitySystem;
        public AbilitySystem AbilitySystem => abilitySystem;
        private TurnSystem turnSystem;
        public TurnSystem TurnSystem => turnSystem;

        public bool active = false;

        [Header("Enironment Settings")]
        public Transform environmentParent;
        public Transform[] battlePartyMemberLocations;
        public GameObject overworldEnvironment;
        [SoundName] public string overworldMusic = "bgm_wld_dev";
        
        [Header("Battle Settings")]
        public FightData data;
        public AudioMixerGroup musicMixerGroup;
        public Transform m_camera;
        public OverworldWindow overworldWindow;
        public BattleWindow battleWindow;
        public Party enemies;
        public Party allies;

        // Private fields
        private bool initialized = false;
        private InputHandler input;
        private GameObject currentBattleEnvironment;
        private Dictionary<Character, Character> currentTargets = new Dictionary<Character, Character>();

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

        public void Initialize(GameManager manager)
        {
            if (initialized) 
                return;

            initialized = true;
            gameManager = manager;
            abilitySystem = GetComponentInChildren<AbilitySystem>();
            turnSystem = GetComponentInChildren<TurnSystem>();
            input = GameManager.Instance.Input;
            playerCharacter = GameManager.Instance.Player;
            m_camera.gameObject.SetActive(false);
            overworldWindow.Initialize(manager);
            battleWindow.Initialize(manager);
            
            if (abilitySystem == null)
                abilitySystem = gameObject.AddComponent<AbilitySystem>();
            if (turnSystem == null)
                turnSystem = gameObject.AddComponent<TurnSystem>();

            turnSystem.Initialize(manager, battleWindow.PartyMembers);
            allies.Initialize(manager);
            enemies.Initialize(manager);

            EndBattle();
        }

        public void StartBattle(CharacterData opponent)
        {
            gameManager.currentGameWindow = battleWindow;

            // Initialize the turn handler and start the battle
            //turnHandler = new TurnSystem(this, );

            input.SetInputLayer("Battle");

            //overworldWindow.DialogueBox.CloseWindow();

            battleWindow.OpenPartyWindow(allies);

            battleWindow.ActionPointBar.OpenWindow();
            gameManager.HudNavigationHandler?.OpenRoot(battleWindow.PartyMembers);
            //battleWindow.PartyMembers.OpenWindow(); No need, opened in OpenRoot()

            if (data.startDialogue != null)
            {
                //gameManager.HudNavigationHandler.PushWindow(battleWindow.DialogueBox);
                gameManager.DialogueHandler.StartDialogue(data.startDialogue);
            }
            else
            {
                battleWindow.DialogueBox.CloseWindow();
            }

            overworldEnvironment.SetActive(false);

            if (currentBattleEnvironment != null && currentBattleEnvironment.name != data.environmentPrefab.name)
                Destroy(currentBattleEnvironment);
            if (currentBattleEnvironment == null)
                currentBattleEnvironment = Instantiate(data.environmentPrefab, environmentParent);
            else
                currentBattleEnvironment.SetActive(true);

            for (int i = 0; i < allies.members.Count; i++)
            {
                if (i < battlePartyMemberLocations.Length)
                {
                    if (allies.members[i] == playerCharacter)
                        continue; // Player is moved separately below for now

                    allies.members[i].transform.GetComponentInChildren<NPCOverworldController>()?.Stop();
                    allies.members[i].isFighting = true;
                    allies.members[i].transform.position = battlePartyMemberLocations[i].position;
                }
                else
                {
                    // If there are more members than locations, just leave them in their current position for now
                }
            }

            // For now move player separately because their controller is not attached to the character transform
            playerCharacter.overworldController.transform.position = data.playerPosition;
            playerCharacter.battleController.Initialize(data.playerHeartPosition);
            playerCharacter.isFighting = true;
            
            m_camera.gameObject.SetActive(true);

            if (AudioManager.Instance != null)
            {
                if (musicMixerGroup != null)
                    AudioManager.Instance.StopPlaying(musicMixerGroup);

                AudioManager.Instance.Play(data.music);
            }

            overworldWindow.CloseWindow();
            battleWindow.OpenWindow();

            active = true;
            turnSystem.StartBattle(data);
            Debug.Log("Battle Started!");
        }
        
        public void EndBattle()
        {
            gameManager.currentGameWindow = overworldWindow;

            // Clean up and return to the overworld
            overworldEnvironment.SetActive(true);

            if (currentBattleEnvironment != null)
                currentBattleEnvironment.SetActive(false);

            for (int i = 0; i < allies.members.Count; i++)
            {
                allies.members[i].isFighting = false;
                allies.members[i].transform.GetComponentInChildren<NPCOverworldController>()?.FollowCharacterTrail(playerCharacter.GetComponentInChildren<CharacterTrailRecorder>(), i + (1 * i));
            }

            //playerCharacter.isFighting = false;
            playerCharacter.battleController.disabled = true;
            playerCharacter.battleController.Deinitialize();

            m_camera.gameObject.SetActive(false);

            if (AudioManager.Instance != null)
            {
                if (musicMixerGroup != null)
                    AudioManager.Instance.StopPlaying(musicMixerGroup);

                AudioManager.Instance.Play(overworldMusic);
            }

            input.SetInputLayer("Overworld");
            battleWindow.DialogueBox.CloseWindow();

            gameManager.HudNavigationHandler?.CloseRoot();

            battleWindow.ActionPointBar.CloseWindow();
            //battleWindow.PartyMembers.CloseWindow(); No need, closed in CloseRoot()
            battleWindow.ActionWindow.CloseWindow();
            battleWindow.TargetWindow.CloseWindow();

            battleWindow.CloseWindow();
            overworldWindow.OpenWindow();

            active = false;
            Debug.Log("Battle Ended!");
        }

        public void UpdateTurnState(int turn)
        {
            for (int i = 0; i < allies.members.Count; i++)
            {
                allies.members[i].UpdateTurnState(turn);
            }
            for (int i = 0; i < enemies.members.Count; i++)
            {
                enemies.members[i].UpdateTurnState(turn);
            }
        }

        public void PerformPlayerTurn()
        {
            playerCharacter.battleController.disabled = true;

            // Get the player's input for the action to perform
            // Perform the selected action (e.g., Attack, Use Item, etc.)
            // Call EndTurn() in the turn handler to proceed to the next turn
        }

        public void PerformEnemyTurn()
        {
            // Determine the action for the enemy character (e.g., AI logic)
            // Perform the selected action
            // Call EndTurn() in the turn handler to proceed to the next turn

            playerCharacter.battleController.disabled = false;
        }

        public void SetCharacterTarget(Character source, Character target)
        {
            if (target != null)
                currentTargets[source] = target;
            else if (currentTargets.ContainsKey(source))
                currentTargets.Remove(source);
        }

        public void ShowDescription(string text)
        {
            battleWindow.DescriptionWindow.SetText(text);
            if (!battleWindow.DescriptionWindow.isOpen)
                battleWindow.DescriptionWindow.OpenWindow();
        }

        public void HideDescription()
        {
            battleWindow.DescriptionWindow.ClearText();
            if (battleWindow.DescriptionWindow.isOpen)
                battleWindow.DescriptionWindow.CloseWindow();
        }
    }
}