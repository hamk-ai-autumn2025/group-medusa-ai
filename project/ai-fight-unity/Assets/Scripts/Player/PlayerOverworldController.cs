using dev.susybaka.TurnBasedGame.Characters;
using dev.susybaka.TurnBasedGame.Input;
using UnityEngine;

namespace dev.susybaka.TurnBasedGame.Player
{
    public class PlayerOverworldController : Controller
    {
        InputHandler Input;

        [Header("Player Overworld")]
        public float speed;
        public float sprintMultiplier = 2f;
        public float sensitivity;

        [Header("Player Overworld - References")]
        [SerializeField] PlayerCharacter playerCharacter;
        [SerializeField] private Transform interactionTrigger;
        public bool rotateInteractionTrigger = true;

        private Vector2 movement;
        private Vector2 storedMovement;

        private void Start()
        {
            Input = GameManager.Instance.Input;
            if (playerCharacter == null)
                playerCharacter = transform.GetComponentInParents<PlayerCharacter>();
        }

        protected override void Update()
        {
            base.Update();

            if (Input == null || disabled)
            {
                UpdateAnimation();
                return;
            }

            if (sensitivity == 0)
                sensitivity = 1;

            movement = Input.MovementInput * sensitivity;

            if (movement != Vector2.zero)
                storedMovement = movement; // keep last look dir for animation

            UpdateAnimation();           // uses storedMovement / rb.velocity
            UpdateInteractionTrigger();  // fine to keep here
        }

        private void FixedUpdate()
        {
            if (Input == null || disabled)
            { 
                m_rigidbody.velocity = Vector2.zero; 
                return; 
            }

            // Top-down: kill gravity
            m_rigidbody.gravityScale = 0f;

            float spd = speed;
            if (Input.SprintHoldInput)
                spd *= sprintMultiplier;

            // Normalize to avoid diagonal speed boost
            Vector2 dir = movement.sqrMagnitude > 1e-6f ? movement.normalized : Vector2.zero;

            // NO deltaTime on velocity
            m_rigidbody.velocity = dir * spd;
        }

        private void UpdateAnimation()
        {
            if (playerCharacter.isFighting)
            {
                m_animator.Play("idle_player_battle");
                m_renderer.flipX = false;
                spriteFlipped = false;
                return;
            }

            if (movement == Vector2.zero)
            {
                // Pick the animation based on the dominant axis to keep things simple.
                if (Mathf.Abs(storedMovement.x) > Mathf.Abs(storedMovement.y))
                {
                    m_animator.Play("idle_side_player_overworld");
                }
                else if (storedMovement.y > 0)
                {
                    m_animator.Play("idle_up_player_overworld");
                }
                else
                {
                    m_animator.Play("idle_down_player_overworld");
                }
                return;
            }

            // Pick the animation based on the dominant axis to keep things simple.
            if (Mathf.Abs(movement.x) > Mathf.Abs(movement.y))
            {
                m_animator.Play("walk_side_player_overworld");
            }
            else if (movement.y > 0)
            {
                m_animator.Play("walk_up_player_overworld");
            }
            else
            {
                m_animator.Play("walk_down_player_overworld");
            }
        }

        private void UpdateInteractionTrigger()
        {
            if (Mathf.Abs(storedMovement.x) > Mathf.Abs(storedMovement.y))
            {
                if (rotateInteractionTrigger)
                {
                    if (spriteFlipped)
                    {
                        interactionTrigger.localEulerAngles = new Vector3(0, 0, 270);
                        interactionTrigger.localPosition = new Vector3(-0.435f, 0, 0);
                    }
                    else
                    {
                        interactionTrigger.localEulerAngles = new Vector3(0, 0, 90);
                        interactionTrigger.localPosition = new Vector3(0.435f, 0, 0);
                    }
                }
            }
            else if (storedMovement.y > 0)
            {
                if (rotateInteractionTrigger)
                {
                    interactionTrigger.localEulerAngles = new Vector3(0, 0, 180);
                    interactionTrigger.localPosition = new Vector3(0, 0.5f, 0);
                }
            }
            else
            {
                if (rotateInteractionTrigger)
                {
                    interactionTrigger.localEulerAngles = new Vector3(0, 0, 0);
                    interactionTrigger.localPosition = Vector3.zero;
                }
            }
        }
    }
}