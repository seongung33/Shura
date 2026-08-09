using UnityEngine;

public class EnemyController : MonoBehaviour
{
    [SerializeField]
    private Transform target;

    [SerializeField]
    private float moveSpeed = 2f;

    [SerializeField]
    private float stopDistance = 0.1f;

    private Rigidbody2D rigidBody;
    private SpriteRenderer spriteRenderer;
    private Vector3 restingScale;
    private float frozenUntil;
    private float movementPhase;
    private bool isMoving;
    private bool usesAuthoredAnimation;

    public ulong AssignedTargetClientId { get; private set; }

    private void Awake()
    {
        rigidBody = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        restingScale = transform.localScale;
        movementPhase = (Mathf.Abs(GetInstanceID()) % 997) * 0.017f;
        usesAuthoredAnimation = GetComponent<Animator>() != null;
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

        isMoving = false;
    }

    private void Update()
    {
        if (usesAuthoredAnimation)
        {
            return;
        }

        float settleSpeed = 10f * Time.deltaTime;

        if (!isMoving || GameplayPauseState.IsLevelUpActive)
        {
            transform.localScale = Vector3.Lerp(
                transform.localScale,
                restingScale,
                settleSpeed
            );
            return;
        }

        float pace = Mathf.InverseLerp(1.2f, 3.2f, moveSpeed);
        float strideFrequency = Mathf.Lerp(4f, 8f, pace);
        float stride = Mathf.Sin(
            Time.time * strideFrequency + movementPhase
        );
        float compression = Mathf.Abs(stride);
        float horizontalPulse = Mathf.Lerp(0.025f, 0.055f, pace);
        float verticalPulse = Mathf.Lerp(0.045f, 0.08f, pace);

        transform.localScale = new Vector3(
            restingScale.x * (1f + compression * horizontalPulse),
            restingScale.y * (1f - compression * verticalPulse),
            restingScale.z
        );
    }

    private void FixedUpdate()
    {
        if (GameplayPauseState.IsLevelUpActive ||
            Time.time < frozenUntil)
        {
            rigidBody.linearVelocity = Vector2.zero;
            isMoving = false;
            return;
        }

        if (target == null)
        {
            rigidBody.linearVelocity = Vector2.zero;
            isMoving = false;
            return;
        }

        Vector2 currentPosition = rigidBody.position;
        Vector2 targetPosition = target.position;

        Vector2 direction = targetPosition - currentPosition;

        if (direction.magnitude <= stopDistance)
        {
            rigidBody.linearVelocity = Vector2.zero;
            isMoving = false;
            return;
        }

        Vector2 travelDirection = direction.normalized;
        isMoving = true;

        if (spriteRenderer != null && Mathf.Abs(travelDirection.x) > 0.02f)
        {
            spriteRenderer.flipX = travelDirection.x < 0f;
        }

        Vector2 nextPosition =
            currentPosition
            + travelDirection * moveSpeed * Time.fixedDeltaTime;

        rigidBody.MovePosition(nextPosition);
    }

    private void OnDisable()
    {
        transform.localScale = restingScale;
    }
}
