using System.Collections.Generic;
using UnityEngine;

namespace dev.susybaka.TurnBasedGame.Characters
{
    public class NPCOverworldController : Controller
    {
        private enum Mode { idle, movePath, chaseTransform, followTrail }

        [Header("Movement")]
        public float speed = 2f;
        public float sprintMultiplier = 2f;
        public float arriveEpsilon = 0.05f;
        public float catchupMultiplier = 1.25f;

        [Header("Targets / Grid")]
        public Transform target;
        public Vector2 gridOffset = new Vector2(0.5f, 0.5f);

        [Header("Follow Character Trail")]
        public CharacterTrailRecorder trail;
        [Min(0)] public int lagTiles = 3;
        private int consumedTrailIndex = -1;

        private readonly Queue<Vector2> currentPath = new Queue<Vector2>();
        private Mode mode = Mode.idle;
        private Vector2 movement;
        private Vector2 storedMovement;

        protected override void Update()
        {
            base.Update();

            if (disabled)
            {
                movement = Vector2.zero;
                UpdateAnimation();
                return;
            }

            //this.LogV((trail == null ? "null" : nameof(trail.PointCount), trail?.PointCount));

            // Choose desired direction based on mode
            Vector2 dir = Vector2.zero;

            // Freeze rigidbody when idle to avoid player from pushing NPCs around
            switch (mode)
            {
                case Mode.movePath:
                    dir = UpdateMovePath();
                    m_rigidbody.constraints = RigidbodyConstraints2D.FreezeRotation;
                    break;
                case Mode.chaseTransform:
                    dir = UpdateChaseTransform();
                    m_rigidbody.constraints = RigidbodyConstraints2D.FreezeRotation;
                    break;
                case Mode.followTrail:
                    dir = UpdateFollowTrail();
                    m_rigidbody.constraints = RigidbodyConstraints2D.FreezeRotation;
                    break;
                default:
                    dir = Vector2.zero;
                    m_rigidbody.constraints = RigidbodyConstraints2D.FreezeAll;
                    break;
            }

            // Normalize to avoid diagonal boost
            movement = dir.sqrMagnitude > 1e-6f ? dir.normalized : Vector2.zero;

            if (movement != Vector2.zero)
                storedMovement = movement; // keep last look dir for animation

            UpdateAnimation();
        }

        private void FixedUpdate()
        {
            if (disabled)
            {
                m_rigidbody.velocity = Vector2.zero;
                return;
            }

            m_rigidbody.gravityScale = 0f;

            float spd = speed;
            if (sprinting)
                spd *= sprintMultiplier;

            // Small catch-up when a path is long or target is moving
            if (mode == Mode.followTrail || mode == Mode.chaseTransform)
                spd *= catchupMultiplier;
            
            m_rigidbody.velocity = movement * spd;
        }

        private Vector2 UpdateMovePath()
        {
            if (currentPath.Count == 0)
            {
                // Only go Idle when explicitly walking a fixed path.
                if (mode == Mode.movePath)
                    mode = Mode.idle;

                return Vector2.zero;
            }

            var next = currentPath.Peek();
            var pos = (Vector2)m_rigidbody.position;
            var delta = next - pos;

            if (delta.magnitude <= arriveEpsilon)
            {
                // Snap to the exact point to avoid creep, then pop and continue
                m_rigidbody.position = next;
                currentPath.Dequeue();
                if (currentPath.Count == 0)
                {
                    // Only go Idle when explicitly walking a fixed path.
                    if (mode == Mode.movePath)
                        mode = Mode.idle;
                    return Vector2.zero;
                }
                next = currentPath.Peek();
                delta = next - (Vector2)m_rigidbody.position;
            }

            return delta;
        }

        private Vector2 UpdateChaseTransform()
        {
            if (target == null)
            { 
                mode = Mode.idle; 
                return Vector2.zero; 
            }

            var pos = (Vector2)m_rigidbody.position;
            var delta = (Vector2)target.position - pos;

            if (delta.magnitude <= arriveEpsilon)
                return Vector2.zero;

            return delta;
        }

