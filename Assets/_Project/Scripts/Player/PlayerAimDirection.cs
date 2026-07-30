using UnityEngine;

/// <summary>
/// 플레이어의 "마지막 이동 방향"을 기억한다 (D-018: 기본공격 발사 방향).
/// PlayerController를 수정하지 않고 Rigidbody2D 속도를 읽어서 추적한다.
/// 멈춰 있으면 마지막으로 움직였던 방향을 유지한다. 시작값은 오른쪽.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerAimDirection : MonoBehaviour
{
    private Rigidbody2D rb;
    private Vector2 aimDirection = Vector2.right;

    public Vector2 AimDirection => aimDirection;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        Vector2 velocity = rb.linearVelocity;

        if (velocity.sqrMagnitude > 0.01f)
        {
            aimDirection = velocity.normalized;
        }
    }
}
