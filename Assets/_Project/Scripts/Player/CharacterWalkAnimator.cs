using UnityEngine;

[RequireComponent(typeof(Animator))]
public class CharacterWalkAnimator : MonoBehaviour
{
    [SerializeField]
    private Animator animator;

    [SerializeField]
    private Transform movementRoot;

    [SerializeField]
    private float moveThreshold = 0.0005f;

    [SerializeField]
    private float stopDelay = 0.1f;

    private Vector3 previousPosition;
    private Vector3 originalScale;
    private float lastMovedTime = float.NegativeInfinity;

    private static readonly int IsMovingHash =
        Animator.StringToHash("IsMoving");

    private void Awake()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        if (movementRoot == null)
        {
            Rigidbody2D parentBody = GetComponentInParent<Rigidbody2D>();

            if (parentBody != null)
            {
                movementRoot = parentBody.transform;
            }
            else if (transform.parent != null)
            {
                movementRoot = transform.parent;
            }
        }

        originalScale = transform.localScale;
    }

    private void OnEnable()
    {
        if (movementRoot != null)
        {
            previousPosition = movementRoot.position;
        }

        if (animator != null)
        {
            animator.SetBool(IsMovingHash, false);
        }
    }

    private void LateUpdate()
    {
        if (movementRoot == null || animator == null)
        {
            return;
        }

        Vector3 currentPosition = movementRoot.position;
        Vector3 movement = currentPosition - previousPosition;

        bool movedThisFrame =
            movement.sqrMagnitude >
            moveThreshold * moveThreshold;

        if (movedThisFrame)
        {
            lastMovedTime = Time.time;
        }

        if (Mathf.Abs(movement.x) > moveThreshold)
        {
            Vector3 scale = originalScale;

            // 원본 이미지가 오른쪽을 보는 기준
            scale.x = Mathf.Abs(originalScale.x) *
                      (movement.x < 0f ? -1f : 1f);

            transform.localScale = scale;
        }

        bool isMoving =
            Time.time - lastMovedTime <= stopDelay;

        animator.SetBool(IsMovingHash, isMoving);

        previousPosition = currentPosition;
    }
}