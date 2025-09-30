using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace dev.susybaka.TurnBasedGame.Misc
{
    public class RandomObjectToggle : MonoBehaviour
    {
        [SerializeField] private List<GameObject> objects;
        [SerializeField] private bool oneAtTime = true;
        [SerializeField] private Vector2 duration = new Vector2(1f, 3f);

        private bool initialized = false;
        private bool notEnough = false;
        private float timer = 0f;
        private GameObject picked = null;

        public void Initialize()
        {
            if (initialized)
                return;

            initialized = true;
            notEnough = false;
            timer = Random.Range(duration.x, duration.y);

            if (objects != null && objects.Count > 0)
            {
                objects.Shuffle();
                for (int i = 0; i < objects.Count; i++)
                {
                    if (i > 0)
                        objects[i].SetActive(false);
                    else
                    {
                        picked = objects[i];
                        picked.SetActive(true);
                    }
                }
            }
        }

        private void Update()
        {
            if (!initialized || objects == null)
                return;

            if (objects.Count < 2)
            {
                if (!notEnough)
                {
                    notEnough = true;
                    Debug.LogWarning("RandomObjectToggle: Not enough objects to toggle.", this);
                }
                return;
            }

            if (timer > 0f)
            {
                timer -= Time.deltaTime;
            }
            else
            {
                timer = Random.Range(duration.x, duration.y);

                if (oneAtTime)
                {
                    if (picked != null)
                        picked.SetActive(false);

                    GameObject r = objects.GetRandomItem();
                    while (picked == r)
                    {
                        r = objects.GetRandomItem();
                    }

                    picked = r;
                    picked.SetActive(true);
                }
            }
        }
    }
}