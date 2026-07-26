using UnityEngine;
using UnityEngine.InputSystem;

namespace Shura.Player
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 5f;

        private Rigidbody2D rb;
        private Vector2 moveInput;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
        }

        public void OnMove(InputValue value)
        {
            moveInput = value.Get<Vector2>();
        }

        private void FixedUpdate()
        {
            // Clamp to unit length so diagonal input isn't faster than a single axis.
            Vector2 direction = moveInput.sqrMagnitude > 1f ? moveInput.normalized : moveInput;
            rb.linearVelocity = direction * moveSpeed;
        }
    }
}
