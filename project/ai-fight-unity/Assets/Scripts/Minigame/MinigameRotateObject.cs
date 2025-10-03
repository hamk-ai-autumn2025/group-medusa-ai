using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace dev.susybaka.TurnBasedGame.Minigame
{
    public class MinigameRotateObject : MonoBehaviour
    {
        [SerializeField] private Vector3 rotation;
        private bool initialized = false;

        public void Initialize()
        {
            if (initialized)
                return;

            initialized = true;
        }

        private void Update()
        {
            if (!initialized)
                return;

            transform.Rotate(rotation * Time.deltaTime);
        }
    }
}