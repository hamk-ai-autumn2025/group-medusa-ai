using System.Collections.Generic;
using UnityEngine;
using dev.susybaka.TurnBasedGame.Globals;

namespace dev.susybaka.TurnBasedGame.Characters
{
    public class Controller : MonoBehaviour
    {
        protected Rigidbody2D m_rigidbody;
        protected Collider2D m_collider;
        protected SpriteRenderer m_renderer;
        protected Animator m_animator;
        protected Character m_character;

        [Header("Base")]
        public bool disabled = false;
        [SerializeField] protected bool flipSprite = true;
        public Flag bound = new Flag("bound", FlagAggregateLogic.AnyTrue);
        public Flag uncontrollable = new Flag("uncontrollable", FlagAggregateLogic.AnyTrue);

        protected bool spriteFlipped;

        protected virtual void Awake()
        {
            m_character = transform.GetComponentInParents<Character>();
            if (m_character == null)
                m_character = transform.GetComponentInChildren<Character>(true);
            m_rigidbody = GetComponentInChildren<Rigidbody2D>();
            m_collider = GetComponentInChildren<Collider2D>();
            m_renderer = GetComponentInChildren<SpriteRenderer>();
            m_animator = GetComponentInChildren<Animator>();
        }

        protected virtual void Update()
        {
            if (m_rigidbody == null || m_renderer == null)
                return;

            if (flipSprite)
            {
                if ((m_rigidbody.velocity.y < 0f || m_rigidbody.velocity.y > 0f))
                {
                    m_renderer.flipX = false;
                    spriteFlipped = false;
                    return;
                }

                if (m_rigidbody.velocity.x < -0.01f && !spriteFlipped)
                {
                    m_renderer.flipX = true;
                    spriteFlipped = true;
                }
                else if (m_rigidbody.velocity.x > 0.01f && spriteFlipped)
                {
                    m_renderer.flipX = false;
                    spriteFlipped = false;
                }
            }
        }
    }
}