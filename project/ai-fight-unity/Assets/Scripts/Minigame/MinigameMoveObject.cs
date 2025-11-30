using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace dev.susybaka.TurnBasedGame.Minigame
{
    public class MinigameMoveObject : MonoBehaviour
    {
        [SerializeField] private Vector3 position;
        [SerializeField] private float duration = 2.5f;
        private bool initialized = false;
        private bool moving = false;
        private bool moved = false;

        public UnityEvent finished;
        RectTransform rect;

        public void Initialize()
        {
            if (initialized)
                return;

            initialized = true;
            rect = GetComponent<RectTransform>();
        }

        private void Update()
        {
            if (!initialized)
                return;

            if (!moved && !moving)
            {
                StartMove();
            }
            else if (moved && !moving)
            {
                transform.position = position;
            }
        }

        private void StartMove()
        {
            moving = true;
            if (rect != null)
                rect.LeanMoveY(position.y, duration).setOnComplete(() => { moved = true; moving = false; finished.Invoke(); });
            else
                transform.LeanMove(position, duration).setOnComplete(() => { moved = true; moving = false; finished.Invoke(); });
        }
    }
}