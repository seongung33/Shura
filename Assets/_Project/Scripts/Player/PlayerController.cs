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

        /// <summary>
        /// 스킬(예: 적토마)이 일시적으로 이동 속도를 조절할 때 사용하는 배율. 기본 1.
        /// </summary>
        public float SpeedMultiplier { get; set; } = 1f;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
        }

        public void ConfigureBaseSpeed(float value)
        {
            if (value > 0f && !float.IsNaN(value) && !float.IsInfinity(value))
            {
                moveSpeed = value;
            }
        }

        public void OnMove(InputValue value)
        {
            moveInput = value.Get<Vector2>();
        }

        private void FixedUpdate()
        {
            // Clamp to unit length so diagonal input isn't faster than a single axis.
            Vector2 direction = moveInput.sqrMagnitude > 1f ? moveInput.normalized : moveInput;
            rb.linearVelocity = direction * moveSpeed * SpeedMultiplier;
        }
    }
}
