using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace dev.susybaka.TurnBasedGame.Minigame.Data
{
    [CreateAssetMenu(fileName = "New Minigame", menuName = "Turn Based Game/Battles/Minigame")]
    public class MinigameData : ScriptableObject
    {
        public float startDelay = 2f;
        public float finishDelay = 2f;
        public MinigameEvent[] events;

#if UNITY_EDITOR
        private void OnValidate()
        {
            for (int i = 0; i < events.Length; i++)
            {
                MinigameEvent e = events[i];

                if (e.duration < 0f)
                    e.duration = 0f;
                if (e.prefabs != null && e.prefabs.Length > 0)
                {
                    for (int p = 0; p < e.prefabs.Length; p++)
                    {
                        MinigamePrefabSpawn ps = events[i].prefabs[p];
                        ps.name = ps.prefab != null ? ps.prefab.name : string.Empty;
                        events[i].prefabs[p] = ps;
                    }
                }

                events[i] = e;
            }
        }
#endif
    }

    [System.Serializable]
    public struct MinigameEvent
    {
        public string name;
        public MinigamePrefabSpawn[] prefabs;
        public float duration;
    }

    [System.Serializable]
    public struct MinigamePrefabSpawn
    {
        public string name;
        public MinigamePrefab prefab;
        public Vector2 spawnLocation;
    }
}