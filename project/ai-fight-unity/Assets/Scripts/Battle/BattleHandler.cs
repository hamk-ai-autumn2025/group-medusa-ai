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
using dev.susybaka.TurnBasedGame.Dialogue;

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
        public Transform[] battleEnemyLocations;
        public GameObject overworldEnvironment;
        [SoundName] public string overworldMusic = "bgm_wld_dev";
        
        [Header("Battle Settings")]
        public FightData data;
        public AudioMixerGroup musicMixerGroup;
        public Transform m_camera;
        public OverworldWindow overworldWindow;
        public BattleWindow battleWindow;
        public CreditsWindow creditsWindow;
        public Party enemies;
        public Party allies;

        // Private fields
        private bool initialized = false;
        private InputHandler input;
        private GameObject currentBattleEnvironment;
        private Dictionary<Character, Character> currentTargets = new Dictionary<Character, Character>();
        private Dictionary<Character, Vector3> originalPositions = new Dictionary<Character, Vector3>();
        public bool? win = null;

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
            creditsWindow?.Initialize(manager);
            creditsWindow?.CloseWindow();

            if (abilitySystem == null)
                abilitySystem = gameObject.AddComponent<AbilitySystem>();
            if (turnSystem == null)
                turnSystem = gameObject.AddComponent<TurnSystem>();

            turnSystem.Initialize(manager, battleWindow.PartyMembers);
            allies.Initialize(manager);
            enemies.Initialize(manager);

            originalPositions = new Dictionary<Character, Vector3>();

            EndBattle();
        }

        public void AddPartyMember(Character character)
        {
            if (character == null)
                return;

            allies.AddMember(character);

            // THIS IS EXTREMELY SHIT AND ABSOLUTE GARBAGE CODE, BUT I REALLY JUST WANTED TO GET THIS SHIT DONE, SORRY
            // ---
            // Sort party so that if "Greg" is present and there are more than 2 members, Greg is last, leader is first, and the other extra is second.
            if (allies.members != null && allies.members.Count >= 3)
            {
                // Find Greg and leader
                int gregIndex = -1;
                int leaderIndex = -1;
                for (int i = 0; i < allies.members.Count; i++)
                {
                    if (allies.members[i] is FriendCharacter fc && fc.data.characterName.Contains("Greg"))
                        gregIndex = i;
                    if (allies.members[i] == allies.leader)
                        leaderIndex = i;
                }

                if (gregIndex != -1 && leaderIndex != -1)
                {
                    // Build new order: leader, extra, ..., Greg
                    var sorted = new List<Character>(allies.members.Count);
                    // Add leader first
                    sorted.Add(allies.members[leaderIndex]);
                    // Add the extra (not leader, not Greg)
                    for (int i = 0; i < allies.members.Count; i++)
                    {
                        if (i != leaderIndex && i != gregIndex)
                        {
                            sorted.Add(allies.members[i]);
                            break; // Only one extra for index 1
                        }
                    }
                    // Add any remaining (not leader, not Greg, not already added as extra)
                    for (int i = 0; i < allies.members.Count; i++)
                    {
                        if (i != leaderIndex && i != gregIndex && !sorted.Contains(allies.members[i]))
                            sorted.Add(allies.members[i]);
                    }
                    // Add Greg last
                    sorted.Add(allies.members[gregIndex]);
                    // Replace the original list
                    allies.members = sorted;
                }
            }
            // ---

            for (int i = 0; i < allies.members.Count; i++)
            {
                allies.members[i].Initialize(allies);
                allies.members[i].isFighting = false;
                //allies.members[i].transform.GetComponentInChildren<NPCOverworldController>()?.FollowCharacterTrail(playerCharacter.GetComponentInChildren<CharacterTrailRecorder>(), i + (1 * i));
                if (allies.members[i] is FriendCharacter friend)
                {
                    //Debug.Log("Following party leader: " + allies.leader.data.characterName);
                    friend.FollowPartyLeader();
                }
            }
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
            battleWindow.DescriptionWindow.OpenWindow();
            battleWindow.PopupWindow.OpenWindow();
            battleWindow.SpeechWindow.CloseWindow();
            battleWindow.AttackMinigameWindow.CloseWindow();

            if (data.startDialogue != null)
            {
                //gameManager.HudNavigationHandler.PushWindow(battleWindow.DialogueBox);
                gameManager.DialogueHandler.StartDialogue(data.startDialogue, new DialogueContext(null, null, null, null));
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

                    originalPositions.Add(allies.members[i], allies.members[i].transform.position);

                    //allies.members[i].transform.GetComponentInChildren<NPCOverworldController>()?.Stop();
                    if (allies.members[i] is FriendCharacter friend)
                        friend.StopFollowing();
                    allies.members[i].isFighting = true;
                    allies.members[i].transform.position = battlePartyMemberLocations[i].position;
                }
                else
                {
                    // If there are more members than locations, just leave them in their current position for now
                }
            }
            for (int i = 0; i < enemies.members.Count; i++)
            {
                if (i < battleEnemyLocations.Length)
                {
                    originalPositions.Add(enemies.members[i], enemies.members[i].transform.position);

                    enemies.members[i].transform.position = battleEnemyLocations[i].position;
                    enemies.members[i].isFighting = true;
                }
                else
                {
                    // If there are more members than locations, just leave them in their current position for now
                }
            }

            // For now move player separately because their controller is not attached to the character transform
            playerCharacter.overworldController.transform.position = data.playerPosition;
            //playerCharacter.battleController.Initialize(data.playerHeartPosition);
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
                //allies.members[i].transform.GetComponentInChildren<NPCOverworldController>()?.FollowCharacterTrail(playerCharacter.GetComponentInChildren<CharacterTrailRecorder>(), i + (1 * i));
                if (originalPositions.ContainsKey(allies.members[i]))
                    allies.members[i].transform.position = originalPositions[allies.members[i]];
                if (allies.members[i] is FriendCharacter friend)
                {
                    friend.FollowPartyLeader();
                }
            }
            for (int i = 0; i < enemies.members.Count; i++)
            {
                enemies.members[i].isFighting = false;
                if (originalPositions.ContainsKey(enemies.members[i]))
                    enemies.members[i].transform.position = originalPositions[enemies.members[i]];
            }

            playerCharacter.isFighting = false;
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
            battleWindow.DescriptionWindow.CloseWindow();
            battleWindow.PopupWindow.CloseWindow();
            battleWindow.SpeechWindow.CloseWindow();
            battleWindow.AttackMinigameWindow.CloseWindow();

            battleWindow.CloseWindow();
            overworldWindow.OpenWindow();
            originalPositions.Clear();

            active = false;
            Debug.Log("Battle Ended!");

            if (creditsWindow != null && win != null)
            {
                creditsWindow.OpenWindow();
                creditsWindow.TriggerEnd((bool)win);
            }
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

        public void SetCharacterTarget(Character source, Character target)
        {
            if (target != null)
                currentTargets[source] = target;
            else if (currentTargets.ContainsKey(source))
                currentTargets.Remove(source);
        }
    }
}