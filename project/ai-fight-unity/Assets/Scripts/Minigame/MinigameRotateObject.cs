using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace dev.susybaka.TurnBasedGame.Minigame
{
    public class MinigameRotateObject : MonoBehaviour
    {
        [SerializeField] private Vector3 rotation;
        [SerializeField] private Vector3 rotationMax;
        private bool initialized = false;

        public void Initialize()
        {
            if (initialized)
                return;

            initialized = true;

            if (rotationMax != Vector3.zero)
            {
                rotation = new Vector3(
                    Random.Range(rotation.x, rotationMax.x),
                    Random.Range(rotation.y, rotationMax.y),
                    Random.Range(rotation.z, rotationMax.z)
                );
            }
        }

        private void Update()
        {
            if (!initialized)
                return;

            transform.Rotate(rotation * Time.deltaTime);
        }
    }
}