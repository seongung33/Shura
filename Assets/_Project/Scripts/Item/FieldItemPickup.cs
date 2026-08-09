using Shura.Player;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(Collider2D))]
public sealed class FieldItemPickup : NetworkBehaviour
{
    private const float HealthRestoreFraction = 0.3f;

    [Header("Enemy Freeze")]

    [SerializeField, Min(0.1f)]
    private float enemyFreezeDuration = 3f;

    [SerializeField, Min(0.1f)]
    private float enemyFreezeRadius = 18f;

    [Header("Visuals")]

    [SerializeField]
    private Color healthColor = new(1f, 0.25f, 0.25f);

    [SerializeField]
    private Color magnetColor = new(1f, 0.85f, 0.15f);

    [SerializeField]
    private Color enemyFreezeColor = new(0.25f, 0.85f, 1f);

    [SerializeField]
    private Color cooldownResetColor = new(0.85f, 0.35f, 1f);

    private readonly NetworkVariable<FieldItemType> networkItemType =
        new(FieldItemType.Health);

    private FieldItemType localItemType;
    private SpriteRenderer spriteRenderer;
    private bool consumed;

    public FieldItemType ItemType =>
        IsSpawned ? networkItemType.Value : localItemType;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public override void OnNetworkSpawn()
    {
        networkItemType.OnValueChanged += OnItemTypeChanged;

        if (IsServer)
        {
            networkItemType.Value = localItemType;
        }

        ApplyVisual(networkItemType.Value);
    }

    public override void OnNetworkDespawn()
    {
        networkItemType.OnValueChanged -= OnItemTypeChanged;
    }

    public void Initialize(FieldItemType itemType)
    {
        localItemType = itemType;
        ApplyVisual(localItemType);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (consumed || !other.CompareTag("Player"))
        {
            return;
        }

        if (IsSpawned && !IsServer)
        {
            return;
        }

        PlayerHealth playerHealth =
            other.GetComponentInParent<PlayerHealth>();

        if (playerHealth == null || !TryApplyEffect(playerHealth))
        {
            return;
        }

        consumed = true;

        if (IsSpawned)
        {
            NetworkObject.Despawn(true);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private bool TryApplyEffect(PlayerHealth playerHealth)
    {
        switch (ItemType)
        {
            case FieldItemType.Health:
                return HealthPickup.TryHealMaxHealthFraction(
                    playerHealth,
                    HealthRestoreFraction
                );

            case FieldItemType.Magnet:
                MagnetPickup.AttractAllExperienceOrbs(
                    playerHealth.transform
                );
                return true;

            case FieldItemType.EnemyFreeze:
                FreezeNearbyEnemies(playerHealth.transform.position);
                return true;

            case FieldItemType.SkillCooldownReset:
                return ResetPlayerSkillCooldowns(playerHealth.gameObject);

            default:
                return false;
        }
    }

    private void FreezeNearbyEnemies(Vector2 center)
    {
        float radiusSquared = enemyFreezeRadius * enemyFreezeRadius;
        EnemyHealth[] enemies = FindObjectsByType<EnemyHealth>(
            FindObjectsSortMode.None
        );

        foreach (EnemyHealth enemy in enemies)
        {
            if (enemy == null ||
                enemy.IsBoss ||
                ((Vector2)enemy.transform.position - center).sqrMagnitude >
                    radiusSquared)
            {
                continue;
            }

            enemy.GetComponent<EnemyController>()
                ?.FreezeFor(enemyFreezeDuration);
            enemy.GetComponent<EnemyAttack>()
                ?.FreezeFor(enemyFreezeDuration);
        }
    }

    private static bool ResetPlayerSkillCooldowns(GameObject player)
    {
        NetworkSkillCastRelay networkRelay =
            player.GetComponent<NetworkSkillCastRelay>();

        if (networkRelay != null && networkRelay.IsSpawned)
        {
            if (!networkRelay.IsServer)
            {
                return false;
            }

            networkRelay.ResetSkillCooldownsServer();
            return true;
        }

        AutoSkillCaster skillCaster =
            player.GetComponent<AutoSkillCaster>();

        if (skillCaster == null)
        {
            return false;
        }

        skillCaster.ResetSkillCooldowns();
        return true;
    }

    private void OnItemTypeChanged(
        FieldItemType previousValue,
        FieldItemType newValue
    )
    {
        ApplyVisual(newValue);
    }

    private void ApplyVisual(FieldItemType itemType)
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        if (spriteRenderer == null)
        {
            return;
        }

        spriteRenderer.color = itemType switch
        {
            FieldItemType.Health => healthColor,
            FieldItemType.Magnet => magnetColor,
            FieldItemType.EnemyFreeze => enemyFreezeColor,
            FieldItemType.SkillCooldownReset => cooldownResetColor,
            _ => Color.white
        };
    }

    private void OnValidate()
    {
        enemyFreezeDuration = Mathf.Max(0.1f, enemyFreezeDuration);
        enemyFreezeRadius = Mathf.Max(0.1f, enemyFreezeRadius);
    }
}
