using System.Collections;
using System.Collections.Generic;
using dev.susybaka.TurnBasedGame;
using UnityEngine;

namespace dev.susybaka.TurnBasedGame
{
    public class TriggerStory : MonoBehaviour
    {
        public int index = -1;

        private GameManager gameManager;

        private void Awake()
        {
            if (gameManager == null)
            {
                gameManager = GameManager.Instance;
            }
        }

        public void Trigger()
        {
            gameManager.SetStoryFlag(index);
        }
    }
}