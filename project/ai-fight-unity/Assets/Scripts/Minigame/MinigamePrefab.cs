using System;
using System.Collections;
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
        [SerializeField] private float delay = 0f;
        [SerializeField] private float lifetime = 5f;
        [SerializeField] private float invincibilityTime = 1f;
        [SerializeField] private bool destroyedOnContact = false;
        [SerializeField] private bool localSpace = true;

        public UnityEvent<Action> onInitialize;

        private Action onHit;
        private float iTimer = 1f;
        private bool omitDamage = false;
        private bool initialized = false;
        private bool active = false;
        private bool delayFinished = false;
        private bool dd = false;
        private Vector2 _movement = Vector2.zero;
        private Vector3 posi = Vector3.zero;

        public void Initialize(Action onHit)
        {
            if (initialized)
                return;

            posi = transform.position;
            initialized = true;
            active = false;
            delayFinished = false;
            _rigidbody = GetComponent<Rigidbody2D>();

            iTimer = invincibilityTime;
            this.onHit = onHit;
            omitDamage = false;

            _movement = new Vector2(Random.Range(movement.x, movement.z), Random.Range(movement.y, movement.w));

            StartCoroutine(Activate());
        }

        // Need to wait a frame to avoid the object from having the wrong position for one frame when spawned
        private IEnumerator Activate()
        {
            transform.position = posi;
            if (delay > 0f)
            {
                yield return new WaitForSeconds(delay);
            }
            if (lifetime > 0f)
            {
                Destroy(gameObject, lifetime);
            }
            onInitialize.Invoke(onHit);
            yield return null;
            transform.position = posi;
            active = true;
        }

        private void FixedUpdate()
        {
            if (!initialized || !active)
                return;

            if (!delayFinished)
                return;

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
            if (!initialized || !active)
                return;

            if (!delayFinished)
            {
                if (!dd)
                {
                    dd = true;
                    if (delay > 0f)
                        Utilities.FunctionTimer.Create(this, () => { delayFinished = true; dd = false; }, delay);
                    else
                        delayFinished = true;
                }
                return;
            }

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
            if (!initialized || !active)
                return;

            if (omitDamage)
                return;

            if (!delayFinished)
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