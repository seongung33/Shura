using Unity.Netcode;
using UnityEngine;

public class EnemyHealth : NetworkBehaviour, IDamageable
{
    [SerializeField]
    private float maxHealth = 100f;

    [Header("Experience Drop")]

    [SerializeField]
    private ExperienceOrb experienceOrbPrefab;

    [SerializeField]
    private int experienceReward = 1;

    private readonly NetworkVariable<float> networkHealth =
        new NetworkVariable<float>();

    private float localHealth;
    private bool isDead;

    public float CurrentHealth
    {
        get
        {
            return IsSpawned ? networkHealth.Value : localHealth;
        }
    }

    private void Awake()
    {
        localHealth = maxHealth;
        isDead = false;
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            networkHealth.Value = maxHealth;
        }
    }

    public void TakeDamage(float damage)
    {
        if (!IsValidDamage(damage))
        {
            return;
        }

        if (!IsSpawned)
        {
            ApplyDamage(damage);
            return;
        }

        if (IsServer)
        {
            ApplyDamage(damage);
            return;
        }

        RequestDamageRpc(damage);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void RequestDamageRpc(float damage)
    {
        if (!IsValidDamage(damage))
        {
            return;
        }

        ApplyDamage(damage);
    }

    private void ApplyDamage(float damage)
    {
        if (isDead)
        {
            return;
        }

        float nextHealth = Mathf.Max(0f, CurrentHealth - damage);

        if (IsSpawned)
        {
            networkHealth.Value = nextHealth;
        }
        else
        {
            localHealth = nextHealth;
        }

        Debug.Log($"적 체력: {nextHealth} / {maxHealth}");

        if (nextHealth <= 0f)
        {
            Die();
        }
    }

    private void Die()
    {
        if (isDead)
        {
            return;
        }

        isDead = true;

        if (IsSpawned)
        {
            if (IsServer)
            {
                SpawnNetworkExperienceOrb();
                NetworkObject.Despawn(true);
            }

            return;
        }

        if (experienceOrbPrefab != null)
        {
            ExperienceOrb experienceOrb = Instantiate(
                experienceOrbPrefab,
                transform.position,
                Quaternion.identity
            );
            experienceOrb.Initialize(experienceReward);
        }

        Destroy(gameObject);
    }

    private void SpawnNetworkExperienceOrb()
    {
        if (experienceOrbPrefab == null)
        {
            return;
        }

        ExperienceOrb experienceOrb = Instantiate(
            experienceOrbPrefab,
            transform.position,
            Quaternion.identity
        );
        experienceOrb.Initialize(experienceReward);

        NetworkObject orbNetworkObject =
            experienceOrb.GetComponent<NetworkObject>();

        if (orbNetworkObject == null)
        {
            Debug.LogError(
                "ExperienceOrb에 NetworkObject가 없어 네트워크 생성할 수 없습니다."
            );
            Destroy(experienceOrb.gameObject);
            return;
        }

        orbNetworkObject.Spawn();
    }

    private static bool IsValidDamage(float damage)
    {
        return damage > 0f &&
            !float.IsNaN(damage) &&
            !float.IsInfinity(damage);
    }

    [ContextMenu("Test Damage")]
    private void TestDamage()
    {
        TakeDamage(25f);
    }
}
