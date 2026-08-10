using UnityEngine;

public class EnemyController : MonoBehaviour
{
    private static Sprite groundMarkerSprite;

    [SerializeField]
    private Transform target;

    [SerializeField]
    private float moveSpeed = 2f;

    [SerializeField]
    private float stopDistance = 0.1f;

    [Header("Walk Animation")]
    [SerializeField]
    private Texture2D walkSpriteSheet;

    [SerializeField, Min(1)]
    private int walkFrameCount = 4;

    [SerializeField, Min(1f)]
    private float walkFramesPerSecond = 8f;

    [SerializeField, Min(1f)]
    private float walkPixelsPerUnit = 700f;

    private Rigidbody2D rigidBody;
    private SpriteRenderer spriteRenderer;
    private Vector3 restingScale;
    private float frozenUntil;
    private float movementPhase;
    private bool isMoving;
    private bool usesAuthoredAnimation;
    private Sprite[] walkFrames;

    public ulong AssignedTargetClientId { get; private set; }

    private void Awake()
    {
        rigidBody = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        restingScale = transform.localScale;
        movementPhase = (Mathf.Abs(GetInstanceID()) % 997) * 0.017f;
        usesAuthoredAnimation = GetComponent<Animator>() != null;
        CreateWalkFrames();
        CreateReadabilityMarker();
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

    public void Retarget(Transform assignedTarget, ulong assignedClientId)
    {
        target = assignedTarget;
        AssignedTargetClientId = assignedClientId;
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

        UpdateWalkFrame();

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

    private void OnDestroy()
    {
        if (walkFrames == null)
        {
            return;
        }

        foreach (Sprite frame in walkFrames)
        {
            if (frame != null)
            {
                Destroy(frame);
            }
        }
    }

    private void CreateWalkFrames()
    {
        if (usesAuthoredAnimation ||
            spriteRenderer == null ||
            walkSpriteSheet == null ||
            walkFrameCount < 1)
        {
            return;
        }

        int frameWidth = walkSpriteSheet.width / walkFrameCount;

        if (frameWidth < 1)
        {
            return;
        }

        walkFrames = new Sprite[walkFrameCount];

        for (int index = 0; index < walkFrameCount; index++)
        {
            Rect frameRect = new Rect(
                index * frameWidth,
                0,
                frameWidth,
                walkSpriteSheet.height
            );

            walkFrames[index] = Sprite.Create(
                walkSpriteSheet,
                frameRect,
                new Vector2(0.5f, 0.5f),
                walkPixelsPerUnit,
                0,
                SpriteMeshType.FullRect
            );
            walkFrames[index].name = $"{walkSpriteSheet.name}_{index}";
        }

        spriteRenderer.sprite = walkFrames[0];
    }

    private void CreateReadabilityMarker()
    {
        GameObject marker = new GameObject("EnemyReadabilityMarker");
        marker.transform.SetParent(transform, false);
        marker.transform.localPosition = new Vector3(0f, -0.42f, 0f);
        marker.transform.localScale = new Vector3(1.05f, 0.72f, 1f);

        SpriteRenderer markerRenderer = marker.AddComponent<SpriteRenderer>();
        markerRenderer.sprite = GetOrCreateGroundMarkerSprite();
        markerRenderer.color = new Color(1f, 0.24f, 0.07f, 0.78f);
        markerRenderer.sortingLayerID = spriteRenderer != null
            ? spriteRenderer.sortingLayerID
            : 0;
        markerRenderer.sortingOrder = spriteRenderer != null
            ? spriteRenderer.sortingOrder - 1
            : -1;
    }

    private static Sprite GetOrCreateGroundMarkerSprite()
    {
        if (groundMarkerSprite != null)
        {
            return groundMarkerSprite;
        }

        const int width = 48;
        const int height = 24;
        Texture2D texture = new Texture2D(
            width,
            height,
            TextureFormat.RGBA32,
            false
        )
        {
            name = "EnemyGroundMarker",
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };
        Color32[] pixels = new Color32[width * height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float normalizedX = (x - (width - 1) * 0.5f) / (width * 0.5f);
                float normalizedY = (y - (height - 1) * 0.5f) / (height * 0.5f);
                float distance = normalizedX * normalizedX +
                                 normalizedY * normalizedY;
                bool ring = distance <= 0.92f && distance >= 0.58f;
                pixels[y * width + x] = ring
                    ? new Color32(255, 255, 255, 255)
                    : new Color32(255, 255, 255, 0);
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply(false, true);
        groundMarkerSprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, width, height),
            new Vector2(0.5f, 0.5f),
            36f,
            0,
            SpriteMeshType.FullRect
        );
        groundMarkerSprite.name = "EnemyGroundMarkerSprite";
        return groundMarkerSprite;
    }

    private void UpdateWalkFrame()
    {
        if (walkFrames == null || walkFrames.Length == 0)
        {
            return;
        }

        if (!isMoving || GameplayPauseState.IsLevelUpActive)
        {
            spriteRenderer.sprite = walkFrames[0];
            return;
        }

        int frameIndex = Mathf.FloorToInt(
            Time.time * walkFramesPerSecond + movementPhase
        ) % walkFrames.Length;

        spriteRenderer.sprite = walkFrames[frameIndex];
    }
}
