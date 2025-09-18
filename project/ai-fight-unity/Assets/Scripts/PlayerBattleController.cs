using dev.susybaka.Shared;
using dev.susybaka.TurnBasedGame.Characters;
using UnityEngine;

namespace dev.susybaka.TurnBasedGame.Player
{
    public class PlayerBattleController : Controller
    {
        InputHandler Input;

        [Header("Player Battle")]
        public bool active = false;
        public float speed;
        public float sensitivity;
        public float jumpForce = 5f;
        [SerializeField] private float jumpCutMultiplier = 0.5f;   // 0.5 = cut upward speed in half on release
        [SerializeField] private float fallGravityMultiplier = 1.4f; // optional: snappier fall; only affects descent

        public bool useGravity = false;
        public float gravityScale = 1f;

        [SerializeField] private Vector2 horizontalBounds = new Vector2(-2,2);
        [SerializeField] private Vector2 verticalBounds = new Vector2(-2, 2);

        [SerializeField] private Transform groundCheck;          // put a small empty at feet
        [SerializeField] private float groundCheckRadius = 0.15f;
        [SerializeField] private LayerMask groundMask;

        private Vector2 movement;
        private bool jumpHeld;
        private bool jumpPressedBuffered;
        private bool jumpReleasedBuffered;
        private bool isGrounded;

        private void Start()
        {
            Input = InputHandler.instance;

            Initialize(new Vector2 (0f, 0f));
        }

        protected override void Update()
        {
            base.Update();

            if (!active)
            {
                m_renderer.enabled = false;
            }
            else
            {
                m_renderer.enabled = true;
            }

            if (Input == null || disabled || !active)
                return;

            if (sensitivity == 0)
                sensitivity = 1;

            // Read movement input
            movement = Input.MovementInput * sensitivity;

            // Sample held input and edge-detect here
            bool held = Input.JumpHoldInput || movement.y > 0;

            // Rising edge -> buffer a press
            if (held && !jumpHeld)
                jumpPressedBuffered = true;

            // Falling edge -> buffer a release
            if (!held && jumpHeld)
                jumpReleasedBuffered = true;

            // Persist held state
            jumpHeld = held;
        }

        private void FixedUpdate()
        {
            if (Input == null || disabled || !active)
                return;

            // Copy & clear buffers so each event is handled exactly once
            bool jumpPressed = jumpPressedBuffered;
            bool jumpReleased = jumpReleasedBuffered;
            jumpPressedBuffered = false;
            jumpReleasedBuffered = false;

            // Gravity (base)
            float g = useGravity ? gravityScale : 0f;
            if (useGravity && m_rigidbody.velocity.y < 0f)
                g *= fallGravityMultiplier; // only speeds up falling; doesn't change max jump height

            m_rigidbody.gravityScale = g;

            // --- Ground check ---
            if (useGravity && groundCheck != null)
            {
                isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundMask);
            }
            else
            {
                // Fallback if no groundCheck provided: consider "grounded" when near lower bound and not moving up
                isGrounded = useGravity && Mathf.Abs(m_rigidbody.velocity.y) < 0.01f &&
                             m_rigidbody.position.y <= verticalBounds.x + 0.02f;
            }

            if (useGravity)
            {
                // Horizontal
                float vx = movement.x * speed;
                m_rigidbody.velocity = new Vector2(vx, m_rigidbody.velocity.y);

                // Jump on press (unchanged max height)
                if (jumpPressed && isGrounded)
                {
                    var v = m_rigidbody.velocity;
                    if (v.y < 0f)
                        v.y = 0f;
                    m_rigidbody.velocity = new Vector2(v.x, 0f);
                    m_rigidbody.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
                }

                // ---- Variable height: cut on release while still going up ----
                if (jumpReleased && m_rigidbody.velocity.y > 0f)
                {
                    m_rigidbody.velocity = new Vector2(
                        m_rigidbody.velocity.x,
                        m_rigidbody.velocity.y * jumpCutMultiplier
                    );
                }
            }
            else
            {
                // No gravity mode
                m_rigidbody.velocity = movement * speed;
            }

            // Bounds clamping from previous version (unchanged)
            Vector2 pos = m_rigidbody.position;
            float clampedX = Mathf.Clamp(pos.x, horizontalBounds.x, horizontalBounds.y);
            float clampedY = Mathf.Clamp(pos.y, verticalBounds.x, verticalBounds.y);

            if (!Mathf.Approximately(clampedX, pos.x))
                m_rigidbody.velocity = new Vector2(0f, m_rigidbody.velocity.y);
            if (!Mathf.Approximately(clampedY, pos.y))
                m_rigidbody.velocity = new Vector2(m_rigidbody.velocity.x, Mathf.Min(0f, m_rigidbody.velocity.y));

            m_rigidbody.position = new Vector2(clampedX, clampedY);
        }

        public void Initialize(Vector2 startPos)
        {
            transform.position = startPos;
            movement = startPos;
            m_collider.isTrigger = true;
            active = false;
            Utilities.FunctionTimer.Create(this, () => { m_collider.isTrigger = false; active = true; }, 2f, "PlayerBattleController_CollisionDelay", false, true);
        }

        public void Deinitialize()
        {
            active = false;
            m_collider.isTrigger = true;
        }
    }

}