        private Vector2 UpdateFollowTrail()
        {
            if (trail == null)
            { 
                mode = Mode.idle; 
                return Vector2.zero; 
            }

            if (trail.Controller != null && trail.Controller.sprinting)
                sprinting = true;
            else
                sprinting = false;

            int newestUsable = trail.PointCount - lagTiles - 1;
            if (newestUsable < 0)
                return Vector2.zero;

            for (int i = consumedTrailIndex + 1; i <= newestUsable; i++)
            {
                var p = trail.GetPoint(i);
                currentPath.Enqueue(p);
                consumedTrailIndex = i;
                // Debug.Log($"[{name}] Enqueued trail[{i}] {p}");
            }

            return UpdateMovePath();
        }

        private void UpdateAnimation()
        {
            if (m_character == null || m_animator == null || m_renderer == null)
            {
                //Debug.LogWarning("NPCOverworldController missing components.");
                return;
            }

            if (sprinting)
            {
                m_animator.SetFloat("speed", sprintMultiplier);
            }
            else
            {
                m_animator.SetFloat("speed", 1f);
            }

            if (m_character.isFighting)
            {
                m_animator.Play("idle_npc_battle");
                m_renderer.flipX = false;
                spriteFlipped = false;
                return;
            }

            if (movement == Vector2.zero)
            {
                if (Mathf.Abs(storedMovement.x) > Mathf.Abs(storedMovement.y))
                {
                    m_animator.Play("idle_side_npc_overworld");
                }
                else if (storedMovement.y > 0)
                {
                    m_animator.Play("idle_up_npc_overworld");
                }
                else
                {
                    m_animator.Play("idle_down_npc_overworld");
                }
                return;
            }

            if (Mathf.Abs(movement.x) > Mathf.Abs(movement.y))
            {
                m_animator.Play("walk_side_npc_overworld");
            }
            else if (movement.y > 0)
            {
                m_animator.Play("walk_up_npc_overworld");
            }
            else
            {
                m_animator.Play("walk_down_npc_overworld");
            }
        }

        public Vector2 TileToWorld(Vector2Int tile) => new Vector2(tile.x + gridOffset.x, tile.y + gridOffset.y);

        public void Stop()
        {
            currentPath.Clear();
            target = null;
            trail = null;
            mode = Mode.idle;
        }

        public void ClearPath()
        {
            currentPath.Clear();
            if (mode == Mode.movePath)
                mode = Mode.idle;
        }

        public void SetDestinationWorld(Vector2 world)
        {
            currentPath.Clear();
            currentPath.Enqueue(world);
            mode = Mode.movePath;
            target = null;
            trail = null;
        }

        public void SetDestinationTile(Vector2Int tile)
        {
            SetDestinationWorld(TileToWorld(tile));
        }

        public void SetPathWorld(IEnumerable<Vector2> worldPoints)
        {
            currentPath.Clear();
            foreach (Vector2 p in worldPoints)
            {
                currentPath.Enqueue(p);
            }
            mode = Mode.movePath;
            target = null;
            trail = null;
        }

        public void SetPathTiles(IEnumerable<Vector2Int> tiles)
        {
            currentPath.Clear();
            foreach (Vector2Int t in tiles)
            {
                currentPath.Enqueue(TileToWorld(t));
            }
            mode = Mode.movePath;
            target = null;
            trail = null;
        }

        public void FollowTransform(Transform t)
        {
            currentPath.Clear();
            target = t;
            mode = Mode.chaseTransform;
            trail = null;
        }

        public void FollowCharacterTrail(CharacterTrailRecorder recorder, int tilesLag)
        {
            trail = recorder;
            lagTiles = Mathf.Max(0, tilesLag);
            currentPath.Clear();
            target = null;

            // Seed so that the very next UpdateFollowTrail() enqueues the current usable point.
            // Start at "one before" the first usable index. Allow -1 to mean "before the first point".
            consumedTrailIndex = Mathf.Clamp(recorder.PointCount - lagTiles - 2, -1, recorder.PointCount - 1);

            mode = Mode.followTrail;
        }
    }
}