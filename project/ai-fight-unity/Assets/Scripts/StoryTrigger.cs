using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using dev.susybaka.TurnBasedGame.Characters;

namespace dev.susybaka.TurnBasedGame
{
    public class StoryTrigger : MonoBehaviour
    {
        GameManager gameManager;
        public bool require1 = false;
        public bool require2 = false;
        public bool require3 = false;
        public bool require4 = false;

        public UnityEvent<Character> onDeny;
        public UnityEvent<Character> onAccept;

        private void Awake()
        {
            gameManager = GameManager.Instance;
        }

        public void CheckProgressionRequirements(Character character)
        {
            if (gameManager == null)
            {
                gameManager = GameManager.Instance;
            }

            bool deny = false;

            if (require1 && !gameManager.story1)
                deny = true;
            if (require2 && !gameManager.story2)
                deny = true;
            if (require3 && !gameManager.story3)
                deny = true;
            if (require4 && !gameManager.story4)
                deny = true;

            if (deny)
            {
                onDeny.Invoke(character);
            }
            else
            {
                onAccept.Invoke(character);
            }
        }
    }
}