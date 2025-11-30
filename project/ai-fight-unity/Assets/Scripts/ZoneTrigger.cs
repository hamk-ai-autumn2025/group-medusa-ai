using System.Collections;
using System.Collections.Generic;
using dev.susybaka.TurnBasedGame;
using UnityEngine;

namespace dev.susybaka.TurnBasedGame
{
    public class ZoneTrigger : MonoBehaviour
    {
        public string zoneName = string.Empty;
        public int zoneIndex = -1;
        public Vector2 warpLocation = Vector2.zero;
        public Vector2 warpOffsetDirection = Vector2.zero;

        private GameManager gameManager;

        private void Awake()
        {
            if (gameManager == null)
            {
                gameManager = GameManager.Instance;
            }
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (gameManager == null)
            {
                gameManager = GameManager.Instance;
            }

            if (collision.CompareTag("Player"))
            {
                if (zoneIndex >= 0)
                {
                    gameManager.EnvironmentHandler.SetCurrentOverworldZone(zoneIndex);
                }
                else if (!string.IsNullOrEmpty(zoneName))
                {
                    gameManager.EnvironmentHandler.SetCurrentOverworldZone(zoneName);
                }
                gameManager.BattleHandler.allies.MoveParty(warpLocation, warpOffsetDirection);
            }
        }
    }
}