using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

namespace dev.susybaka.TurnBasedGame
{
    public class EnvironmentHandler : MonoBehaviour
    {
        public static EnvironmentHandler Instance;

        public List<MapZone> mapZones = new List<MapZone>();
        public MapZone currentZone;
        public CinemachineConfiner2D confiner;
        public Camera _camera;

        private void OnValidate()
        {
            for (int i = 0; i < mapZones.Count; i++)
            {
                MapZone z = mapZones[i];

                if (string.IsNullOrEmpty(z.name) && z.tilemap != null)
                {
                    z.name = z.tilemap.name;
                }

                mapZones[i] = z;
            }
        }

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(Instance.gameObject);
            }
            Instance = this;

            // Begin in first zone by default
            SetCurrentOverworldZone(0);
        }

        public void SetCurrentOverworldZone(string zoneName)
        {
            for (int i = 0; i < mapZones.Count; i++)
            {
                if (mapZones[i].name == zoneName)
                {
                    currentZone = mapZones[i];
                    RefreshCurrentOverworldZone();
                }
                else
                {
                    mapZones[i].tilemap.SetActive(false);
                    mapZones[i].bounds.gameObject.SetActive(false);
                    mapZones[i].triggers.SetActive(false);
                }
            }
        }

        public void SetCurrentOverworldZone(int zoneIndex)
        {
            currentZone = mapZones[zoneIndex];
            RefreshCurrentOverworldZone();

            for (int i = 0; i < mapZones.Count; i++)
            {
                if (i == zoneIndex)
                    continue;

                mapZones[i].tilemap.SetActive(false);
                mapZones[i].bounds.gameObject.SetActive(false);
                mapZones[i].triggers.SetActive(false);
            }
        }

        private void RefreshCurrentOverworldZone()
        {
            confiner.m_BoundingShape2D = currentZone.bounds;
            confiner.InvalidateCache();
            currentZone.tilemap.SetActive(true);
            currentZone.bounds.gameObject.SetActive(true);
            currentZone.triggers.SetActive(true);
        }
    }

    [System.Serializable]
    public struct MapZone
    {
        public string name;
        public GameObject tilemap;
        public PolygonCollider2D bounds;
        public GameObject triggers;
    }
}