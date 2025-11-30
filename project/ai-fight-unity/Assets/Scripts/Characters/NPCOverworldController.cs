using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Tilemaps;

namespace dev.susybaka.TurnBasedGame.Characters
{
    public class NPCOverworldController : Controller
    {
        private enum Mode { idle, movePath, chaseTransform, followTrail }

        [Header("Movement")]
        public bool sprint = false;
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
        [SerializeField] private float maxWaypointStep = 1.6f; // > sqrt(2) tile; good for orthogonal/diag

        [Header("Catchup Gating")]
        public int catchupBacklogTiles = 3;     // how many unconsumed trail points before we boost
        public float catchupDistance = 0.6f;    // or if physically this far from next waypoint, boost

        private readonly Queue<Vector2> currentPath = new Queue<Vector2>();
        private Mode mode = Mode.idle;
        private Vector2 movement;
        private Vector2 storedMovement;
        private bool wasFighting = false;
        private Vector2 animMove;
        private Vector2? lastEnqueued;
        private int seenTrailVersion = -1;

        [Header("Animations")]
        [SerializeField] private bool hasMovementAnimations = true;

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
            if (sprint)
                spd *= sprintMultiplier;

            // Small catch-up when a path is long or target is moving
            if (mode == Mode.followTrail)
            {
                int backlog = (trail != null) ? (trail.PointCount - lagTiles - 1) - consumedTrailIndex : 0;
                bool behind = backlog >= catchupBacklogTiles
                              || (currentPath.Count > 0 && Vector2.Distance(m_rigidbody.position, currentPath.Peek()) > catchupDistance);

                if (behind)
                    spd *= catchupMultiplier;
            }
            else if (mode == Mode.chaseTransform)
            {
                if (target != null && Vector2.Distance(m_rigidbody.position, (Vector2)target.position) > catchupDistance)
                    spd *= catchupMultiplier;
            }

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

            // If this hop is unrealistically large for a tile path, it's a stale pre-warp waypoint.
            // Nuke the queue and wait for fresh trail points.
            if (mode == Mode.followTrail && delta.magnitude > maxWaypointStep)
            {
                currentPath.Clear();
                return Vector2.zero;
            }

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

            if (seenTrailVersion != trail.Version)
            {
                seenTrailVersion = trail.Version;
                currentPath.Clear();
                consumedTrailIndex = -1;
                // optional if you added it:
                // lastEnqueued = null;
                return Vector2.zero; // wait one frame, then we’ll enqueue only post-warp points
            }

            int newestUsable = trail.PointCount - lagTiles - 1;
            if (newestUsable < 0)
                return Vector2.zero;

            for (int i = consumedTrailIndex + 1; i <= newestUsable; i++)
            {
                var p = trail.GetPoint(i);
                if (lastEnqueued.HasValue && (p - lastEnqueued.Value).sqrMagnitude < 1e-6f)
                    continue; // skip duplicates
                currentPath.Enqueue(p);
                lastEnqueued = p;
                consumedTrailIndex = i;
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

            if (!hasMovementAnimations)
            {
                return;
            }

            float targetX = m_rigidbody.velocity.x;
            float targetY = m_rigidbody.velocity.y;
            animMove = new Vector2(Mathf.MoveTowards(animMove.x, targetX, Time.deltaTime * 16f), Mathf.MoveTowards(animMove.y, targetY, Time.deltaTime * 16f)); // damp

            if (sprint)
            {
                m_animator.SetFloat("speed", 2f);
            }
            else
            {
                m_animator.SetFloat("speed", 1f);
            }

            if (m_character.isFighting && !wasFighting)
            {
                if (m_character.isAlive)
                {
                    m_animator.Play("idle_npc_battle");
                }
                m_renderer.flipX = false;
                spriteFlipped = false;
                wasFighting = true;
            } 
            else if (!m_character.isFighting)
            {
                m_animator.SetBool("inBattle", false);
                wasFighting = false;
            }

            if (m_character.isFighting)
            {
                m_animator.SetBool("inBattle", true);
                return;
            }

            if (animMove == Vector2.zero)//animMove < 0.05f && animMove > -0.05f) //movement == Vector2.zero)
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

            if (Mathf.Abs(animMove.x) > Mathf.Abs(animMove.y))
            {
                m_animator.Play("walk_side_npc_overworld");
            }
            else if (animMove.y > 0)
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
            if (trail != null)
                trail.ResetAtPosition(m_rigidbody.position);
            currentPath.Clear();
            consumedTrailIndex = -1;
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

        public void OnTeleported()
        {
            currentPath.Clear();
            consumedTrailIndex = -1;
            lastEnqueued = null;

            // Start so the very next UpdateFollowTrail enqueues the first usable point
            if (trail != null)
            {
                consumedTrailIndex = Mathf.Clamp(trail.PointCount - lagTiles - 2, -1, trail.PointCount - 1);

                // Optional: snap follower to its rightful lag position to avoid racing
                int idx = Mathf.Clamp(trail.PointCount - lagTiles - 1, 0, trail.PointCount - 1);
                m_rigidbody.position = trail.GetPoint(idx);
                m_rigidbody.velocity = Vector2.zero;
                target = null;
                mode = Mode.followTrail;
            }
            else
            {
                mode = Mode.idle;
            }
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