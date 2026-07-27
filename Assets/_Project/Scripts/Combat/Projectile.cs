using UnityEngine;

public class Projectile : MonoBehaviour
{
    [Header("Target Search")]

    [SerializeField]
    private LayerMask enemyLayer;

    [SerializeField]
    private float retargetRange = 200f;

    [Header("Lifetime")]

    [SerializeField]
    private float lifeTime = 5f;

    private Transform target;
    private float moveSpeed;
    private float damage;

    private bool isInitialized;
    private void Awake()
    {
        Destroy(gameObject, lifeTime);
    }

    public void Initialize(
        Transform newTarget,
        float newMoveSpeed,
        float newDamage
    )
    {
        target = newTarget;
        moveSpeed = newMoveSpeed;
        damage = newDamage;

        isInitialized = true;
    }

    private void Update()
    {
        if (!isInitialized)
        {
            return;
        }

        if (target == null)
        {
            target = EnemyTargetFinder.FindNearestEnemy(
                transform.position,
                retargetRange,
                enemyLayer
                );

            if (target == null)
            {
                Destroy(gameObject);
                return;
            }
        }

        MoveTowardsTarget();
    }
    private void MoveTowardsTarget()
    {
        Vector2 currentPosition = transform.position;
        Vector2 targetPosition = target.position;

        Vector2 direction =
            (targetPosition - currentPosition).normalized;

        transform.position +=
            (Vector3)(direction * moveSpeed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"투사체 충돌: {other.gameObject.name}");

        Transform hitRoot =
            other.attachedRigidbody != null
                ? other.attachedRigidbody.transform
                : other.transform.root;

        bool isEnemyLayer =
            (enemyLayer.value & (1 << hitRoot.gameObject.layer)) != 0;

        if (!isEnemyLayer)
        {
            return;
        }

        IDamageable damageable =
            other.GetComponentInParent<IDamageable>();

        if (damageable == null)
        {
            Debug.Log("IDamageable을 찾지 못함");
            return;
        }

        Debug.Log($"적에게 {damage} 피해 적용");
        damageable.TakeDamage(damage);

        Destroy(gameObject);
    }
}