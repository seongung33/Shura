using UnityEngine;
using UnityEngine.Rendering;

public class EnemyController : MonoBehaviour
{
    [SerializeField]
    private Transform target;

    [SerializeField]
    private float moveSpeed = 2f;

    [SerializeField]
    private float stopDistance = 0.1f;

    private Rigidbody2D rigidBody;
    private float frozenUntil;

    public ulong AssignedTargetClientId { get; private set; }

    private void Awake()
    {
        rigidBody = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        if (target != null)
        {
            return;
        }

        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player == null)
        {
            Debug.Log("Player 태그를 가진 오브젝트를 찾지 못했습니다.");
            return;
        }

        target = player.transform;
    }

    public void ConfigureRuntime(
        Transform assignedTarget,
        ulong assignedClientId,
        float moveSpeedMultiplier
    )
    {
        target = assignedTarget;
        AssignedTargetClientId = assignedClientId;
        moveSpeed *= Mathf.Max(0.01f, moveSpeedMultiplier);
    }

    public void FreezeFor(float duration)
    {
        if (duration <= 0f ||
            float.IsNaN(duration) ||
            float.IsInfinity(duration))
        {
            return;
        }

        frozenUntil = Mathf.Max(frozenUntil, Time.time + duration);

        if (rigidBody != null)
        {
            rigidBody.linearVelocity = Vector2.zero;
        }
    }

    private void FixedUpdate()
    {
        if (GameplayPauseState.IsLevelUpActive ||
            Time.time < frozenUntil)
        {
            rigidBody.linearVelocity = Vector2.zero;
            return;
        }

        if (target == null)
        {
            rigidBody.linearVelocity = Vector2.zero;
            return;
        }

        Vector2 currentPosition = rigidBody.position;
        Vector2 targetPosition = target.position;

        Vector2 direction = targetPosition - currentPosition;

        if (direction.magnitude <= stopDistance)
        {
            rigidBody.linearVelocity = Vector2.zero;
            return;
        }

        Vector2 nextPosition =
            currentPosition
            + direction.normalized * moveSpeed * Time.fixedDeltaTime;

        rigidBody.MovePosition(nextPosition);
    }
}
