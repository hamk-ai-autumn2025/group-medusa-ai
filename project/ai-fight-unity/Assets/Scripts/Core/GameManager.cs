using UnityEngine;
using dev.susybaka.TurnBasedGame.Battle;
using dev.susybaka.TurnBasedGame.Dialogue;
using dev.susybaka.TurnBasedGame.Input;
using dev.susybaka.TurnBasedGame.Minigame;
using dev.susybaka.TurnBasedGame.Player;
using dev.susybaka.TurnBasedGame.UI;
using dev.susybaka.Shared.UI;
using dev.susybaka.TurnBasedGame.Characters;
using dev.susybaka.TurnBasedGame.AI;

namespace dev.susybaka.TurnBasedGame
{
    public enum GameStateWindowType { none, dialogue, party, action, target, ultimate, talk }

    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance;

        [Header("Global Settings")]
        public float dynamicTimeScale = 1f;
        public float dynamicGravityScale = 1f;
        public float deltaTime { get; private set; }
        public float gravity { get; private set; }

        [Header("Game Settings")]
        public GameStateWindow currentGameWindow;

        [Header("Components")]
        [SerializeField] private PlayerCharacter player;
        [SerializeField] private InputHandler inputHandler;
        [SerializeField] private DialogueHandler dialogueHandler;
        [SerializeField] private BattleHandler battleHandler;
        [SerializeField] private HudNavigationHandler hudNavigationHandler;
        [SerializeField] private MinigameHandler minigameHandler;
        [SerializeField] private AIHandler aiHandler;

        public PlayerCharacter Player => player;
        public InputHandler Input => inputHandler;
        public DialogueHandler DialogueHandler => dialogueHandler;
        public BattleHandler BattleHandler => battleHandler;
        public HudNavigationHandler HudNavigationHandler => hudNavigationHandler;
        public MinigameHandler MinigameHandler => minigameHandler;
        public AIHandler AIHandler => aiHandler;

        public static bool Available => Instance != null;
        public static bool PlayerAvailable => Available && Instance.player != null;
        public static bool InputHandlerAvailable => Available && Instance.inputHandler != null;
        public static bool DialogueHandlerAvailable => Available && Instance.dialogueHandler != null;
        public static bool BattleHandlerAvailable => Available && Instance.battleHandler != null;
        public static bool HudNavigationHandlerAvailable => Available && Instance.hudNavigationHandler != null;
        public static bool MinigameHandlerAvailable => Available && Instance.minigameHandler != null;
        public static bool AIHandlerAvailable => Available && Instance.aiHandler != null;

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);
        }

        private void Start()
        {
            if (battleHandler != null)
                currentGameWindow = battleHandler.overworldWindow;

            if (inputHandler != null)
                inputHandler.Initialize(this);
            if (dialogueHandler != null)
                dialogueHandler.Initialize(this);
            if (battleHandler != null)
                battleHandler.Initialize(this);
            if (hudNavigationHandler != null)
                hudNavigationHandler.Initialize(this);
            if (minigameHandler != null)
                minigameHandler.Initialize(this);

            // Temporary: Have allies follow player in overworld
            // This should ideally be handled by an overworld manager or similar
            // Currently this is so god damn ugly but it works for now
            if (battleHandler != null && battleHandler.allies.members != null && battleHandler.allies.members.Count > 0)
            {
                for (int i = 0; i < battleHandler.allies.members.Count; i++)
                {
                    var member = battleHandler.allies.members[i];
                    if (member != null && member is FriendCharacter)
                        member.GetComponent<NPCOverworldController>().FollowCharacterTrail(player.GetComponentInChildren<CharacterTrailRecorder>(), i + (1 * i)); // God save us from this awful hardcoded spacing
                }
            }
        }

        private void Update()
        {
            deltaTime = Time.deltaTime * dynamicTimeScale;
            gravity = Physics2D.gravity.y * dynamicGravityScale;

            if (Utilities.RateLimiter(10))
            {
                if (currentGameWindow != null && dialogueHandler != null)
                {
                    dialogueHandler.dialogueBox = currentGameWindow.DialogueBox;
                }
            }
        }

        // Opens a specific window in the HUD navigation handler
        // Kind of a bad design but it works for now so who cares lol
        public void OpenWindow(GameStateWindowType type)
        {
            if (!HudNavigationHandlerAvailable)
                return;

            HudWindow window;
            BattleWindow bw = currentGameWindow as BattleWindow;
            OverworldWindow ow = currentGameWindow as OverworldWindow;

            switch (type)
            {
                case GameStateWindowType.dialogue:
                    window = currentGameWindow?.DialogueBox;
                    break;
                case GameStateWindowType.party:
                    window = bw?.PartyMembers;
                    break;
                case GameStateWindowType.action:
                    window = bw?.ActionWindow;
                    break;
                case GameStateWindowType.target:
                    window = bw?.TargetWindow;
                    break;
                case GameStateWindowType.ultimate:
                    window = bw?.ActionPointBar;
                    break;
                case GameStateWindowType.talk:
                    window = bw?.TalkWindow;
                    break;
                default:
                    window = null;
                    break;
            }

            if (window != null)
                hudNavigationHandler.PushWindow(window);
        }
    }
}