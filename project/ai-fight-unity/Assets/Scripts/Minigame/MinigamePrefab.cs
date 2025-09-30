using System;
using dev.susybaka.TurnBasedGame.Player;
using UnityEngine;
using UnityEngine.Events;
using Random = UnityEngine.Random;

namespace dev.susybaka.TurnBasedGame.Minigame
{
    public class MinigamePrefab : MonoBehaviour
    {
        private Rigidbody2D _rigidbody;

        [SerializeField] private Vector4 movement;
        [SerializeField] private float lifetime = 5f;
        [SerializeField] private float invincibilityTime = 1f;
        [SerializeField] private bool destroyedOnContact = false;
        [SerializeField] private bool localSpace = true;

        public UnityEvent<Action> onInitialize;

        private Action onHit;
        private float iTimer = 1f;
        private bool omitDamage = false;
        private bool initialized = false;
        private Vector2 _movement = Vector2.zero;

        public void Initialize(Action onHit)
        {
            if (initialized)
                return;

            initialized = true;
            _rigidbody = GetComponent<Rigidbody2D>();

            iTimer = invincibilityTime;
            this.onHit = onHit;
            omitDamage = false;

            _movement = new Vector2(Random.Range(movement.x, movement.z), Random.Range(movement.y, movement.w));

            if (lifetime > 0f)
            {
                Destroy(gameObject, lifetime);
            }
            onInitialize.Invoke(onHit);
        }

        private void FixedUpdate()
        {
            if (_rigidbody != null)
            {
                if (localSpace)
                {
                    // Move in local space according to the object's rotation
                    Vector2 localDirection = transform.TransformDirection(_movement);
                    _rigidbody.MovePosition(_rigidbody.position + localDirection * Time.fixedDeltaTime);
                }
                else
                {
                    // Move in global space towards the specified direction
                    _rigidbody.MovePosition(_rigidbody.position + _movement * Time.fixedDeltaTime);
                }
            }
        }

        private void Update()
        {
            if (iTimer < invincibilityTime)
            {
                omitDamage = true;
                iTimer += Time.deltaTime;
            }
            else
            {
                omitDamage = false;
            }
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (omitDamage)
                return;

            if (collision.CompareTag("Player") && collision.TryGetComponent(out PlayerBattleController _))
            {                
                if (onHit != null)
                {
                    iTimer = 0f;
                    onHit.Invoke();
                }

                if (destroyedOnContact)
                {
                    Destroy(gameObject);
                }
            }
        }
    }
}