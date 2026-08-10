using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class NetworkPlayerMovement : NetworkBehaviour
{
    [SerializeField]
    private float moveSpeed = 5f;

    private Rigidbody2D rigidBody;
    private SpriteRenderer spriteRenderer;

    private Vector2 moveInput;

    public float SpeedMultiplier { get; set; } = 1f;
    public float GrowthSpeedMultiplier { get; set; } = 1f;
    public bool SkillMovementOverride { get; set; }

    public void ConfigureBaseSpeed(float value)
    {
        if (value > 0f && !float.IsNaN(value) && !float.IsInfinity(value))
        {
            moveSpeed = value;
        }
    }

    private void Awake()
    {
        rigidBody = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public override void OnNetworkSpawn()
    {
        // 접속자들이 완전히 겹쳐서 생성되지 않도록
        // OwnerClientId에 따라 시작 위치를 조금 다르게 설정한다.
        if (IsOwner)
        {
            transform.position = new Vector3(
                (float)OwnerClientId * 2f,
                0f,
                0f
            );

            if (spriteRenderer != null)
            {
                spriteRenderer.color = Color.green;
            }

            return;
        }

        // 상대방 플레이어는 내 컴퓨터의 물리 엔진으로 움직이지 않는다.
        // NetworkTransform이 전달한 위치만 표시한다.
        rigidBody.simulated = false;

        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.red;
        }
    }

    public void OnMove(InputValue value)
    {
        if (!IsOwner)
        {
            return;
        }

        Vector2 input = value.Get<Vector2>();
        moveInput = input.sqrMagnitude > 1f
            ? input.normalized
            : input;
    }

    private void FixedUpdate()
    {
        if (!IsOwner)
        {
            return;
        }

        if (SkillMovementOverride)
        {
            rigidBody.linearVelocity = Vector2.zero;
            return;
        }

        if (GameplayPauseState.IsLevelUpActive)
        {
            rigidBody.linearVelocity = Vector2.zero;
            return;
        }

        rigidBody.linearVelocity =
            moveInput * moveSpeed * SpeedMultiplier * GrowthSpeedMultiplier;
    }

    public override void OnNetworkDespawn()
    {
        if (rigidBody != null &&
            rigidBody.simulated)
        {
            rigidBody.linearVelocity =
                Vector2.zero;
        }

        SpeedMultiplier = 1f;
        GrowthSpeedMultiplier = 1f;
        SkillMovementOverride = false;
    }
}
