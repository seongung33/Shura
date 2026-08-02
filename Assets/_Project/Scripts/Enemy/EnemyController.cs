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

    private void FixedUpdate()
    {
        if (target == null)
        {
            GameObject player= GameObject.FindGameObjectWithTag("Player");

            if (player != null)
            {
                target = player.transform;
            }
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
