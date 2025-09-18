using System.Collections.Generic;
using UnityEngine;
using static dev.susybaka.TurnBasedGame.Core.GlobalData;
using static dev.susybaka.TurnBasedGame.Core.GlobalData.Flag;

namespace dev.susybaka.TurnBasedGame.Characters
{
    public class Controller : MonoBehaviour
    {
        protected Rigidbody2D m_rigidbody;
        protected Collider2D m_collider;
        protected SpriteRenderer m_renderer;
        protected Animator m_animator;

        [Header("Base")]
        public bool disabled = false;
        [SerializeField] protected bool flipSprite = true;
        public Flag bound = new Flag("bound", AggregateLogic.AnyTrue);
        public Flag uncontrollable = new Flag("uncontrollable", AggregateLogic.AnyTrue);

        protected bool spriteFlipped;

        protected virtual void Awake()
        {
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