using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace dev.susybaka.TurnBasedGame.Minigame
{
    public class MinigameCollider : MonoBehaviour
    {
        private MinigamePrefab prefab;

        private void Awake()
        {
            prefab = transform.GetComponentInParents<MinigamePrefab>();
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            prefab.OnTriggerEnter2D(collision);
        }
    }
}