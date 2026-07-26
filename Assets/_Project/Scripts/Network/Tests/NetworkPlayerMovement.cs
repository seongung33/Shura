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

    private void Update()
    {
        // 내가 소유하지 않은 상대방 캐릭터에는
        // 내 키보드 입력을 적용하지 않는다.
        if (!IsOwner)
        {
            return;
        }

        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
        {
            moveInput = Vector2.zero;
            return;
        }

        float horizontal = 0f;
        float vertical = 0f;

        if (keyboard.aKey.isPressed ||
            keyboard.leftArrowKey.isPressed)
        {
            horizontal -= 1f;
        }

        if (keyboard.dKey.isPressed ||
            keyboard.rightArrowKey.isPressed)
        {
            horizontal += 1f;
        }

        if (keyboard.sKey.isPressed ||
            keyboard.downArrowKey.isPressed)
        {
            vertical -= 1f;
        }

        if (keyboard.wKey.isPressed ||
            keyboard.upArrowKey.isPressed)
        {
            vertical += 1f;
        }

        moveInput = new Vector2(
            horizontal,
            vertical
        ).normalized;
    }

    private void FixedUpdate()
    {
        if (!IsOwner)
        {
            return;
        }

        rigidBody.linearVelocity =
            moveInput * moveSpeed;
    }

    public override void OnNetworkDespawn()
    {
        if (rigidBody != null &&
            rigidBody.simulated)
        {
            rigidBody.linearVelocity =
                Vector2.zero;
        }
    }
}