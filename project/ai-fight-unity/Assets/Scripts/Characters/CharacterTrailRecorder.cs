using System.Collections.Generic;
using UnityEngine;

namespace dev.susybaka.TurnBasedGame.Characters
{
    /// <summary>
    /// Records the sequence of tile centers the player steps through.
    /// Followers consume this with a lag to replicate the path exactly.
    /// </summary>
    public class CharacterTrailRecorder : MonoBehaviour
    {
        [Header("Grid")]
        public Vector2 gridOffset = new Vector2(0.5f, 0.5f); // tile centers
        public int maxPoints = 4096; // large enough so we don't trim under normal play

        private readonly List<Vector2> _points = new List<Vector2>();
        private Vector2Int _lastTile = new Vector2Int(int.MinValue, int.MinValue);

        private Controller _controller;
        public Controller Controller => _controller;

        public int PointCount => _points.Count;
        public Vector2 GetPoint(int index)
        {
            if (_points == null)
                return Vector2.zero;
            if (_points.Count < 1 || index < 0 || index >= _points.Count)
                return Vector2.zero;
            return _points[index];
        }

        void Start()
        {
            // seed with starting tile
            var tile = WorldToTile(transform.position);
            _points.Add(TileToWorld(tile));
            _lastTile = tile;
            _controller = GetComponentInChildren<Controller>(true);
        }

        void Update()
        {
            var tile = WorldToTile(transform.position);
            if (tile != _lastTile)
            {
                _lastTile = tile;
                var p = TileToWorld(tile);
                _points.Add(p);
                if (_points.Count > maxPoints)
                    _points.RemoveAt(0); // simple trim (followers with very large lags might need a larger buffer)
            }
        }

        public Vector2Int WorldToTile(Vector3 world) => new Vector2Int(Mathf.FloorToInt(world.x), Mathf.FloorToInt(world.y));
        public Vector2 TileToWorld(Vector2Int tile) => new Vector2(tile.x + gridOffset.x, tile.y + gridOffset.y);
    }
}