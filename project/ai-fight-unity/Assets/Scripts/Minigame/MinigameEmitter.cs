using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace dev.susybaka.TurnBasedGame.Minigame
{
    public class MinigameEmitter : MonoBehaviour
    {
        [SerializeField] private MinigamePrefab[] prefabs;
        [SerializeField] private Vector2 positionOffset;
        [SerializeField] private Vector2 interval = Vector2.one;
        [SerializeField] private bool noDuplicates = true;

        private bool initialized = false;
        private Action onHit;
        private float timer = 0f;
        private int previousIndex = -1;

        public void Initialize(Action onHit)
        {
            if (initialized)
                return;

            initialized = true;
            this.onHit = onHit;
            timer = Random.Range(interval.x, interval.y);
        }

        private void Update()
        {
            if (!initialized || prefabs.Length < 1)
                return;

            timer -= Time.deltaTime;
            if (timer <= 0f)
            {
                timer = 0f;
                timer = Random.Range(interval.x, interval.y);
                int index = Random.Range(0, prefabs.Length);

                if (noDuplicates && prefabs.Length > 1)
                {
                    while (index == previousIndex)
                    {
                        index = Random.Range(0, prefabs.Length);
                    }
                }
                Instantiate(prefabs[index], transform.position + (Vector3)positionOffset, transform.rotation, transform.parent).Initialize(onHit);
                previousIndex = index;
            }
        }

        private void OnDestroy()
        {
            onHit = null;
        }
    }
}